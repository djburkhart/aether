using System;

using MoonSharp.Interpreter;

namespace Aether.Scripting
{
    /// <summary>
    /// Lua via MoonSharp (pure C#, HardSandbox). Attaches a host debugger so
    /// <c>GetAction</c> can pause on source lines. No Visual Studio / DAP.</summary>
    public sealed class LuaScriptLanguage : IScriptLanguage
    {
        public string Id
        {
            get { return "lua"; }
        }

        public string DisplayName
        {
            get { return "Lua"; }
        }

        public string FileExtension
        {
            get { return ".lua"; }
        }

        public ScriptResult Run(string source, ScriptRunContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (context.Document == null)
                throw new ArgumentNullException("document");
            if (string.IsNullOrWhiteSpace(source))
                return ScriptResult.Fail("Lua source is empty.");

            try
            {
                UserData.RegisterType<ScriptDocument>();
                var script = new Script(CoreModules.Preset_HardSandbox);
                script.Globals["document"] = context.Document;
                script.AttachDebugger(new MoonSharpHostDebugger(context.Breaks, context.Path, context.Document));
                script.DoString(source);
                return ScriptResult.Ok(context.Document.Output);
            }
            catch (InterpreterException ex)
            {
                return ScriptResult.Fail(ex.DecoratedMessage ?? ex.Message, ex);
            }
            catch (Exception ex)
            {
                return ScriptResult.Fail(ex.Message, ex);
            }
        }
    }
}
