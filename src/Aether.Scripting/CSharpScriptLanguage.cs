using System;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Aether.Scripting
{
    /// <summary>
    /// C# via Roslyn scripting. Statement boundaries get an injected
    /// <c>__line(n)</c> hook so breakpoints pause before the statement.</summary>
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

        public ScriptResult Run(string source, ScriptRunContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (context.Document == null)
                throw new ArgumentNullException("document");
            if (string.IsNullOrWhiteSpace(source))
                return ScriptResult.Fail("C# source is empty.");

            string rewritten = CSharpLineHooks.Inject(source);
            var globals = new ScriptGlobals(context.Document, context.Breaks, context.Path);
            ScriptOptions options = ScriptOptions.Default
                .WithReferences(typeof(ScriptDocument).Assembly)
                .WithImports("System");

            try
            {
                CSharpScript.RunAsync(rewritten, options, globals).GetAwaiter().GetResult();
                return ScriptResult.Ok(context.Document.Output);
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
