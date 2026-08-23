namespace Aether.Scripting
{
    /// <summary>One Run: document, source path, and the debugger hook.</summary>
    public sealed class ScriptRunContext
    {
        public ScriptRunContext(ScriptDocument document, string path, IDebugger debugger, IStatementBreak breaks)
        {
            Document = document;
            Path = path ?? string.Empty;
            Debugger = debugger;
            Breaks = breaks;
        }

        public ScriptDocument Document { get; }

        public string Path { get; }

        public IDebugger Debugger { get; }

        public IStatementBreak Breaks { get; }
    }
}
