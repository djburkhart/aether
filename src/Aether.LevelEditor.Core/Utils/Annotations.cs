//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Only ReferenceConstraint (resource-ref file-extension tags) is ported.
// Display-shape annotations used by native rendering were omitted.

namespace LevelEditorCore
{
    /// <summary>
    /// Schema annotations</summary>
    public static class Annotations
    {
       
        /// <summary>
        /// This annotation can be applied to any schema type derived from resourceReferenceType.
        /// To constrain type of resource that this can reference.</summary>
        public static class ReferenceConstraint
        {
            // name of the annotation.
            public const string Name = "sce.atf.referenceConstraint";

            // attributes
            public const string ValidResourceFileExts = "validResourceFileExts";
            public const string ResourceType = "resourceType";
        }
    }
}
