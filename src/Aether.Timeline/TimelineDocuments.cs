//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// DomXmlReader / DomXmlWriter helpers for TimelineEditor documents, plus
// testdata path lookup and add-interval used by the Avalonia shell and
// headless proof.

using System;
using System.IO;
using System.Reflection;
using System.Xml;

using Sce.Atf.Adaptation;
using Sce.Atf.Controls.Timelines;
using Sce.Atf.Dom;

using TimelineEditorSample;
using TimelineEditorSample.DomNodeAdapters;

namespace Aether.Timeline
{
    /// <summary>
    /// Shared TimelineEditor document construction and DomXml I/O.</summary>
    public static class TimelineDocuments
    {
        public const string SampleDocumentFileName = "100.timeline";
        public const string SchemaFileName = "timeline.xsd";
        public const string Namespace = "timeline";

        /// <summary>
        /// Expected counts in the committed ATF 100.timeline fixture.</summary>
        public const int ExampleGroupCount = 3;
        public const int ExampleTrackCount = 10;
        public const int ExampleIntervalCount = 60;
        public const int ExampleMarkerCount = 4;

        public static string FindSchemaPath()
        {
            return FindTimelineFile(SchemaFileName);
        }

        public static string FindSampleDocumentPath()
        {
            return FindTimelineFile(SampleDocumentFileName);
        }

        public static string FindTimelineTestdataDirectory()
        {
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (!string.IsNullOrEmpty(dir))
            {
                string candidate = Path.Combine(dir, "testdata", "atf", "TimelineEditor");
                if (File.Exists(Path.Combine(candidate, SchemaFileName)))
                    return Path.GetFullPath(candidate);
                dir = Path.GetDirectoryName(dir);
            }

            string cwd = Path.Combine(Directory.GetCurrentDirectory(), "testdata", "atf", "TimelineEditor");
            if (File.Exists(Path.Combine(cwd, SchemaFileName)))
                return Path.GetFullPath(cwd);

            return null;
        }

        /// <summary>
        /// True when the path is a TimelineEditor document (.timeline or timeline XML root).</summary>
        public static bool IsTimelineDocument(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

            if (string.Equals(Path.GetExtension(path), ".timeline", StringComparison.OrdinalIgnoreCase))
                return true;

            try
            {
                using (var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true }))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType != XmlNodeType.Element)
                            continue;
                        return reader.LocalName == "timeline" &&
                            (reader.NamespaceURI == Namespace || string.IsNullOrEmpty(reader.NamespaceURI));
                    }
                }
            }
            catch (XmlException)
            {
                return false;
            }

            return false;
        }

        public static void WriteXml(DomNode document, Stream stream, Uri uri, XmlSchemaTypeCollection typeCollection)
        {
            var writer = new DomXmlWriter(typeCollection);
            writer.Write(document, stream, uri);
        }

        public static void WriteXml(DomNode document, string path, XmlSchemaTypeCollection typeCollection)
        {
            string full = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using (var stream = File.Create(full))
                WriteXml(document, stream, new Uri(full), typeCollection);
        }

        public static DomNode ReadXml(string path, XmlSchemaTypeLoader loader)
        {
            string full = Path.GetFullPath(path);
            using (var stream = File.OpenRead(full))
            {
                var reader = new DomXmlReader(loader);
                return reader.Read(stream, new Uri(full));
            }
        }

        public static DomNode LoadExample(SchemaLoader loader)
        {
            string path = FindSampleDocumentPath();
            if (path == null)
                throw new InvalidOperationException("Could not find testdata/atf/TimelineEditor/100.timeline");
            return ReadXml(path, loader);
        }

        public static Interval FindInterval(DomNode document, string name)
        {
            TimelineEditorSample.DomNodeAdapters.Timeline timeline =
                document.Cast<TimelineEditorSample.DomNodeAdapters.Timeline>();
            foreach (IGroup group in timeline.Groups)
            {
                foreach (ITrack track in group.Tracks)
                {
                    foreach (IInterval interval in track.Intervals)
                    {
                        Interval typed = interval as Interval;
                        if (typed != null && string.Equals(typed.Name, name, StringComparison.Ordinal))
                            return typed;
                    }
                }
            }
            return null;
        }

        public static int CountTracks(DomNode document)
        {
            int count = 0;
            TimelineEditorSample.DomNodeAdapters.Timeline timeline =
                document.Cast<TimelineEditorSample.DomNodeAdapters.Timeline>();
            foreach (IGroup group in timeline.Groups)
                count += group.Tracks.Count;
            return count;
        }

        public static int CountIntervals(DomNode document)
        {
            int count = 0;
            TimelineEditorSample.DomNodeAdapters.Timeline timeline =
                document.Cast<TimelineEditorSample.DomNodeAdapters.Timeline>();
            foreach (IGroup group in timeline.Groups)
            {
                foreach (ITrack track in group.Tracks)
                    count += track.Intervals.Count;
            }
            return count;
        }

        /// <summary>
        /// Adds an interval to the first track (or creates a group/track if needed).</summary>
        public static Interval AddInterval(DomNode document, string name, float start, float length)
        {
            TimelineEditorSample.DomNodeAdapters.Timeline timeline =
                document.Cast<TimelineEditorSample.DomNodeAdapters.Timeline>();
            ITrack track = FirstTrack(timeline);
            if (track == null)
            {
                IGroup group = timeline.CreateGroup();
                group.Name = "Group";
                timeline.Groups.Add(group);
                track = group.CreateTrack();
                track.Name = "Track";
                group.Tracks.Add(track);
            }

            IInterval interval = track.CreateInterval();
            interval.Name = name;
            interval.Start = start;
            interval.Length = length;
            track.Intervals.Add(interval);
            return interval.Cast<Interval>();
        }

        private static ITrack FirstTrack(TimelineEditorSample.DomNodeAdapters.Timeline timeline)
        {
            foreach (IGroup group in timeline.Groups)
            {
                foreach (ITrack track in group.Tracks)
                    return track;
            }
            return null;
        }

        private static string FindTimelineFile(string fileName)
        {
            string nextToExe = Path.Combine(AppContext.BaseDirectory, fileName);
            if (File.Exists(nextToExe))
                return Path.GetFullPath(nextToExe);

            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (!string.IsNullOrEmpty(dir))
            {
                string candidate = Path.Combine(dir, "testdata", "atf", "TimelineEditor", fileName);
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
                dir = Path.GetDirectoryName(dir);
            }

            string cwd = Path.Combine(Directory.GetCurrentDirectory(), "testdata", "atf", "TimelineEditor", fileName);
            if (File.Exists(cwd))
                return Path.GetFullPath(cwd);

            return null;
        }
    }
}
