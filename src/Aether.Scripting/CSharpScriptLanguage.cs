using System;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Aether.Scripting
{
    /// <summary>
    /// C# via Roslyn scripting. The sample globals are <see cref="ScriptGlobals"/>
    /// only — this is not a security sandbox; it is the in-editor host API.</summary>
    public sealed class CSharpScriptLanguage : IScriptLanguage
    {
        public string Id
        {
            get { return "csharp"; }
        }

        public string DisplayName
        {
            get { return "C#"; }
        }

        public string FileExtension
        {
            get { return ".csx"; }
        }

        public ScriptResult Run(string source, ScriptDocument document)
        {
            if (document == null)
                throw new ArgumentNullException("document");
            if (string.IsNullOrWhiteSpace(source))
                return ScriptResult.Fail("C# source is empty.");

            var globals = new ScriptGlobals(document);
            ScriptOptions options = ScriptOptions.Default
                .WithReferences(typeof(ScriptDocument).Assembly)
                .WithImports("System");

            try
            {
                CSharpScript.RunAsync(source, options, globals).GetAwaiter().GetResult();
                return ScriptResult.Ok(document.Output);
            }
            catch (CompilationErrorException ex)
            {
                return ScriptResult.Fail(ex.Message, ex);
            }
            catch (Exception ex)
            {
                return ScriptResult.Fail(ex.Message, ex);
            }
        }
    }
}
