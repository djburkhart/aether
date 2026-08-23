using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

using Aether.Timeline;

using Sce.Atf;
using Sce.Atf.Adaptation;
using Sce.Atf.Applications;
using Sce.Atf.Controls.Timelines;
using Sce.Atf.Dom;

using TimelineEditorSample.DomNodeAdapters;

namespace Aether.Editor
{
    /// <summary>
    /// TimelineEditor document session: schema, 100.timeline load, Open/Save,
    /// selection, and HistoryContext. The Avalonia timeline view binds here.</summary>
    public sealed class TimelineSession : INotifyPropertyChanged
    {
        public TimelineSession()
        {
            string schemaPath = TimelineDocuments.FindSchemaPath();
            if (schemaPath == null)
                throw new InvalidOperationException("Could not find testdata/atf/TimelineEditor/timeline.xsd");

            SchemaPath = schemaPath;
            Loader = new Aether.Timeline.SchemaLoader(schemaPath);
            Rows = new ObservableCollection<TimelineRowItem>();
            Intervals = new ObservableCollection<TimelineIntervalItem>();
            LoadExample();
        }

        public string SchemaPath { get; }

        public Aether.Timeline.SchemaLoader Loader { get; }

        public DomNode Document { get; private set; } = null!;

        public TimelineEditorSample.DomNodeAdapters.Timeline Timeline { get; private set; } = null!;

        public HistoryContext History { get; private set; } = null!;

        public SelectionContext Selection { get; private set; } = null!;

        public ObservableCollection<TimelineRowItem> Rows { get; }

        public ObservableCollection<TimelineIntervalItem> Intervals { get; }

        public string? FilePath
        {
            get { return m_filePath; }
            private set
            {
                if (m_filePath == value)
                    return;
                m_filePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        public bool CanSave
        {
            get { return m_filePath != null; }
        }

        public bool IsDirty
        {
            get { return History != null && History.Dirty; }
        }

        public string WindowTitle
        {
            get
            {
                string name = m_filePath != null ? Path.GetFileName(m_filePath) : "timeline";
                return IsDirty ? name + " *" : name;
            }
        }

        public TimelineIntervalItem? SelectedInterval
        {
            get { return m_selectedInterval; }
            set
            {
                if (m_selectedInterval == value)
                    return;
                m_selectedInterval = value;
                OnPropertyChanged();

                if (value != null)
                    Selection.Selection.SetRange(new object[] { value.Interval.DomNode });
                else
                    Selection.Selection.Clear();

                OnPropertyChanged(nameof(StatusText));
                GraphChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string StatusText
        {
            get
            {
                string doc = m_filePath != null ? Path.GetFileName(m_filePath) : "100.timeline";
                if (m_selectedInterval == null)
                    return doc + (IsDirty ? "*" : string.Empty) + " — " + Intervals.Count + " intervals, " + TrackCount + " tracks";
                return doc + (IsDirty ? "*" : string.Empty) + " — " + m_selectedInterval.Display;
            }
        }

        public int TrackCount
        {
            get { return TimelineDocuments.CountTracks(Document); }
        }

        public bool CanUndo
        {
            get { return History != null && History.CanUndo; }
        }

        public bool CanRedo
        {
            get { return History != null && History.CanRedo; }
        }

        public string UndoText
        {
            get
            {
                return History != null && History.CanUndo
                    ? "Undo " + History.UndoDescription
                    : "Undo";
            }
        }

        public string RedoText
        {
            get
            {
                return History != null && History.CanRedo
                    ? "Redo " + History.RedoDescription
                    : "Redo";
            }
        }

        public event EventHandler? GraphChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void LoadExample()
        {
            BindDocument(TimelineDocuments.LoadExample(Loader), null);
        }

        public void Open(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is required.", nameof(path));
            BindDocument(TimelineDocuments.ReadXml(path, Loader), Path.GetFullPath(path));
        }

        public void Save()
        {
            if (m_filePath == null)
                throw new InvalidOperationException("No file path; use Save As.");
            SaveAs(m_filePath);
        }

        public void SaveAs(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is required.", nameof(path));
            if (Loader.TypeCollection == null)
                throw new InvalidOperationException("Schema type collection is not loaded.");

            TimelineDocuments.WriteXml(Document, path, Loader.TypeCollection);
            FilePath = Path.GetFullPath(path);
            History.Dirty = false;
            NotifyFileState();
        }

        public void Undo()
        {
            if (History.CanUndo)
                History.Undo();
            ReloadGraph();
            NotifyHistoryCommands();
        }

        public void Redo()
        {
            if (History.CanRedo)
                History.Redo();
            ReloadGraph();
            NotifyHistoryCommands();
        }

        public TimelineIntervalItem? Find(string name)
        {
            foreach (TimelineIntervalItem item in Intervals)
            {
                if (item.Name == name)
                    return item;
            }
            return null;
        }

        /// <summary>
        /// Adds one interval on the first track — enough to prove insert in this slice.</summary>
        public Interval AddInterval()
        {
            string name = UniqueIntervalName("Interval");
            History.DoTransaction(
                () => TimelineDocuments.AddInterval(Document, name, 42, 5),
                "Add Interval");
            ReloadGraph();
            TimelineIntervalItem? item = Find(name);
            if (item != null)
                SelectedInterval = item;
            NotifyHistoryCommands();
            NotifyFileState();
            return TimelineDocuments.FindInterval(Document, name)!;
        }

        private void BindDocument(DomNode document, string? filePath)
        {
            UnhookHistory();

            Document = document;
            Timeline = document.Cast<TimelineEditorSample.DomNodeAdapters.Timeline>();
            History = document.Cast<HistoryContext>();
            Selection = document.Cast<SelectionContext>();
            m_filePath = filePath;
            History.Dirty = false;
            HookHistory();

            m_selectedInterval = null;
            ReloadGraph();
            OnPropertyChanged(nameof(Document));
            OnPropertyChanged(nameof(Timeline));
            OnPropertyChanged(nameof(History));
            OnPropertyChanged(nameof(Selection));
            OnPropertyChanged(nameof(FilePath));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(SelectedInterval));
            NotifyFileState();
            NotifyHistoryCommands();
        }

        private void ReloadGraph()
        {
            string? selectedName = m_selectedInterval != null ? m_selectedInterval.Name : null;
            Rows.Clear();
            Intervals.Clear();

            int row = 0;
            foreach (IGroup group in Timeline.Groups)
            {
                Rows.Add(new TimelineRowItem(group.Name ?? "Group", true, row));
                row++;
                if (!group.Expanded)
                    continue;
                foreach (ITrack track in group.Tracks)
                {
                    Rows.Add(new TimelineRowItem(track.Name ?? "Track", false, row));
                    foreach (IInterval interval in track.Intervals)
                    {
                        Interval? typed = interval as Interval;
                        if (typed == null)
                            continue;
                        Intervals.Add(new TimelineIntervalItem(typed, row));
                    }
                    row++;
                }
            }

            TimelineIntervalItem? match = selectedName != null ? Find(selectedName) : null;
            m_selectedInterval = match;
            OnPropertyChanged(nameof(SelectedInterval));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(TrackCount));
            GraphChanged?.Invoke(this, EventArgs.Empty);
        }

        private void HookHistory()
        {
            History.History.CommandDone += OnHistoryChanged;
            History.History.CommandUndone += OnHistoryChanged;
            History.DirtyChanged += OnDirtyChanged;
            m_historyHooked = true;
        }

        private void UnhookHistory()
        {
            if (!m_historyHooked)
                return;
            History.History.CommandDone -= OnHistoryChanged;
            History.History.CommandUndone -= OnHistoryChanged;
            History.DirtyChanged -= OnDirtyChanged;
            m_historyHooked = false;
        }

        private void OnHistoryChanged(object? sender, EventArgs e)
        {
            ReloadGraph();
            NotifyHistoryCommands();
            NotifyFileState();
        }

        private void OnDirtyChanged(object? sender, EventArgs e)
        {
            NotifyFileState();
        }

        private void NotifyHistoryCommands()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(UndoText));
            OnPropertyChanged(nameof(RedoText));
        }

        private void NotifyFileState()
        {
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(StatusText));
        }

        private string UniqueIntervalName(string prefix)
        {
            var namer = new UniqueNamer();
            foreach (TimelineIntervalItem item in Intervals)
                namer.Name(item.Name);
            return namer.Name(prefix);
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private TimelineIntervalItem? m_selectedInterval;
        private string? m_filePath;
        private bool m_historyHooked;
    }

    public sealed class TimelineRowItem
    {
        public TimelineRowItem(string name, bool isGroup, int row)
        {
            Name = name;
            IsGroup = isGroup;
            Row = row;
        }

        public string Name { get; }

        public bool IsGroup { get; }

        public int Row { get; }
    }

    public sealed class TimelineIntervalItem
    {
        public TimelineIntervalItem(Interval interval, int row)
        {
            Interval = interval;
            Row = row;
        }

        public Interval Interval { get; }

        public int Row { get; }

        public string Name
        {
            get { return Interval.Name ?? string.Empty; }
        }

        public float Start
        {
            get { return Interval.Start; }
        }

        public float Length
        {
            get { return Interval.Length; }
        }

        public int ColorArgb
        {
            get { return Interval.Color.ToArgb(); }
        }

        public string Display
        {
            get { return Name + "  ·  " + Start + "+" + Length; }
        }
    }
}
