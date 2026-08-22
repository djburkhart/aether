//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.

using System;
using System.Collections.Generic;

using Sce.Atf.Adaptation;
using Sce.Atf.Controls.Timelines;
using Sce.Atf.Dom;

namespace TimelineEditorSample.DomNodeAdapters
{
    /// <summary>
    /// Adapts DomNode to a Track</summary>
    public class Track : DomNodeAdapter, ITrack, ICloneable
    {
        /// <summary>
        /// Gets or sets the track name</summary>
        public string Name
        {
            get { return (string)DomNode.GetAttribute(Schema.trackType.nameAttribute); }
            set { DomNode.SetAttribute(Schema.trackType.nameAttribute, value); }
        }

        /// <summary>
        /// Gets the group that contains the track</summary>
        public IGroup Group
        {
            get { return GetParentAs<Group>(); }
        }

        /// <summary>
        /// Creates a new interval</summary>
        public IInterval CreateInterval()
        {
            return new DomNode(Schema.intervalType.Type).As<IInterval>();
        }

        /// <summary>
        /// Gets the list of all intervals in the track</summary>
        public IList<IInterval> Intervals
        {
            get { return GetChildList<IInterval>(Schema.trackType.intervalChild); }
        }

        /// <summary>
        /// Creates a new key</summary>
        public IKey CreateKey()
        {
            return new DomNode(Schema.keyType.Type).As<IKey>();
        }

        /// <summary>
        /// Gets the list of all keys in the track</summary>
        public IList<IKey> Keys
        {
            get { return GetChildList<IKey>(Schema.trackType.keyChild); }
        }

        /// <summary>
        /// Copies this timeline object</summary>
        public virtual object Clone()
        {
            DomNode domCopy = DomNode.Copy(new DomNode[] { DomNode })[0];
            return domCopy.As<ITimelineObject>();
        }

        /// <summary>
        /// Returns the Name property</summary>
        public override string ToString()
        {
            return Name;
        }
    }
}
