namespace Aether.Scripting
{
    /// <summary>
    /// Registers languages and runs source against a <see cref="ScriptDocument"/>.
    /// In-process host — not SLED's SCMP / C++ LibSledDebugger target.</summary>
    public interface IScriptHost
    {
        /// <summary>Registered languages.</summary>
        System.Collections.Generic.IReadOnlyList<IScriptLanguage> Languages { get; }

        /// <summary>Breakpoint hook. Run does not stop in this slice.</summary>
        IDebugger Debugger { get; }

        /// <summary>Looks up a language by id or file extension.</summary>
        IScriptLanguage FindLanguage(string idOrExtension);

        /// <summary>Runs source with the given language id (<c>csharp</c> / <c>lua</c>).</summary>
        ScriptResult Run(string languageId, string source, ScriptDocument document);
    }
}
