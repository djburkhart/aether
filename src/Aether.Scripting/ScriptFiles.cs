using System;
using System.IO;
using System.Reflection;

namespace Aether.Scripting
{
    /// <summary>Testdata path lookup for committed C# / Lua fixtures.</summary>
    public static class ScriptFiles
    {
        public const string SampleCSharpFileName = "resize-bill.csx";
        public const string SampleLuaFileName = "resize-bill.lua";
        public const int ExpectedBillSize = 14;
        public const int DefaultBillSize = 12;

        /// <summary>
        /// 1-based line of <c>SetAttribute(..., 14)</c> in the resize-bill fixtures.
        /// A breakpoint here must pause before Bill Size changes.</summary>
        public const int SampleWriteLine = 2;

        /// <summary>True when the path is a script this host can run (.csx / .lua).</summary>
        public static bool IsScriptFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            string ext = Path.GetExtension(path);
            return string.Equals(ext, ".csx", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ext, ".lua", StringComparison.OrdinalIgnoreCase);
        }

        public static string FindSampleCSharpPath()
        {
            return FindScriptFile(SampleCSharpFileName);
        }

        public static string FindSampleLuaPath()
        {
            return FindScriptFile(SampleLuaFileName);
        }

        public static string FindScriptsTestdataDirectory()
        {
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (!string.IsNullOrEmpty(dir))
            {
                string candidate = Path.Combine(dir, "testdata", "scripts");
                if (File.Exists(Path.Combine(candidate, SampleCSharpFileName)))
                    return Path.GetFullPath(candidate);
                dir = Path.GetDirectoryName(dir);
            }

            string cwd = Path.Combine(Directory.GetCurrentDirectory(), "testdata", "scripts");
            if (File.Exists(Path.Combine(cwd, SampleCSharpFileName)))
                return Path.GetFullPath(cwd);

            return null;
        }

        public static string FindScriptFile(string fileName)
        {
            string nextToExe = Path.Combine(AppContext.BaseDirectory, fileName);
            if (File.Exists(nextToExe))
                return Path.GetFullPath(nextToExe);

            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (!string.IsNullOrEmpty(dir))
            {
                string candidate = Path.Combine(dir, "testdata", "scripts", fileName);
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
                dir = Path.GetDirectoryName(dir);
            }

            string cwd = Path.Combine(Directory.GetCurrentDirectory(), "testdata", "scripts", fileName);
            if (File.Exists(cwd))
                return Path.GetFullPath(cwd);

            return null;
        }
    }
}
