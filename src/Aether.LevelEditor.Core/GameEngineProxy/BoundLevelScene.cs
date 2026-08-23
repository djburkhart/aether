// Copyright 2026 Resolvora LLC / Aether Engine contributors.
// CPU-side Level scene snapshot. Walks IGameObjectFolder / IGameObject so
// a bound world exists even when NullGameEngine is the IGameEngineProxy
// (ubuntu CI / no GraphicsDevice). Not a renderer.

using System;
using System.Collections.Generic;

using Sce.Atf.Adaptation;
using Sce.Atf.VectorMath;

namespace LevelEditorCore
{
    /// <summary>
    /// One GameObject mirrored into the bound scene.</summary>
    public sealed class BoundSceneObject
    {
        public BoundSceneObject(
            string name,
            Vec3F translation,
            Vec3F rotation,
            Vec3F scale,
            Vec3F worldTranslation)
        {
            Name = name ?? string.Empty;
            Translation = translation;
            Rotation = rotation;
            Scale = scale;
            WorldTranslation = worldTranslation;
        }

        public string Name { get; }

        /// <summary>Local ITransformable.Translation.</summary>
        public Vec3F Translation { get; }

        public Vec3F Rotation { get; }

        public Vec3F Scale { get; }

        /// <summary>World translation (parent groups applied).</summary>
        public Vec3F WorldTranslation { get; }
    }

    /// <summary>
    /// Snapshot of the loaded IGame as named placeholder objects.
    /// Always available; does not require a GPU device.</summary>
    public sealed class BoundLevelScene
    {
        public const string BoundBackend = "bound";
        public const string StrideBackend = "stride";

        public BoundLevelScene()
        {
            m_objects = new List<BoundSceneObject>();
            Backend = BoundBackend;
        }

        /// <summary>
        /// <see cref="StrideBackend"/> when a Stride IGameEngineProxy owns the
        /// world; otherwise <see cref="BoundBackend"/>.</summary>
        public string Backend { get; private set; }

        public int Count
        {
            get { return m_objects.Count; }
        }

        public IReadOnlyList<BoundSceneObject> Objects
        {
            get { return m_objects; }
        }

        public BoundSceneObject Find(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            foreach (BoundSceneObject obj in m_objects)
            {
                if (string.Equals(obj.Name, name, StringComparison.Ordinal))
                    return obj;
            }
            return null;
        }

        public bool Contains(string name)
        {
            return Find(name) != null;
        }

        /// <summary>
        /// Nearest placeholder under an image-space pixel. Uses
        /// <see cref="ViewportSceneCamera"/> (same LookAt/perspective as RTT).
        /// Null when the ray misses every cube AABB.</summary>
        public BoundSceneObject PickAt(float pixelX, float pixelY, int width, int height)
        {
            return ViewportSceneCamera.PickAtPixel(this, pixelX, pixelY, width, height);
        }

        /// <summary>
        /// Nearest placeholder under NDC (−1..1, y up). Aspect is width/height
        /// of the same buffer the RTT presenter uses.</summary>
        public BoundSceneObject PickAtNdc(float ndcX, float ndcY, float aspect)
        {
            return ViewportSceneCamera.PickAtNdc(this, ndcX, ndcY, aspect);
        }

        /// <summary>Rebuild from <paramref name="game"/>. Null game clears the scene.</summary>
        public void SyncFrom(IGame game, string backend)
        {
            m_objects.Clear();
            Backend = string.IsNullOrEmpty(backend) ? BoundBackend : backend;
            if (game == null || game.RootGameObjectFolder == null)
                return;
            WalkFolder(game.RootGameObjectFolder);
        }

        private void WalkFolder(IGameObjectFolder folder)
        {
            if (folder == null)
                return;
            foreach (IGameObject gob in folder.GameObjects)
                WalkObject(gob);
            foreach (IGameObjectFolder sub in folder.GameObjectFolders)
                WalkFolder(sub);
        }

        private void WalkObject(IGameObject gob)
        {
            if (gob == null)
                return;
            m_objects.Add(Capture(gob));
            IGameObjectGroup group = gob.As<IGameObjectGroup>();
            if (group == null)
                return;
            foreach (IGameObject child in group.GameObjects)
                WalkObject(child);
        }

        private static BoundSceneObject Capture(IGameObject gob)
        {
            Vec3F translation = gob.Translation;
            Vec3F rotation = gob.Rotation;
            Vec3F scale = gob.Scale;
            Vec3F world = translation;
            try
            {
                Matrix4F matrix = TransformUtils.ComputeWorldTransform(gob);
                if (matrix != null)
                    world = matrix.Translation;
            }
            catch (Exception)
            {
                world = translation;
            }
            return new BoundSceneObject(gob.Name, translation, rotation, scale, world);
        }

        private readonly List<BoundSceneObject> m_objects;
    }
}
