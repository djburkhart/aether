using Aether.Stride;

namespace Aether.Editor
{
    /// <summary>
    /// Center Viewport session: live presenter + Stride host probe.
    /// NullGameEngine stays the LevelEditor data backend.</summary>
    public sealed class ViewportSession
    {
        public ViewportSession()
        {
            Result = StrideHost.Probe();
            Presenter = new ViewportPresenter();
            Presenter.Tick(0);
        }

        public StrideHostResult Result { get; }

        public ViewportPresenter Presenter { get; }

        public bool Initialized
        {
            get { return Result.Initialized; }
        }

        public bool IsLivePresent
        {
            get { return Presenter.IsLiveControl; }
        }

        public string OverlayText
        {
            get
            {
                string gpu = Presenter.ActivePath == ViewportPresenter.StrideRttPath
                    ? "Stride GPU rtt"
                    : (StrideRttPresenter.DeviceReady ? "Stride GPU ready" : "Stride GPU: no");
                return Presenter.ActivePath + " · " + Presenter.FrameCount + " frames · " +
                    Presenter.Width + "×" + Presenter.Height + " · " + gpu +
                    " · #2741 open";
            }
        }

        public string StatusText
        {
            get { return OverlayText + "\n" + Result.StatusText; }
        }
    }
}
