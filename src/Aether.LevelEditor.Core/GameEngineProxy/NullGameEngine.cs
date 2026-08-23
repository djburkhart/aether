//Copyright © 2015 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// No-op IGameEngineProxy so GameObject adapters can sit behind the engine
// cut line. Does not load assets, update subsystems, or pretend a renderer exists.

namespace LevelEditorCore
{
    /// <summary>
    /// No-op game engine used when LevelEditor runs without LvEdRenderingEngine.
    /// SetGameWorld / Update / WaitForPendingResources do nothing.</summary>
    public sealed class NullGameEngine : IGameEngineProxy
    {
        /// <summary>
        /// Shared instance with empty resource-type info.</summary>
        public static readonly NullGameEngine Instance = new NullGameEngine();

        /// <summary>
        /// Constructor</summary>
        public NullGameEngine()
        {
            Info = new EngineInfo();
        }

        /// <inheritdoc/>
        public EngineInfo Info { get; }

        /// <inheritdoc/>
        public void SetGameWorld(IGame game)
        {
        }

        /// <inheritdoc/>
        public void Update(FrameTime time, UpdateType updateType)
        {
        }

        /// <inheritdoc/>
        public void WaitForPendingResources()
        {
        }
    }
}
