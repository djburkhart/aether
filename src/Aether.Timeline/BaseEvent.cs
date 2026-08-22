//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.

using System;
using System.Drawing;

using Sce.Atf;
using Sce.Atf.Adaptation;
using Sce.Atf.Controls.Timelines;
using Sce.Atf.Dom;

namespace TimelineEditorSample.DomNodeAdapters
{
    /// <summary>
    /// Class that adapts a DomNode to an event; a base class for adapters for Intervals, Markers, and Keys</summary>
    public class BaseEvent : DomNodeAdapter, IEvent, ICloneable
    {
        /// <summary>
        /// Gets and sets the event's name</summary>
        public virtual string Name
        {
            get { return string.Empty; }
            set { }
        }

        /// <summary>
        /// Gets and sets the event's start time</summary>
        public float Start
        {
            get { return (float)DomNode.GetAttribute(Schema.eventType.startAttribute); }
            set
            {
                float constrained = Math.Max(value, 0);
                constrained = (float)MathUtil.Snap(constrained, 1.0);
                DomNode.SetAttribute(Schema.eventType.startAttribute, constrained);
            }
        }

        /// <summary>
        /// Gets and sets the event's length (duration)</summary>
        public virtual float Length
        {
            get { return 0.0f; }
            set { }
        }

        /// <summary>
        /// Gets and sets the event's color</summary>
        public virtual Color Color
        {
            get { return Color.LimeGreen; }
            set { }
        }

        /// <summary>
        /// Gets and sets the event's user-readable description</summary>
        public string Description
        {
            get { return (string)DomNode.GetAttribute(Schema.eventType.descriptionAttribute); }
            set { DomNode.SetAttribute(Schema.eventType.descriptionAttribute, value); }
        }

        /// <summary>
        /// Copies this timeline object</summary>
        public virtual object Clone()
        {
            DomNode domCopy = DomNode.Copy(new DomNode[] { DomNode })[0];
            return domCopy.As<ITimelineObject>();
        }

        /// <summary>
        /// Returns a string that represents the event</summary>
        public override string ToString()
        {
            string result = DomNode.GetAttribute(Schema.eventType.descriptionAttribute).ToString();
            if (result == string.Empty)
                result = Name;
            return result;
        }
    }
}
