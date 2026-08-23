using System;

using MoonSharp.Interpreter;

namespace Aether.Scripting
{
    /// <summary>
    /// Lua via MoonSharp (pure C#, HardSandbox). Chosen over NLua so this
    /// slice has no native Lua / C++ build. SLED bundled Lua 5.1.4 / 5.2.3
    /// for an in-game C++ target; Aether hosts in-process instead.</summary>
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

        public ScriptResult Run(string source, ScriptDocument document)
        {
            if (document == null)
                throw new ArgumentNullException("document");
            if (string.IsNullOrWhiteSpace(source))
                return ScriptResult.Fail("Lua source is empty.");

            try
            {
                UserData.RegisterType<ScriptDocument>();
                var script = new Script(CoreModules.Preset_HardSandbox);
                script.Globals["document"] = document;
                script.DoString(source);
                return ScriptResult.Ok(document.Output);
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
