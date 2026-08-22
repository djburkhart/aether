//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.

using System;
using System.Collections.Generic;

using Sce.Atf.Adaptation;
using Sce.Atf.Controls.Timelines;
using Sce.Atf.Dom;

namespace TimelineEditorSample.DomNodeAdapters
{
    /// <summary>
    /// Adapts DomNode to a group of tracks</summary>
    public class Group : DomNodeAdapter, IGroup, ICloneable
    {
        /// <summary>
        /// Gets and sets the group name</summary>
        public string Name
        {
            get { return (string)DomNode.GetAttribute(Schema.groupType.nameAttribute); }
            set { DomNode.SetAttribute(Schema.groupType.nameAttribute, value); }
        }

        /// <summary>
        /// Gets and sets whether the group is expanded</summary>
        public bool Expanded
        {
            get { return (bool)DomNode.GetAttribute(Schema.groupType.expandedAttribute); }
            set { DomNode.SetAttribute(Schema.groupType.expandedAttribute, value); }
        }

        /// <summary>
        /// Gets the timeline that contains the group</summary>
        public ITimeline Timeline
        {
            get { return GetParentAs<Timeline>(); }
        }

        /// <summary>
        /// Creates a new track. Does not add the track to this group.</summary>
        public ITrack CreateTrack()
        {
            return new DomNode(Schema.trackType.Type).As<ITrack>();
        }

        /// <summary>
        /// Gets the list of all tracks in the group</summary>
        public IList<ITrack> Tracks
        {
            get { return GetChildList<ITrack>(Schema.groupType.trackChild); }
        }

        /// <summary>
        /// Copies this timeline object</summary>
        public virtual object Clone()
        {
            DomNode domCopy = DomNode.Copy(new DomNode[] { DomNode })[0];
            return domCopy.As<ITimelineObject>();
        }
    }
}
