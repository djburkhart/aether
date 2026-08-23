using System;
using System.Reflection;
using System.Text;

using Stride.Engine;
using Stride.Games;

namespace Aether.Stride
{
    /// <summary>
    /// One-shot Stride host probe. Constructs <see cref="Game"/> and tries
    /// <see cref="GameContextHeadless"/>. Does not claim an Avalonia present
    /// path — stride3d/stride#2741 is still open.</summary>
    public static class StrideHost
    {
        public const string PackageId = "Stride.Engine";
        public const string PackageVersion = "4.4.0-beta5";
        public const string Issue2741 = "https://github.com/stride3d/stride/issues/2741";

        /// <summary>Runs the probe. Safe to call without a display.</summary>
        public static StrideHostResult Probe()
        {
            var result = new StrideHostResult();
            try
            {
                Assembly games = typeof(GameContextHeadless).Assembly;
                Assembly engine = typeof(Game).Assembly;
                result.GamesAssembly = games.GetName().ToString();
                result.EngineAssembly = engine.GetName().ToString();
                result.EngineLoaded = true;
                result.HeadlessContextAvailable = true;

                using (var game = new Game())
                {
                    result.GameConstructed = true;
                    result.GameTypeName = game.GetType().FullName;

                    game.WindowCreated += (s, e) =>
                    {
                        result.WindowCreated = true;
                        result.WindowTypeName = game.Window != null
                            ? game.Window.GetType().FullName
                            : null;
                    };

                    EventHandler started = null;
                    started = (s, e) =>
                    {
                        result.GraphicsDeviceCreated = game.GraphicsDevice != null;
                        game.Exit();
                    };
                    Game.GameStarted += started;
                    try
                    {
                        game.Run(new GameContextHeadless(64, 64));
                        result.HeadlessRunCompleted = true;
                    }
                    catch (Exception ex)
                    {
                        result.PresentError = Flatten(ex);
                        result.PresentBlocker = ClassifyPresentBlocker(ex);
                    }
                    finally
                    {
                        Game.GameStarted -= started;
                    }
                }
            }
            catch (Exception ex)
            {
                result.LoadError = Flatten(ex);
            }

            result.StrideGpuPresent = result.GraphicsDeviceCreated;
            result.PresentPath = DescribePresentPath(result);
            result.StatusText = BuildStatus(result);
            return result;
        }

        private static string ClassifyPresentBlocker(Exception ex)
        {
            string text = Flatten(ex) + Environment.NewLine + ex;
            if (text.IndexOf("vulkan", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("ErrorIncompatibleDriver", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Vulkan instance/device creation failed (no usable GPU/driver on this host). " +
                    "Null graphics backend was removed in Stride 4.4. Headless still needs a real API.";
            }
            if (text.IndexOf("display", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("SDL", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Display/SDL window creation failed.";
            }
            return "Game.Run failed after GameWindowHeadless (no in-pane present; #2741 still open).";
        }

        private static string DescribePresentPath(StrideHostResult result)
        {
            if (!result.EngineLoaded)
                return "Stride.Engine did not load.";
            if (result.HeadlessRunCompleted)
                return "GameContextHeadless Run completed.";
            if (result.WindowCreated)
                return "GameContextHeadless created GameWindowHeadless, then graphics-device init failed.";
            if (result.GameConstructed)
                return "Game constructed; headless Run did not create a window.";
            return "Stride assemblies loaded; Game was not constructed.";
        }

        private static string BuildStatus(StrideHostResult result)
        {
            var text = new StringBuilder();
            text.AppendLine("Stride viewport spike (not WYSIWYG, not play-in-editor).");
            text.AppendLine("Package: " + PackageId + " " + PackageVersion);
            text.AppendLine("Engine loaded: " + result.EngineLoaded);
            text.AppendLine("Game constructed: " + result.GameConstructed);
            text.AppendLine("GameContextHeadless: " + result.HeadlessContextAvailable);
            text.AppendLine("Window: " + (result.WindowTypeName ?? "(none)"));
            text.AppendLine("Stride GPU present: " + result.StrideGpuPresent);
            text.AppendLine("Path: " + result.PresentPath);
            if (!string.IsNullOrEmpty(result.PresentError))
                text.AppendLine("Device error: " + result.PresentError);
            if (!string.IsNullOrEmpty(result.PresentBlocker))
                text.AppendLine("Blocker: " + result.PresentBlocker);
            text.AppendLine("Open: " + Issue2741);
            text.AppendLine("Next: wait for an official Avalonia Game control (#2741), or try a Windows-only NativeControlHost + child HWND (WPF GameEngineHost equivalent). Community AvaStride embeds Avalonia inside the game — the opposite direction.");
            if (!string.IsNullOrEmpty(result.LoadError))
                text.AppendLine("Load error: " + result.LoadError);
            return text.ToString().TrimEnd();
        }

        private static string Flatten(Exception ex)
        {
            if (ex == null)
                return null;
            var text = new StringBuilder();
            for (Exception walk = ex; walk != null; walk = walk.InnerException)
            {
                if (text.Length > 0)
                    text.Append(" -> ");
                string message = walk.Message ?? string.Empty;
                int nl = message.IndexOf('\n');
                if (nl >= 0)
                    message = message.Substring(0, nl);
                text.Append(walk.GetType().Name);
                text.Append(": ");
                text.Append(message.Trim());
            }
            return text.ToString();
        }
    }

    /// <summary>Outcome of <see cref="StrideHost.Probe"/>.</summary>
    public sealed class StrideHostResult
    {
        public bool EngineLoaded { get; set; }

        public bool GameConstructed { get; set; }

        public bool HeadlessContextAvailable { get; set; }

        public bool WindowCreated { get; set; }

        public bool HeadlessRunCompleted { get; set; }

        public bool GraphicsDeviceCreated { get; set; }

        /// <summary>True only when Stride created a real graphics device.</summary>
        public bool StrideGpuPresent { get; set; }

        public string GamesAssembly { get; set; }

        public string EngineAssembly { get; set; }

        public string GameTypeName { get; set; }

        public string WindowTypeName { get; set; }

        public string PresentPath { get; set; }

        public string PresentError { get; set; }

        public string PresentBlocker { get; set; }

        public string LoadError { get; set; }

        public string StatusText { get; set; }

        /// <summary>
        /// True when Stride types loaded and a <see cref="Game"/> was constructed.
        /// Present is not required.</summary>
        public bool Initialized
        {
            get { return EngineLoaded && GameConstructed; }
        }
    }
}
