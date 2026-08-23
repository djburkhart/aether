//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.

namespace Sce.Atf.Controls.Timelines
{
    /// <summary>
    /// Interface for intervals, which are zero or greater length events on a track</summary>
    public interface IInterval : IEvent
    {
        /// <summary>
        /// Gets the track containing this interval</summary>
        ITrack Track { get; }
    }

    /// <summary>
    /// Useful static and extension methods for IInterval objects</summary>
    public static class Intervals
    {
        /// <summary>
        /// Sets the interval's track</summary>
        public static void SetTrack(this IInterval interval, ITrack newTrack)
        {
            ITrack currentTrack = interval.Track;
            if (currentTrack != null)
                currentTrack.Intervals.Remove(interval);
            if (newTrack != null)
                newTrack.Intervals.Add(interval);
        }
    }
}
