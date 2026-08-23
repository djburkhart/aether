//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// IListable omitted (ItemInfo / ImageList are WinForms).

using System.Collections.Generic;

namespace LevelEditorCore
{
    /// <summary>
    /// Interface for game object folders</summary>
    public interface IGameObjectFolder : IHierarchical, INameable, IVisible, ILockable
    {
        /// <summary>
        /// Gets the list of game objects</summary>
        IList<IGameObject> GameObjects
        {
            get;
        }
         

        /// <summary>
        /// Get the list of child folders</summary>
        IList<IGameObjectFolder> GameObjectFolders
        {
            get;
        }
    }
}
