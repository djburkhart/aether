//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// IGameLoop (Update/Render of viewports) was not ported. FrameTime stays so
// IGameEngineProxy.Update can be called by a no-op engine without a renderer.

namespace LevelEditorCore
{
    /// <summary>
    /// Used by Render loop service to pass total and elasped time 
    /// to RenderLoop client.    
    /// </summary>
    public struct FrameTime
    {        
        /// <summary>
        /// construct FrameTime</summary>        
        public FrameTime(double totalTime, float elapsedTime)
        {
            TotalTime = totalTime;
            ElapsedTime = elapsedTime;
        }

        /// <summary>
        /// Gets total simulation time in seconds</summary>
        public readonly double TotalTime;
        
        /// <summary>
        /// Gets elapsed time in seconds.
        /// since last update</summary>
        public readonly float ElapsedTime;                
    }
}
