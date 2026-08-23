using System;

using LevelEditorCore;

using Aether.Stride;

namespace Aether.Editor
{
    /// <summary>
    /// Center Viewport session: live presenter + Stride host probe.
    /// After device init, the Level session attaches StrideGameEngine or
    /// keeps NullGameEngine. Orbit / pan / zoom write the shared
    /// <see cref="ViewportCamera"/> that pick and RTT already read.</summary>
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
            m_level = level;
            Presenter.BindLevel(level);
            Presenter.BindEngine(level.Engine);
            Presenter.Tick(0.016);
        }

        public StrideHostResult Result { get; }

        public ViewportPresenter Presenter { get; }

        /// <summary>
        /// Shared orbit camera. Same instance
        /// <see cref="ViewportSceneCamera.Current"/>.</summary>
        public ViewportCamera Camera
        {
            get { return ViewportSceneCamera.Current; }
        }

        /// <summary>Orbit the Viewport camera (radians). Never throws.</summary>
        public bool OrbitBy(float yawRadians, float pitchRadians)
        {
            try
            {
                Camera.OrbitBy(yawRadians, pitchRadians);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Pan the Viewport camera (world units along camera right/up).
        /// Never throws.</summary>
        public bool PanBy(float right, float up)
        {
            try
            {
                Camera.PanBy(right, up);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Zoom the Viewport camera. Positive delta moves farther.
        /// Never throws.</summary>
        public bool ZoomBy(float delta)
        {
            try
            {
                Camera.ZoomBy(delta);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

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
                string play = "play " + Presenter.LastUpdateType;
                if (m_level != null)
                    play = "play " + m_level.PlayState + " · " + m_level.EngineUpdateType;
                return Presenter.ActivePath + " · " + Presenter.FrameCount + " frames · " +
                    Presenter.Width + "×" + Presenter.Height + " · " + gpu +
                    " · " + scene + " · " + play +
                    " · gizmo " + TranslateGizmo.OverlayMode +
                    " · F5 play · F6 pause · Shift+F5 stop · W move · E rotate · R scale · click to pick · drag handle · RMB orbit · MMB pan · wheel zoom · #2741 open";
            }
        }

        public string StatusText
        {
            get { return OverlayText + "\n" + Result.StatusText; }
        }

        private LevelSession? m_level;
    }
}
