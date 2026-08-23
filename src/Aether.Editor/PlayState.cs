// Copyright 2026 Resolvora LLC / Aether Engine contributors.
// Editor play-in-editor state. Maps to IGameEngineProxy UpdateType:
// Stopped → Editing, Playing → GamePlay, Paused → Paused.

namespace Aether.Editor
{
    /// <summary>
    /// Play / Pause / Stop for the bound Level world. Not #2741 Game
    /// control and not a physics or character-controller loop.</summary>
    public enum PlayState
    {
        /// <summary>Editing. Engine ticks with <see cref="LevelEditorCore.UpdateType.Editing"/>.</summary>
        Stopped,

        /// <summary>Live. Engine ticks with <see cref="LevelEditorCore.UpdateType.GamePlay"/>.</summary>
        Playing,

        /// <summary>Frozen. Engine ticks with <see cref="LevelEditorCore.UpdateType.Paused"/> (delta ~0).</summary>
        Paused
    }
}
