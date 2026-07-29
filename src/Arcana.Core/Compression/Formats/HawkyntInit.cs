using System.Reflection;
using Compression.Registry;

namespace Arcana.Core.Compression.Formats;

public static class HawkyntInit
{
    private static bool _initialized;
    private static readonly object _lock = new();

    public static void Ensure()
    {
        if (_initialized) return;
        lock (_lock)
        {
            if (_initialized) return;
            FormatRegistry.Initialize();
            RegisterAllFormatDescriptors();
            _initialized = true;
        }
    }

    private static void RegisterAllFormatDescriptors()
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            RegisterFromAssembly(asm);

        var dir = Path.GetDirectoryName(typeof(HawkyntInit).Assembly.Location);
        if (dir is null) return;
        foreach (var dll in Directory.EnumerateFiles(dir, "FileFormat.*.dll"))
        {
            try
            {
                var asm = Assembly.LoadFrom(dll);
                RegisterFromAssembly(asm);
            }
            catch { }
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
            }
            catch { }
        }
    }
}
