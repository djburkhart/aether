using System;

namespace Aether.Scripting
{
    /// <summary>Outcome of one script run.</summary>
    public sealed class ScriptResult
    {
        public ScriptResult(bool succeeded, string output, Exception error = null)
        {
            Succeeded = succeeded;
            Output = output ?? string.Empty;
            Error = error;
        }

        public bool Succeeded { get; }

        public string Output { get; }

        public Exception Error { get; }

        public static ScriptResult Ok(string output)
        {
            return new ScriptResult(true, output, null);
        }

        public static ScriptResult Fail(string message, Exception error = null)
        {
            return new ScriptResult(false, message ?? string.Empty, error);
        }
    }
}
