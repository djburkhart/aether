//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.

using System.Collections.Generic;

namespace Sce.Atf.Controls.Timelines
{
    /// <summary>
    /// Interface for tracks, which contain zero or more events</summary>
    public interface ITrack : ITimelineObject
    {
        /// <summary>
        /// Gets and sets the track name</summary>
        string Name { get; set; }

        /// <summary>
        /// Gets the group that contains the track</summary>
        IGroup Group { get; }

        /// <summary>
        /// Creates a new interval. Does not add it to this track.</summary>
        IInterval CreateInterval();

        /// <summary>
        /// Gets the list of all intervals in the track</summary>
        IList<IInterval> Intervals { get; }

        /// <summary>
        /// Creates a new key. Does not add it to this track.</summary>
        IKey CreateKey();

        /// <summary>
        /// Gets the list of all keys in the track</summary>
        IList<IKey> Keys { get; }
    }
}
