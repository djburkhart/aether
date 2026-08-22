//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Replaced the VS2010 DomGen.exe host with a non-interactive net10.0 CLI.
// Schema walk and C# emit still live in Aether.Atf.DomGen (SchemaLoader / SchemaGen).

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

using DomGen;

namespace Aether.Atf.DomGen.Cli
{
    internal static class Program
    {
        private const int ExitOk = 0;
        private const int ExitUsage = 1;
        private const int ExitFailed = 2;
        private const int ExitCheckMismatch = 3;

        public static int Main(string[] args)
        {
            if (args == null || args.Length == 0 || HasHelp(args))
            {
                WriteHelp(args != null && args.Length == 0);
                return args == null || args.Length == 0 ? ExitUsage : ExitOk;
            }

            ParsedArgs parsed;
            try
            {
                parsed = Parse(args);
            }
            catch (UsageException ex)
            {
                Console.Error.WriteLine("Error: {0}", ex.Message);
                Console.Error.WriteLine("  aether-domgen <schema.xsd> <output.cs> <schema-namespace> <class-namespace>");
                Console.Error.WriteLine("  aether-domgen --schema testdata/atf/UsingDom/game.xsd --output GameSchema.cs --schema-namespace Game.UsingDom --class-namespace UsingDom");
                return ExitUsage;
            }

            try
            {
                return Run(parsed);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: {0}", ex.Message);
                return ExitFailed;
            }
        }

        private static int Run(ParsedArgs parsed)
        {
            if (!File.Exists(parsed.SchemaPath))
            {
                Console.Error.WriteLine("Error: schema file not found: {0}", parsed.SchemaPath);
                Console.Error.WriteLine("  aether-domgen --schema <schema.xsd> --output <output.cs> --schema-namespace <ns> --class-namespace <ns>");
                return ExitFailed;
            }

            if (string.IsNullOrEmpty(parsed.OutputPath) && !parsed.Stdout && !parsed.DryRun)
            {
                Console.Error.WriteLine("Error: no output path specified.");
                Console.Error.WriteLine("  aether-domgen --schema <schema.xsd> --output <output.cs> --schema-namespace <ns> --class-namespace <ns>");
                Console.Error.WriteLine("  aether-domgen --schema <schema.xsd> --schema-namespace <ns> --class-namespace <ns> --stdout");
                return ExitUsage;
            }

            var typeLoader = new SchemaLoader();
            bool upToDate = false;
            string cacheFile = parsed.OutputPath != null ? parsed.OutputPath + ".dep" : null;

            if (parsed.UseCache && cacheFile != null && File.Exists(cacheFile))
            {
                var resolver = new HashingXmlUrlResolver();
                typeLoader.SchemaResolver = resolver;
                typeLoader.Load(parsed.SchemaPath);

                try
                {
                    string previousHashString;
                    using (TextReader reader = File.OpenText(cacheFile))
                        previousHashString = reader.ReadLine();

                    var sb = new StringBuilder();
                    foreach (byte[] hash in resolver.Hashes)
                        sb.Append(Convert.ToBase64String(hash));

                    string hashString = sb.ToString();
                    upToDate = previousHashString == hashString;

                    if (!upToDate)
                    {
                        using (TextWriter writer = new StreamWriter(cacheFile))
                            writer.WriteLine(hashString);
                    }
                }
                catch (Exception)
                {
                    upToDate = false;
                }
            }
            else
            {
                typeLoader.Load(parsed.SchemaPath);
            }

            string[] headerArgs = BuildHeaderArgs(parsed);
            var options = SchemaGenOptions.FromArgs(headerArgs);
            options.GenerateAdapters |= parsed.GenerateAdapters;
            options.AnnotatedOnly |= parsed.AnnotatedOnly;
            options.GenerateEnums |= parsed.GenerateEnums;
            options.CommandLineArgs = headerArgs;

            string generated = SchemaGen.Generate(
                typeLoader,
                parsed.SchemaNamespace,
                parsed.ClassNamespace,
                parsed.ClassName,
                options);

            if (parsed.DryRun)
            {
                Console.WriteLine("would write: {0}", parsed.OutputPath ?? "(stdout)");
                Console.WriteLine("bytes: {0}", Encoding.UTF8.GetByteCount(generated));
                Console.WriteLine("up_to_date: {0}", upToDate);
                Console.WriteLine("class: {0}", parsed.ClassName);
                return ExitOk;
            }

            if (parsed.Check)
            {
                if (string.IsNullOrEmpty(parsed.OutputPath) || !File.Exists(parsed.OutputPath))
                {
                    Console.Error.WriteLine("Error: --check requires an existing output file to compare.");
                    Console.Error.WriteLine("  aether-domgen game.xsd GameSchema.cs Game.UsingDom UsingDom --check");
                    return ExitUsage;
                }

                string expected = File.ReadAllText(parsed.OutputPath);
                if (Normalize(expected) == Normalize(generated))
                {
                    Console.WriteLine("ok: {0}", parsed.OutputPath);
                    return ExitOk;
                }

                Console.Error.WriteLine("Error: generated C# does not match {0}", parsed.OutputPath);
                WriteFirstDifference(Normalize(expected), Normalize(generated));
                return ExitCheckMismatch;
            }

            if (upToDate)
            {
                Console.WriteLine("up_to_date: {0}", parsed.OutputPath);
                return ExitOk;
            }

            if (parsed.Stdout || string.IsNullOrEmpty(parsed.OutputPath))
            {
                Console.Out.Write(generated);
                return ExitOk;
            }

            string directory = Path.GetDirectoryName(parsed.OutputPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(parsed.OutputPath, generated, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            Console.WriteLine("wrote: {0}", parsed.OutputPath);
            Console.WriteLine("bytes: {0}", Encoding.UTF8.GetByteCount(generated));
            return ExitOk;
        }

        private static string[] BuildHeaderArgs(ParsedArgs parsed)
        {
            // Keep the ATF header shape: DomGen "game.xsd" "GameSchema.cs" "Game.UsingDom" "UsingDom"
            var list = new List<string>
            {
                Path.GetFileName(parsed.SchemaPath),
                parsed.OutputPath != null ? Path.GetFileName(parsed.OutputPath) : parsed.ClassName + ".cs",
                parsed.SchemaNamespace,
                parsed.ClassNamespace
            };
            if (parsed.GenerateAdapters)
                list.Add("-a");
            if (parsed.AnnotatedOnly)
                list.Add("-annotatedOnly");
            if (parsed.GenerateEnums)
                list.Add("-enums");
            if (parsed.UseCache)
                list.Add("-cache");
            return list.ToArray();
        }

        private static string Normalize(string text)
        {
            return text.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        private static void WriteFirstDifference(string expected, string actual)
        {
            string[] expectedLines = expected.Split('\n');
            string[] actualLines = actual.Split('\n');
            int count = Math.Min(expectedLines.Length, actualLines.Length);
            for (int i = 0; i < count; i++)
            {
                if (expectedLines[i] != actualLines[i])
                {
                    Console.Error.WriteLine("  first mismatch at line {0}", i + 1);
                    Console.Error.WriteLine("  expected: {0}", expectedLines[i]);
                    Console.Error.WriteLine("  actual:   {0}", actualLines[i]);
                    return;
                }
            }

            if (expectedLines.Length != actualLines.Length)
            {
                Console.Error.WriteLine("  line count expected {0}, actual {1}", expectedLines.Length, actualLines.Length);
            }
        }

        private static bool HasHelp(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg == "-h" || arg == "--help" || arg == "-?" || arg == "/?")
                    return true;
            }
            return false;
        }

        private static void WriteHelp(bool missingArgs)
        {
            if (missingArgs)
                Console.Error.WriteLine("Error: missing required arguments.");

            TextWriter w = missingArgs ? Console.Error : Console.Out;
            w.WriteLine("Generate typed C# from an ATF XML schema (SonyWWS DomGen).");
            w.WriteLine();
            w.WriteLine("Usage:");
            w.WriteLine("  aether-domgen <schema.xsd> <output.cs> <schema-namespace> <class-namespace> [options]");
            w.WriteLine("  aether-domgen --schema <schema.xsd> --output <output.cs> --schema-namespace <ns> --class-namespace <ns> [options]");
            w.WriteLine();
            w.WriteLine("Options:");
            w.WriteLine("  --schema <path>              Input .xsd");
            w.WriteLine("  --output <path>              Output .cs (class name defaults to file name)");
            w.WriteLine("  --schema-namespace <ns>      Schema target namespace (empty string uses the first loaded collection)");
            w.WriteLine("  --class-namespace <ns>       C# namespace of the generated class");
            w.WriteLine("  --class-name <name>          Generated static class name (default: output file name)");
            w.WriteLine("  -a, --adapters               Generate DomNodeAdapter partial classes");
            w.WriteLine("  --annotated-only             Include only types with sce.domgen include=\"true\"");
            w.WriteLine("  --enums                      Generate enums for restricted string attribute types");
            w.WriteLine("  --cache                      Skip rewrite when a .dep hash of resolved schemas matches");
            w.WriteLine("  --stdout                     Write generated C# to stdout");
            w.WriteLine("  --dry-run                    Load and generate, but do not write a file");
            w.WriteLine("  --check                      Generate and compare to --output; exit 3 on mismatch");
            w.WriteLine("  -h, --help                   Show this help");
            w.WriteLine();
            w.WriteLine("Original DomGen flags are also accepted: -adapters, -annotatedOnly, -enums, -cache.");
            w.WriteLine();
            w.WriteLine("Examples:");
            w.WriteLine("  aether-domgen testdata/atf/UsingDom/game.xsd testdata/atf/UsingDom/GameSchema.cs Game.UsingDom UsingDom");
            w.WriteLine("  aether-domgen --schema testdata/atf/UsingDom/game.xsd --output GameSchema.cs --schema-namespace Game.UsingDom --class-namespace UsingDom");
            w.WriteLine("  aether-domgen testdata/atf/UsingDom/game.xsd testdata/atf/UsingDom/GameSchema.cs Game.UsingDom UsingDom --check");
            w.WriteLine("  aether-domgen testdata/atf/UsingDom/game.xsd GameSchema.cs Game.UsingDom UsingDom --adapters --dry-run");
        }

        private static ParsedArgs Parse(string[] args)
        {
            var parsed = new ParsedArgs();
            var positionals = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                switch (arg)
                {
                    case "--schema":
                        parsed.SchemaPath = RequireValue(args, ref i, arg);
                        break;
                    case "--output":
                        parsed.OutputPath = RequireValue(args, ref i, arg);
                        break;
                    case "--schema-namespace":
                        parsed.SchemaNamespace = RequireValue(args, ref i, arg);
                        break;
                    case "--class-namespace":
                        parsed.ClassNamespace = RequireValue(args, ref i, arg);
                        break;
                    case "--class-name":
                        parsed.ClassName = RequireValue(args, ref i, arg);
                        break;
                    case "-a":
                    case "--adapters":
                    case "-adapters":
                        parsed.GenerateAdapters = true;
                        break;
                    case "--annotated-only":
                    case "-annotatedOnly":
                        parsed.AnnotatedOnly = true;
                        break;
                    case "--enums":
                    case "-enums":
                        parsed.GenerateEnums = true;
                        break;
                    case "--cache":
                    case "-cache":
                        parsed.UseCache = true;
                        break;
                    case "--stdout":
                        parsed.Stdout = true;
                        break;
                    case "--dry-run":
                        parsed.DryRun = true;
                        break;
                    case "--check":
                        parsed.Check = true;
                        break;
                    default:
                        if (arg.StartsWith("-", StringComparison.Ordinal))
                            throw new UsageException("unknown option " + arg);
                        positionals.Add(arg);
                        break;
                }
            }

            if (positionals.Count > 0 && parsed.SchemaPath == null)
            {
                if (positionals.Count < 4)
                    throw new UsageException("positional usage requires schema, output, schema-namespace, and class-namespace.");
                parsed.SchemaPath = positionals[0];
                parsed.OutputPath = positionals[1];
                parsed.SchemaNamespace = positionals[2];
                parsed.ClassNamespace = positionals[3];
                if (positionals.Count > 4)
                    throw new UsageException("unexpected extra argument: " + positionals[4]);
            }

            if (string.IsNullOrEmpty(parsed.SchemaPath))
                throw new UsageException("no schema specified.");
            if (parsed.SchemaNamespace == null)
                throw new UsageException("no schema-namespace specified.");
            if (string.IsNullOrEmpty(parsed.ClassNamespace))
                throw new UsageException("no class-namespace specified.");

            if (string.IsNullOrEmpty(parsed.ClassName))
            {
                if (!string.IsNullOrEmpty(parsed.OutputPath))
                    parsed.ClassName = Path.GetFileNameWithoutExtension(parsed.OutputPath);
                else
                    parsed.ClassName = "Schema";
            }

            return parsed;
        }

        private static string RequireValue(string[] args, ref int i, string name)
        {
            if (i + 1 >= args.Length)
                throw new UsageException(name + " requires a value.");
            i++;
            return args[i];
        }

        private sealed class ParsedArgs
        {
            public string SchemaPath;
            public string OutputPath;
            public string SchemaNamespace;
            public string ClassNamespace;
            public string ClassName;
            public bool GenerateAdapters;
            public bool AnnotatedOnly;
            public bool GenerateEnums;
            public bool UseCache;
            public bool Stdout;
            public bool DryRun;
            public bool Check;
        }

        private sealed class UsageException : Exception
        {
            public UsageException(string message) : base(message) { }
        }
    }

    /// <summary>
    /// Resolver which takes a hash of all streams which are resolved (original DomGen cache helper).</summary>
    internal sealed class HashingXmlUrlResolver : XmlUrlResolver
    {
        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            object entity = base.GetEntity(absoluteUri, role, ofObjectToReturn);
            Stream s = entity as Stream;
            if (s != null)
            {
                long pos = s.Position;
                using (var md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(s);
                    s.Position = pos;
                    m_hashes.Add(hash);
                }
            }
            return entity;
        }

        public IEnumerable<byte[]> Hashes { get { return m_hashes; } }

        private readonly List<byte[]> m_hashes = new List<byte[]>();
    }
}
