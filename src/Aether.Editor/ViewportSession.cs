using Aether.Stride;

namespace Aether.Editor
{
    /// <summary>
    /// Center Viewport session: live presenter + Stride host probe.
    /// After device init, the Level session attaches StrideGameEngine or
    /// keeps NullGameEngine.</summary>
    public sealed class ViewportSession
    {
        public ViewportSession()
        {
            // Attempt RTT first so GraphicsAdapterFactory is still clean.
            // Probe() runs Game.Run and can leave the factory half-initialized.
            Presenter = new ViewportPresenter();
            Presenter.Tick(0);
            Result = StrideHost.Probe();
        }

        /// <summary>
        /// Bind the Level backend into the presenter and tick once so a GPU
        /// path can draw the bound scene instead of the demo cube.</summary>
        public void BindLevel(LevelSession level)
        {
            if (level == null)
                return;
            Presenter.BindEngine(level.Engine);
            Presenter.Tick(0.016);
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
                string scene = StrideRttPresenter.PlaceholderCount > 0
                    ? StrideRttPresenter.PlaceholderCount + " scene objects"
                    : "no level scene";
                return Presenter.ActivePath + " · " + Presenter.FrameCount + " frames · " +
                    Presenter.Width + "×" + Presenter.Height + " · " + gpu +
                    " · " + scene + " · click to pick · #2741 open";
            }
        }

        public string StatusText
        {
            get { return OverlayText + "\n" + Result.StatusText; }
        }
    }
}
