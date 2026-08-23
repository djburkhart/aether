namespace Aether.Scripting
{
    /// <summary>
    /// Globals for Roslyn C# scripts. Scripts use <c>document</c> and <c>log</c>
    /// — not process or file APIs.</summary>
    public sealed class ScriptGlobals
    {
        public ScriptGlobals(ScriptDocument document)
        {
            this.document = document;
        }

        /// <summary>The bound Aether document.</summary>
        public ScriptDocument document { get; }

        /// <summary>Writes a line to the Script pane output.</summary>
        public void log(string message)
        {
            document.Log(message);
        }
    }
}
