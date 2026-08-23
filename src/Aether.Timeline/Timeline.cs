//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Implements ITimeline only. IHierarchicalTimeline / timeline references
// are out of this slice (100.timeline has none).

using System.Collections.Generic;

using Sce.Atf.Adaptation;
using Sce.Atf.Controls.Timelines;
using Sce.Atf.Dom;

namespace TimelineEditorSample.DomNodeAdapters
{
    /// <summary>
    /// Adapts DomNode to a Timeline</summary>
    public class Timeline : DomNodeAdapter, ITimeline
    {
        /// <summary>
        /// Creates a new group</summary>
        public IGroup CreateGroup()
        {
            return new DomNode(Schema.groupType.Type).As<IGroup>();
        }

        /// <summary>
        /// Creates a new marker</summary>
        public IMarker CreateMarker()
        {
            return new DomNode(Schema.markerType.Type).As<IMarker>();
        }

        /// <summary>
        /// Gets the list of all groups in the timeline</summary>
        public IList<IGroup> Groups
        {
            get { return GetChildList<IGroup>(Schema.timelineType.groupChild); }
        }

        /// <summary>
        /// Gets the list of all markers in the timeline</summary>
        public IList<IMarker> Markers
        {
            get { return GetChildList<IMarker>(Schema.timelineType.markerChild); }
        }
    }
}
