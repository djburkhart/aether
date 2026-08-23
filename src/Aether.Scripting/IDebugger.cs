using System;
using System.Collections.Generic;

namespace Aether.Scripting
{
    /// <summary>
    /// Breakpoint hook for a later debugger slice.
    /// A DAP listen-and-attach server is not implemented here: this slice's
    /// done bar is Run of C# and Lua. <see cref="BreakpointHit"/> is never
    /// raised by the current run loop.</summary>
    public interface IDebugger
    {
        /// <summary>Breakpoints recorded by the host (not yet honored on Run).</summary>
        IReadOnlyList<Breakpoint> Breakpoints { get; }

        /// <summary>Records a breakpoint. The current run loop does not stop.</summary>
        void SetBreakpoint(string path, int line);

        /// <summary>Removes one recorded breakpoint.</summary>
        void RemoveBreakpoint(string path, int line);

        /// <summary>Clears all recorded breakpoints.</summary>
        void ClearBreakpoints();

        /// <summary>Reserved for a future run loop that honors breakpoints.</summary>
        event EventHandler<BreakpointHitEventArgs> BreakpointHit;
    }

    /// <summary>One recorded breakpoint.</summary>
    public sealed class Breakpoint
    {
        public Breakpoint(string path, int line)
        {
            Path = path ?? string.Empty;
            Line = line;
        }

        public string Path { get; }

        public int Line { get; }
    }

    /// <summary>Args for <see cref="IDebugger.BreakpointHit"/>.</summary>
    public sealed class BreakpointHitEventArgs : EventArgs
    {
        public BreakpointHitEventArgs(Breakpoint breakpoint)
        {
            Breakpoint = breakpoint;
        }

        public Breakpoint Breakpoint { get; }
    }
}
