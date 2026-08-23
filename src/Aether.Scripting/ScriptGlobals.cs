namespace Aether.Scripting
{
    /// <summary>
    /// Globals for Roslyn C# scripts. Scripts use <c>document</c> and <c>log</c>
    /// — not process or file APIs. <c>__line</c> is injected by the host.</summary>
    public sealed class ScriptGlobals
    {
        public ScriptGlobals(ScriptDocument document, IStatementBreak breaks, string path)
        {
            this.document = document;
            m_breaks = breaks;
            m_path = path ?? string.Empty;
        }

        /// <summary>The bound Aether document.</summary>
        public ScriptDocument document { get; }

        /// <summary>Writes a line to the Script pane output.</summary>
        public void log(string message)
        {
            document.Log(message);
        }

        /// <summary>Host-injected statement hook. Not part of the sample API.</summary>
        public void __line(int line)
        {
            if (m_breaks != null)
                m_breaks.OnStatement("csharp", m_path, line, document);
        }

        private readonly IStatementBreak m_breaks;
        private readonly string m_path;
    }
}
