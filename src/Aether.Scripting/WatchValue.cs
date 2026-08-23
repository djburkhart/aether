using System.Collections.Generic;

namespace Aether.Scripting
{
    /// <summary>One watch row shown while paused.</summary>
    public sealed class WatchValue
    {
        public WatchValue(string name, string value)
        {
            Name = name ?? string.Empty;
            Value = value ?? string.Empty;
        }

        public string Name { get; }

        public string Value { get; }

        public override string ToString()
        {
            return Name + " = " + Value;
        }
    }

    /// <summary>Where Run is paused and what the watch pane can show.</summary>
    public sealed class PauseInfo
    {
        public PauseInfo(string languageId, string path, int line, IReadOnlyList<WatchValue> watches)
        {
            LanguageId = languageId ?? string.Empty;
            Path = path ?? string.Empty;
            Line = line;
            Watches = watches ?? new WatchValue[0];
        }

        public string LanguageId { get; }

        public string Path { get; }

        public int Line { get; }

        public IReadOnlyList<WatchValue> Watches { get; }
    }
}
