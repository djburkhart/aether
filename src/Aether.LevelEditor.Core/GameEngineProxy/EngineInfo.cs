//Copyright © 2015 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Added a parameterless constructor and EmptyEngineInfoXml so a no-op
// IGameEngineProxy can sit behind adapters without a native engine dump.

using System;

namespace LevelEditorCore
{
    /// <summary>
    /// Provide access to game engine information.</summary>
    public class EngineInfo
    {
        /// <summary>
        /// Minimal engine-info XML with no supported resource types.
        /// Used by <see cref="NullGameEngine"/>.</summary>
        public const string EmptyEngineInfoXml = "<EngineInfo><SupportedResources/></EngineInfo>";

        /// <summary>
        /// Construct an empty EngineInfo (no native resource types).</summary>
        public EngineInfo()
            : this(EmptyEngineInfoXml)
        {
        }

        /// <summary>
        /// Construct new instance of EngineInfo 
        /// by parsing engineInfoStr.</summary>
        /// <param name="engineInfoStr"></param>
        public EngineInfo(string engineInfoStr)
        {

            if (string.IsNullOrWhiteSpace(engineInfoStr))
                throw new ArgumentNullException("engineInfoStr");
            ResourceInfos = new ResourcesInfos(engineInfoStr);
        }

        /// <summary>
        /// Information about all the supported assets types</summary>
        public readonly ResourcesInfos ResourceInfos;
       
    }
}
