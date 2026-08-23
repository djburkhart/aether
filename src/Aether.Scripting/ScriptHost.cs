using System;
using System.Collections.Generic;

namespace Aether.Scripting
{
    /// <summary>
    /// Default host: C# (Roslyn) + Lua (MoonSharp) and a no-op debugger hook.</summary>
    public sealed class ScriptHost : IScriptHost
    {
        public ScriptHost()
        {
            Debugger = new ScriptDebugger();
            m_languages.Add(new CSharpScriptLanguage());
            m_languages.Add(new LuaScriptLanguage());
        }

        public IReadOnlyList<IScriptLanguage> Languages
        {
            get { return m_languages; }
        }

        public IDebugger Debugger { get; }

        public IScriptLanguage FindLanguage(string idOrExtension)
        {
            if (string.IsNullOrEmpty(idOrExtension))
                return null;

            string key = idOrExtension.Trim();
            foreach (IScriptLanguage language in m_languages)
            {
                if (string.Equals(language.Id, key, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(language.FileExtension, key, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(language.FileExtension, "." + key.TrimStart('.'), StringComparison.OrdinalIgnoreCase))
                    return language;
            }
            return null;
        }

        public ScriptResult Run(string languageId, string source, ScriptDocument document)
        {
            IScriptLanguage language = FindLanguage(languageId);
            if (language == null)
                return ScriptResult.Fail("Unknown script language: " + languageId);
            return language.Run(source, document);
        }

        private readonly List<IScriptLanguage> m_languages = new List<IScriptLanguage>();
    }
}
