using Aether.Stride;

namespace Aether.Editor
{
    /// <summary>
    /// Viewport dock session. Holds the Stride host probe result. NullGameEngine
    /// stays the LevelEditor backend — this spike does not present frames.</summary>
    public sealed class ViewportSession
    {
        public ViewportSession()
        {
            Result = StrideHost.Probe();
        }

        public StrideHostResult Result { get; }

        public bool Initialized
        {
            get { return Result.Initialized; }
        }

        public string StatusText
        {
            get { return Result.StatusText; }
        }
    }
}
