// Copyright 2026 Resolvora LLC / Aether Engine contributors.
// Translate / rotate / scale tool mode for the Viewport gizmo. CPU only.

namespace LevelEditorCore
{
    /// <summary>
    /// Which transform gizmo the Viewport draws and hit-tests.
    /// Headless APIs (<c>BeginAxisDrag</c> / <c>BeginRotateDrag</c> /
    /// <c>BeginScaleDrag</c>) stay explicit and do not depend on this.</summary>
    public enum GizmoMode
    {
        Translate = 0,
        Rotate = 1,
        Scale = 2
    }
}
