using System.Diagnostics;
using System.Security.Cryptography;
using Serilog;

namespace Arcana.Core.Cryptography;

public class EncryptionProvider
{
    private readonly ILogger _log = Serilog.Log.ForContext<EncryptionProvider>();
    private readonly EncryptionOptions _options;
    private byte[]? _key;
    private byte[]? _salt;

    public EncryptionProvider(EncryptionOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        var alg = options.Key != null ? "AES-256-GCM" : "AES-256-GCM+Argon2";
        var kdf = options.Password != null ? "Argon2id" : "none";
        _log.Information("Encryption initialized with {Algorithm} / {Kdf}", alg, kdf);
    }

    private byte[] GetKey()
    {
        if (_key != null) return _key;

        if (_options.Key != null)
        {
            _key = _options.Key;
        }
        else if (_options.Password != null)
        {
            var sw = Stopwatch.StartNew();
            _salt = Argon2KeyDerivation.GenerateSalt();
            _key = Argon2KeyDerivation.DeriveKey(
                _options.Password, _salt, 32,
                _options.KdfMemoryMB, _options.KdfIterations, _options.KdfParallelism);
            sw.Stop();
            _log.Debug("Key derivation: {Time}ms", sw.ElapsedMilliseconds);
        }
        else
        {
            throw new InvalidOperationException("No key or password configured");
        }

        return _key;
    }

    public byte[] Encrypt(byte[] plaintext, byte[] associatedData)
    {
        _log.Verbose("Encrypting {Length} bytes", plaintext.Length);
        var key = GetKey();
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        try
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Encryption failed: {Message}", ex.Message);
            throw;
        }

        var payload = new byte[nonce.Length + tag.Length + ciphertext.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, nonce.Length);
        ciphertext.CopyTo(payload, nonce.Length + tag.Length);

        if (_salt != null)
        {
            var result = new byte[_salt.Length + payload.Length];
            _salt.CopyTo(result, 0);
            payload.CopyTo(result, _salt.Length);
            return result;
        }

        return payload;
    }

    public byte[] Decrypt(byte[] data, byte[] associatedData)
    {
        _log.Verbose("Decrypting {Length} bytes", data.Length);
        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;

        if (_options.Key != null)
        {
            _key = _options.Key;
        }
        else if (_options.Password != null)
        {
            _salt = data[..16];
            data = data[16..];
            _key = Argon2KeyDerivation.DeriveKey(
                _options.Password, _salt, 32,
                _options.KdfMemoryMB, _options.KdfIterations, _options.KdfParallelism);
        }

        var nonce = data[..nonceSize];
        var tag = data[nonceSize..(nonceSize + tagSize)];
        var ciphertext = data[(nonceSize + tagSize)..];

        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key!, tagSize);
        try
        {
            aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Decryption failed: {Message}", ex.Message);
            throw;
        }

        return plaintext;
    }

    public Stream CreateEncryptingStream(Stream innerStream)
    {
        var salt = Argon2KeyDerivation.GenerateSalt();
        var key = Argon2KeyDerivation.DeriveKey(
            _options.Password!, salt, 32,
            _options.KdfMemoryMB, _options.KdfIterations, _options.KdfParallelism);
        var encryptor = new EncryptionProvider(new EncryptionOptions { Key = key });
        return new EncryptStream(innerStream, salt, encryptor);
    }

    public Stream CreateDecryptingStream(Stream innerStream)
    {
        return new DecryptStream(innerStream, _options);
    }

    private class EncryptStream : Stream
    {
        private readonly Stream _inner;
        private readonly byte[] _salt;
        private readonly EncryptionProvider _provider;
        private readonly MemoryStream _buffer = new();
        private bool _written;

        public EncryptStream(Stream inner, byte[] salt, EncryptionProvider provider)
        {
            _inner = inner;
            _salt = salt;
            _provider = provider;
        }

        public override bool CanWrite => true;
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _buffer.Write(buffer, offset, count);
            _written = true;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        {
            await _buffer.WriteAsync(buffer, offset, count, ct).ConfigureAwait(false);
            _written = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _written)
            {
                var plaintext = _buffer.ToArray();
                var encrypted = _provider.Encrypt(plaintext, []);
                _inner.Write(_salt, 0, _salt.Length);
                _inner.Write(encrypted, 0, encrypted.Length);
            }
            _buffer.Dispose();
            base.Dispose(disposing);
        }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }

    private class DecryptStream : Stream
    {
        private readonly Stream _inner;
        private readonly EncryptionOptions _options;
        private byte[]? _decrypted;
        private int _position;

        public DecryptStream(Stream inner, EncryptionOptions options)
        {
            _inner = inner;
            _options = options;
        }

        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override bool CanSeek => true;
        public override long Length => EnsureDecrypted().Length;
        public override long Position { get => _position; set => _position = (int)value; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var data = EnsureDecrypted();
            var available = Math.Min(count, data.Length - _position);
            if (available <= 0) return 0;
            Buffer.BlockCopy(data, _position, buffer, offset, available);
            _position += available;
            return available;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            _position = origin switch
            {
                SeekOrigin.Begin => (int)offset,
                SeekOrigin.Current => (int)(_position + offset),
                SeekOrigin.End => (int)(EnsureDecrypted().Length + offset),
                _ => _position,
            };
            return _position;
        }

        private byte[] EnsureDecrypted()
        {
            if (_decrypted != null) return _decrypted;
            using var ms = new MemoryStream();
            _inner.CopyTo(ms);

            var saltSize = 16;
            var salt = new byte[saltSize];
            ms.Position = 0;
            ms.ReadExactly(salt);
            var encryptedData = new byte[ms.Length - saltSize];
            ms.ReadExactly(encryptedData);

            var key = Argon2KeyDerivation.DeriveKey(
                _options.Password!, salt, 32,
                _options.KdfMemoryMB, _options.KdfIterations, _options.KdfParallelism);

            var decryptProvider = new EncryptionProvider(new EncryptionOptions { Key = key });
            _decrypted = decryptProvider.Decrypt(encryptedData, []);
            return _decrypted;
        }

        public override void Flush() { }
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
