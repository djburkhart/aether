//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// IListable was omitted — Sce.Atf.Applications.ItemInfo / ImageList are WinForms
// tree-lister types and are not in Aether.Atf.Core. Hierarchy display is built
// by walking IGameObjectFolder / IGameObjectGroup.

namespace LevelEditorCore
{   
    /// <summary>
    /// Interface for game objects</summary>
    public interface IGameObject : ITransformable, INameable, IVisible, ILockable
    { 
         /// <summary>  
         /// Gets the game that owns this game object.</summary>  
         /// <returns>The game that owns this game object, or null if this object isn't owned.</returns>  
         IGame GetGame();
    }
}
