//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.

using System.Collections.Generic;

namespace Sce.Atf.Controls.Timelines
{
    /// <summary>
    /// Interface for groups, which contain zero or more tracks and can be expanded or collapsed
    /// in a timeline viewing control</summary>
    public interface IGroup : ITimelineObject
    {
        /// <summary>
        /// Gets and sets the group name</summary>
        string Name { get; set; }

        /// <summary>
        /// Gets and sets whether or not the group is expanded</summary>
        bool Expanded { get; set; }

        /// <summary>
        /// Gets the timeline that contains the group</summary>
        ITimeline Timeline { get; }

        /// <summary>
        /// Creates a new track. Does not add the track to this group.</summary>
        ITrack CreateTrack();

        /// <summary>
        /// Gets a list of all tracks in the group</summary>
        IList<ITrack> Tracks { get; }
    }
}
