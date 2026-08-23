// Copyright 2026 Resolvora LLC / Aether Engine contributors.
// Stride-backed IGameEngineProxy. SetGameWorld walks IGameObjectFolder /
// IGameObject and creates Entity placeholders. Used only when a
// GraphicsDevice exists; otherwise the editor keeps NullGameEngine.

using System;
using System.Collections.Generic;

using LevelEditorCore;

using Sce.Atf.Adaptation;
using Sce.Atf.VectorMath;

using Stride.Core.Mathematics;
using Stride.Engine;

namespace Aether.Stride
{
    /// <summary>
    /// Level backend that mirrors GameObjects as Stride scene entities.
    /// Does not load asset files, run physics, or pick. Rendering stays on
    /// <see cref="StrideRttPresenter"/> (same Image present path).</summary>
    public sealed class StrideGameEngine : IGameEngineProxy
    {
        public const string BackendName = BoundLevelScene.StrideBackend;

        public StrideGameEngine()
        {
            Info = new EngineInfo();
            Scene = new Scene { Name = "AetherLevel" };
        }

        /// <inheritdoc/>
        public EngineInfo Info { get; }

        /// <summary>Stride scene holding one Entity per GameObject.</summary>
        public Scene Scene { get; }

        public int EntityCount
        {
            get { return m_entities.Count; }
        }

        /// <summary>
        /// Stride engine when <see cref="StrideRttPresenter.DeviceReady"/>;
        /// otherwise <see cref="NullGameEngine.Instance"/>.</summary>
        public static IGameEngineProxy CreateOrFallback()
        {
            try
            {
                if (StrideRttPresenter.DeviceReady)
                    return new StrideGameEngine();
            }
            catch (Exception)
            {
            }
            return NullGameEngine.Instance;
        }

        public bool HasEntity(string name)
        {
            return FindEntity(name) != null;
        }

        public Entity FindEntity(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            Entity match;
            return m_byName.TryGetValue(name, out match) ? match : null;
        }

        /// <inheritdoc/>
        public void SetGameWorld(IGame game)
        {
            m_game = game;
            Rebuild();
        }

        /// <inheritdoc/>
        public void Update(FrameTime time, UpdateType updateType)
        {
            if (m_game == null)
                return;
            // Names / TRS can change between rebuilds; refresh in place.
            SyncTransforms();
        }

        /// <inheritdoc/>
        public void WaitForPendingResources()
        {
        }

        private void Rebuild()
        {
            ClearEntities();
            if (m_game == null || m_game.RootGameObjectFolder == null)
                return;
            WalkFolder(m_game.RootGameObjectFolder, null);
        }

        private void WalkFolder(IGameObjectFolder folder, Entity parent)
        {
            if (folder == null)
                return;
            foreach (IGameObject gob in folder.GameObjects)
                WalkObject(gob, parent);
            foreach (IGameObjectFolder sub in folder.GameObjectFolders)
                WalkFolder(sub, parent);
        }

        private void WalkObject(IGameObject gob, Entity parent)
        {
            if (gob == null)
                return;
            Entity entity = new Entity(gob.Name ?? string.Empty);
            ApplyLocal(entity, gob);
            if (parent != null)
                entity.SetParent(parent);
            else
                Scene.Entities.Add(entity);

            m_entities.Add(entity);
            m_sources.Add(gob);
            RememberName(entity);

            IGameObjectGroup group = gob.As<IGameObjectGroup>();
            if (group == null)
                return;
            foreach (IGameObject child in group.GameObjects)
                WalkObject(child, entity);
        }

        private void SyncTransforms()
        {
            m_byName.Clear();
            int count = Math.Min(m_entities.Count, m_sources.Count);
            for (int i = 0; i < count; i++)
            {
                IGameObject gob = m_sources[i];
                Entity entity = m_entities[i];
                if (gob == null || entity == null)
                    continue;
                entity.Name = gob.Name ?? string.Empty;
                ApplyLocal(entity, gob);
                RememberName(entity);
            }
        }

        private void RememberName(Entity entity)
        {
            if (entity == null || string.IsNullOrEmpty(entity.Name))
                return;
            if (!m_byName.ContainsKey(entity.Name))
                m_byName.Add(entity.Name, entity);
        }

        private static void ApplyLocal(Entity entity, IGameObject gob)
        {
            Vec3F t = gob.Translation;
            Vec3F r = gob.Rotation;
            Vec3F s = gob.Scale;
            entity.Transform.Position = new Vector3(t.X, t.Y, t.Z);
            entity.Transform.RotationEulerXYZ = new Vector3(r.X, r.Y, r.Z);
            float sx = s.X != 0f ? s.X : 1f;
            float sy = s.Y != 0f ? s.Y : 1f;
            float sz = s.Z != 0f ? s.Z : 1f;
            entity.Transform.Scale = new Vector3(sx, sy, sz);
        }

        private void ClearEntities()
        {
            foreach (Entity entity in m_entities)
            {
                try
                {
                    if (entity != null)
                        entity.SetParent(null);
                }
                catch (Exception)
                {
                }
            }
            Scene.Entities.Clear();
            m_entities.Clear();
            m_sources.Clear();
            m_byName.Clear();
        }

        private IGame m_game;
        private readonly List<Entity> m_entities = new List<Entity>();
        private readonly List<IGameObject> m_sources = new List<IGameObject>();
        private readonly Dictionary<string, Entity> m_byName =
            new Dictionary<string, Entity>(StringComparer.Ordinal);
    }
}
