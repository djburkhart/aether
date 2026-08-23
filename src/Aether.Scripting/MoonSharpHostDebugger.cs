using System.Collections.Generic;

using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Debugging;

namespace Aether.Scripting
{
    /// <summary>
    /// MoonSharp <c>IDebugger</c> that forwards source-line stops to
    /// <see cref="IStatementBreak"/>. Visual Studio is not involved.</summary>
    internal sealed class MoonSharpHostDebugger : MoonSharp.Interpreter.Debugging.IDebugger
    {
        public MoonSharpHostDebugger(IStatementBreak breaks, string path, ScriptDocument document)
        {
            m_breaks = breaks;
            m_path = path ?? string.Empty;
            m_document = document;
        }

        public DebuggerCaps GetDebuggerCaps()
        {
            return DebuggerCaps.CanDebugSourceCode | DebuggerCaps.HasLineBasedBreakpoints;
        }

        public void SetDebugService(DebugService debugService)
        {
        }

        public void SetSourceCode(SourceCode sourceCode)
        {
        }

        public void SetByteCode(string[] byteCode)
        {
        }

        public bool IsPauseRequested()
        {
            return true;
        }

        public bool SignalRuntimeException(ScriptRuntimeException ex)
        {
            return false;
        }

        public DebuggerAction GetAction(int ip, SourceRef sourceref)
        {
            if (sourceref != null && !sourceref.IsClrLocation && sourceref.FromLine > 0)
                m_breaks.OnStatement("lua", m_path, sourceref.FromLine, m_document);
            return new DebuggerAction { Action = DebuggerAction.ActionType.Run };
        }

        public void SignalExecutionEnded()
        {
        }

        public void Update(WatchType watchType, IEnumerable<WatchItem> items)
        {
        }

        public List<DynamicExpression> GetWatchItems()
        {
            return new List<DynamicExpression>();
        }

        public void RefreshBreakpoints(IEnumerable<SourceRef> refs)
        {
        }

        private readonly IStatementBreak m_breaks;
        private readonly string m_path;
        private readonly ScriptDocument m_document;
    }
}
