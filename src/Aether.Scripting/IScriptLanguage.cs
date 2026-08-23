namespace Aether.Scripting
{
    /// <summary>
    /// One hosted language. SLED used <c>ISledLanguagePlugin</c> for the same
    /// split (name + extensions). This is a new Aether interface, not a port
    /// of that WinForms plugin.</summary>
    public interface IScriptLanguage
    {
        /// <summary>Stable id, e.g. <c>csharp</c> or <c>lua</c>.</summary>
        string Id { get; }

        /// <summary>Name shown in the Script pane.</summary>
        string DisplayName { get; }

        /// <summary>Primary file extension including the dot (<c>.csx</c>, <c>.lua</c>).</summary>
        string FileExtension { get; }

        /// <summary>Runs <paramref name="source"/> against the context document.</summary>
        ScriptResult Run(string source, ScriptRunContext context);
    }
}
