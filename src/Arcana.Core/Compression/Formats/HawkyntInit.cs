using System.Reflection;
using Compression.Registry;
using Serilog;

namespace Arcana.Core.Compression.Formats;

public static class HawkyntInit
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(HawkyntInit));

    private static bool _initialized;
    private static readonly object _lock = new();
    private static int _registeredCount;

    public static void Ensure()
    {
        if (_initialized) return;
        lock (_lock)
        {
            if (_initialized) return;
            Log.Information("Initializing Hawkynt format registry");
            FormatRegistry.Initialize();
            RegisterAllFormatDescriptors();
            _initialized = true;
            Log.Information("Hawkynt format registry ready ({Formats} formats)", _registeredCount);
        }
    }

    private static void RegisterAllFormatDescriptors()
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            RegisterFromAssembly(asm);

        var dir = Path.GetDirectoryName(typeof(HawkyntInit).Assembly.Location);
        if (dir is null) return;
        Log.Debug("Scanning {Directory} for FileFormat.*.dll", dir);
        foreach (var dll in Directory.EnumerateFiles(dir, "FileFormat.*.dll"))
        {
            try
            {
                var asm = Assembly.LoadFrom(dll);
                Log.Debug("Loaded format assembly {Assembly}", asm.FullName);
                RegisterFromAssembly(asm);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load format assembly {Dll}", dll);
            }
        }
    }

    private static void RegisterFromAssembly(Assembly asm)
    {
        if (!asm.GetName().Name?.StartsWith("FileFormat.") is true)
            return;

        foreach (var type in asm.GetExportedTypes())
        {
            if (!typeof(IFormatDescriptor).IsAssignableFrom(type))
                continue;
            if (type.IsAbstract || type.IsInterface)
                continue;
            if (type.GetConstructor(Type.EmptyTypes) is null)
                continue;

            try
            {
                var desc = (IFormatDescriptor)Activator.CreateInstance(type)!;
                if (desc.Category != FormatCategory.Archive)
                    continue;
                if (FormatRegistry.GetById(desc.Id) is not null)
                    continue;
                FormatRegistry.Register(desc);
                _registeredCount++;
                Log.Verbose("Registered format descriptor {Id}", desc.Id);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not register format descriptor {Type} from {Assembly}",
                    type.FullName, asm.GetName().Name);
            }
        }
    }
}
