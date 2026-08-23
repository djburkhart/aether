using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;
using Stride.Shaders;
using Stride.Shaders.Compiler;

namespace Aether.Stride
{
    /// <summary>
    /// Long-lived Stride GPU presenter. Creates a <see cref="GraphicsDevice"/>
    /// without <c>Game.Run</c>, renders placeholders (or a demo cube) to an
    /// offscreen target, and copies BGRA into the Viewport pixel buffer.
    /// Returns false (never throws) when the device cannot be created — ubuntu
    /// CI has no Vulkan and is expected to stay on the software cube.</summary>
    public sealed class StrideRttPresenter : IDisposable
    {
        public const string PathName = "stride-rtt";

        /// <summary>One-line status for headless CI: attempted / skipped / ready.</summary>
        public static string StatusLine
        {
            get { return s_statusLine ?? "stride-rtt not attempted"; }
        }

        public static bool DeviceReady
        {
            get { return s_instance != null && !s_failed; }
        }

        /// <summary>How many Level placeholders the next GPU frame will draw.</summary>
        public static int PlaceholderCount
        {
            get
            {
                lock (s_gate)
                    return s_placeholders != null ? s_placeholders.Length : 0;
            }
        }

        /// <summary>
        /// Bind (or clear) Level GameObject placeholders. Drawn instead of the
        /// demo cube when a device exists and the list is non-empty. Safe with
        /// no device — the list is ignored until TryRender succeeds.</summary>
        public static void SetPlaceholders(IReadOnlyList<ScenePlaceholder> placeholders)
        {
            lock (s_gate)
            {
                if (placeholders == null || placeholders.Count == 0)
                {
                    s_placeholders = Array.Empty<ScenePlaceholder>();
                    return;
                }
                var copy = new ScenePlaceholder[placeholders.Count];
                for (int i = 0; i < placeholders.Count; i++)
                    copy[i] = placeholders[i];
                s_placeholders = copy;
            }
        }

        /// <summary>
        /// Render one frame into <paramref name="pixels"/> (BGRA8888).
        /// Safe to call from the existing ViewportPresenter tick. Never throws.</summary>
        public static bool TryRender(byte[] pixels, int width, int height, double seconds)
        {
            if (pixels == null || width < 1 || height < 1)
                return false;
            lock (s_gate)
            {
                try
                {
                    if (s_failed)
                        return false;
                    if (s_instance == null)
                    {
                        SetStatus("stride-rtt attempted");
                        string reason;
                        s_instance = Create(out reason);
                        if (s_instance == null)
                        {
                            s_failed = true;
                            SetStatus("stride-rtt skipped: " + reason);
                            return false;
                        }
                        string kind = s_instance.m_effect != null ? " (lit cube)" : " (clear only)";
                        SetStatus("stride-rtt ready: " + s_instance.m_renderer +
                            " / " + GraphicsDevice.Platform + kind);
                        if (s_instance.m_effect == null && !string.IsNullOrEmpty(s_instance.m_shaderError))
                        {
                            Console.WriteLine("stride-rtt cube compile failed:");
                            Console.WriteLine(s_instance.m_shaderError);
                            s_statusLine = s_statusLine + Environment.NewLine +
                                "stride-rtt cube compile failed:" + Environment.NewLine +
                                s_instance.m_shaderError;
                        }
                    }
                    return s_instance.Render(pixels, width, height, seconds);
                }
                catch (Exception ex)
                {
                    s_failed = true;
                    DisposeInstance();
                    SetStatus("stride-rtt skipped: " + Flatten(ex));
                    return false;
                }
            }
        }

        public void Dispose()
        {
            m_staging?.Dispose();
            m_color?.Dispose();
            m_depth?.Dispose();
            m_cube?.Dispose();
            m_effect?.Dispose();
            m_device?.Dispose();
            m_staging = null;
            m_color = null;
            m_depth = null;
            m_cube = null;
            m_effect = null;
            m_device = null;
        }

        private static StrideRttPresenter Create(out string reason)
        {
            reason = null;
            string step = "start";
            try
            {
                step = "adapter-initialize";
                GraphicsAdapterFactory.Initialize();
                step = "device-new";
                GraphicsDevice device = GraphicsDevice.New(DeviceCreationFlags.None, GraphicsProfile.Level_11_0);
                if (device == null)
                {
                    reason = "GraphicsDevice.New returned null.";
                    return null;
                }

                step = "graphics-context";
                var presenter = new StrideRttPresenter
                {
                    m_device = device,
                    m_renderer = device.RendererName ?? GraphicsDevice.Platform.ToString(),
                    m_context = new GraphicsContext(device)
                };

                try
                {
                    step = "compile-lit-cube";
                    presenter.m_effect = CompileLitCube(device, presenter.m_context);
                    step = "cube-mesh";
                    if (presenter.m_effect != null)
                        presenter.m_cube = GeometricPrimitive.Cube.New(device, 1.15f, 1f, 1f, false);
                }
                catch (Exception ex)
                {
                    presenter.m_shaderError = ex.ToString();
                }

                return presenter;
            }
            catch (Exception ex)
            {
                reason = Classify(ex) + " [" + step + "]";
                Console.WriteLine("stride-rtt exception: " + ex);
                return null;
            }
        }

        private bool Render(byte[] pixels, int width, int height, double seconds)
        {
            EnsureTargets(width, height);
            if (m_color == null || m_context == null)
                return false;

            CommandList commandList = m_context.CommandList;
            commandList.SetRenderTargetAndViewport(m_depth, m_color);
            commandList.Clear(m_color, ClearNavy);
            if (m_depth != null)
                commandList.Clear(m_depth, DepthStencilClearOptions.DepthBuffer);

            if (m_effect != null && m_cube != null)
            {
                try
                {
                    ScenePlaceholder[] placeholders = s_placeholders;
                    if (placeholders != null && placeholders.Length > 0)
                        DrawPlaceholders(width, height, placeholders);
                    else
                        DrawCube(width, height, seconds);
                }
                catch (Exception)
                {
                    DrawCube(width, height, seconds);
                }
            }

            commandList.Flush();

            if (m_readback == null || m_readback.Length != width * height)
                m_readback = new Color[width * height];

            bool copied = m_staging != null
                ? m_color.GetData(commandList, m_staging, m_readback)
                : m_color.GetData(commandList, m_readback);
            if (!copied)
                return false;

            int count = Math.Min(pixels.Length / 4, m_readback.Length);
            for (int i = 0; i < count; i++)
            {
                Color c = m_readback[i];
                int o = i * 4;
                pixels[o] = c.B;
                pixels[o + 1] = c.G;
                pixels[o + 2] = c.R;
                pixels[o + 3] = 255;
            }
            return true;
        }

        private void DrawCube(int width, int height, double seconds)
        {
            float aspect = height > 0 ? (float)width / height : 1f;
            var view = Matrix.LookAtRH(new Vector3(0f, 1.15f, 3.05f), Vector3.Zero, Vector3.UnitY);
            var projection = Matrix.PerspectiveFovRH((float)Math.PI / 4f, aspect, 0.1f, 32f);
            var world = Matrix.RotationYawPitchRoll((float)seconds * 0.85f, (float)seconds * 0.35f, 0f);
            DrawMesh(world, view, projection, CubeColor);
        }

        private void DrawPlaceholders(int width, int height, ScenePlaceholder[] placeholders)
        {
            float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
            for (int i = 0; i < placeholders.Length; i++)
            {
                ScenePlaceholder p = placeholders[i];
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Z < minZ) minZ = p.Z;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
                if (p.Z > maxZ) maxZ = p.Z;
            }

            var center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, (minZ + maxZ) * 0.5f);
            float radius = Math.Max(Math.Max(maxX - minX, maxY - minY), maxZ - minZ);
            if (radius < 6f)
                radius = 6f;
            var eye = center + new Vector3(radius * 0.95f, radius * 0.65f, radius * 1.15f);

            float aspect = height > 0 ? (float)width / height : 1f;
            var view = Matrix.LookAtRH(eye, center, Vector3.UnitY);
            var projection = Matrix.PerspectiveFovRH((float)Math.PI / 4f, aspect, 0.1f, radius * 8f + 32f);

            for (int i = 0; i < placeholders.Length; i++)
            {
                ScenePlaceholder p = placeholders[i];
                float sx = ClampPlaceholderScale(p.Sx);
                float sy = ClampPlaceholderScale(p.Sy);
                float sz = ClampPlaceholderScale(p.Sz);
                var world = Matrix.Scaling(sx, sy, sz) *
                    Matrix.RotationYawPitchRoll(p.Ry, p.Rx, p.Rz) *
                    Matrix.Translation(p.X, p.Y, p.Z);
                DrawMesh(world, view, projection, PlaceholderColor(i));
            }
        }

        private void DrawMesh(Matrix world, Matrix view, Matrix projection, Color4 color)
        {
            Matrix worldViewProjection = world * view * projection;
            m_effect.Parameters.Set(WorldViewProjectionKey, worldViewProjection);
            m_effect.Parameters.Set(LightDirectionKey, LightDir);
            m_effect.Parameters.Set(ColorKey, color);
            m_effect.UpdateEffect(m_device);
            m_cube.Draw(m_context, m_effect);
        }

        private static float ClampPlaceholderScale(float scale)
        {
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
                return 0.7f;
            if (scale < 0.35f)
                return 0.35f;
            if (scale > 1.75f)
                return 1.75f;
            return scale;
        }

        private static Color4 PlaceholderColor(int index)
        {
            // Same cyan family as the demo cube, slightly varied per object.
            float hue = (index * 0.17f) % 1f;
            float g = 0.62f + 0.22f * hue;
            float b = 0.88f - 0.18f * hue;
            return new Color4(0.32f + 0.18f * hue, g, b, 1f);
        }

        private void EnsureTargets(int width, int height)
        {
            if (m_color != null && m_color.Width == width && m_color.Height == height)
                return;

            m_staging?.Dispose();
            m_color?.Dispose();
            m_depth?.Dispose();
            m_staging = null;
            m_color = null;
            m_depth = null;

            m_color = Texture.New2D(
                m_device, width, height,
                PixelFormat.R8G8B8A8_UNorm,
                TextureFlags.RenderTarget | TextureFlags.ShaderResource);
            try
            {
                m_depth = Texture.New2D(
                    m_device, width, height,
                    PixelFormat.D24_UNorm_S8_UInt,
                    TextureFlags.DepthStencil);
            }
            catch
            {
                m_depth = Texture.New2D(
                    m_device, width, height,
                    PixelFormat.D16_UNorm,
                    TextureFlags.DepthStencil);
            }
            m_staging = Texture.New2D(
                m_device, width, height,
                PixelFormat.R8G8B8A8_UNorm,
                TextureFlags.None, 1, GraphicsResourceUsage.Staging);

            m_device.Presenter = new RenderTargetGraphicsPresenter(
                m_device, m_color, m_depth != null ? m_depth.Format : PixelFormat.D16_UNorm);
        }

        private static EffectInstance CompileLitCube(GraphicsDevice device, GraphicsContext context)
        {
            string root = Path.Combine(Path.GetTempPath(), "aether-stride-shaders");
            string shaderDir = Path.Combine(root, "shaders");
            Directory.CreateDirectory(shaderDir);
            File.WriteAllText(Path.Combine(shaderDir, "ShaderBaseStream.sdsl"), ShaderBaseStreamSource);
            File.WriteAllText(Path.Combine(shaderDir, "ShaderBase.sdsl"), ShaderBaseSource);
            File.WriteAllText(Path.Combine(shaderDir, "AetherViewportLit.sdsl"), LitCubeSource);

            // Unique mount — do not steal VirtualFileSystem "/".
            var provider = new FileSystemProvider("/aether-shaders", root);

            // EffectCompilerFactory.CreateEffectCompiler wraps a local compiler in
            // EffectCompilerCache, which throws ArgumentNullException
            // ("Using the cache requires a database") when DatabaseFileProvider
            // is null. The local EffectCompiler compiles without a Game.Run
            // ObjectDatabase. Verified against Stride.Shaders.Compilers 4.4.0-beta5.
            var compiler = new EffectCompiler(provider);
            compiler.SourceDirectories.Add(EffectCompilerBase.DefaultSourceShaderFolder);

            var mixin = new ShaderMixinSource { Name = "AetherViewportLit" };
            mixin.Mixins.Add(new ShaderClassSource("AetherViewportLit"));

            var parameters = new CompilerParameters();
            parameters.EffectParameters.Platform = GraphicsDevice.Platform;
            parameters.EffectParameters.Profile = GraphicsProfile.Level_11_0;

            CompilerResults results = compiler.Compile(mixin, parameters);
            if (results.HasErrors)
                throw new InvalidOperationException(results.ToText());

            EffectBytecodeCompilerResult compiled = results.Bytecode.WaitForResult();
            if (compiled.CompilationLog != null && compiled.CompilationLog.HasErrors)
                throw new InvalidOperationException(compiled.CompilationLog.ToText());
            if (compiled.Bytecode == null)
                throw new InvalidOperationException("Effect compiler returned no bytecode.");

            var effect = new Effect(device, compiled.Bytecode) { Name = "AetherViewportLit" };
            var instance = new EffectInstance(effect);
            if (!instance.UpdateEffect(device))
                throw new InvalidOperationException("EffectInstance.UpdateEffect failed.");
            return instance;
        }

        private static bool LooksLikeMissingGpu(Exception ex)
        {
            string full = ex == null ? string.Empty : Flatten(ex) + Environment.NewLine + ex;
            return full.IndexOf("vulkan", StringComparison.OrdinalIgnoreCase) >= 0 ||
                full.IndexOf("ErrorIncompatibleDriver", StringComparison.OrdinalIgnoreCase) >= 0 ||
                full.IndexOf("DXGI.GetApi", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Classify(Exception ex)
        {
            string text = Flatten(ex);
            if (LooksLikeMissingGpu(ex))
            {
                return "Vulkan instance/device creation failed (no usable GPU/driver). " +
                    "ubuntu CI is expected to stay on software-writeablebitmap. " + text;
            }
            return text;
        }

        private static string Flatten(Exception ex)
        {
            if (ex == null)
                return "unknown error";
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

        private static void SetStatus(string line)
        {
            s_statusLine = line;
            Console.WriteLine(line);
        }

        private static void DisposeInstance()
        {
            if (s_instance == null)
                return;
            try { s_instance.Dispose(); }
            catch { }
            s_instance = null;
        }

        private GraphicsDevice m_device;
        private GraphicsContext m_context;
        private Texture m_color;
        private Texture m_depth;
        private Texture m_staging;
        private GeometricPrimitive m_cube;
        private EffectInstance m_effect;
        private Color[] m_readback;
        private string m_renderer;
        private string m_shaderError;

        private static readonly object s_gate = new object();
        private static StrideRttPresenter s_instance;
        private static bool s_failed;
        private static string s_statusLine;
        private static ScenePlaceholder[] s_placeholders = Array.Empty<ScenePlaceholder>();

        private static readonly Color4 ClearNavy = new Color4(0.04f, 0.06f, 0.16f, 1f);
        private static readonly Color4 CubeColor = new Color4(0.40f, 0.78f, 0.92f, 1f);
        private static readonly Vector3 LightDir = Vector3.Normalize(new Vector3(0.35f, 0.85f, 0.40f));

        private static readonly ValueParameterKey<Matrix> WorldViewProjectionKey =
            ParameterKeys.NewValue(Matrix.Identity, "WorldViewProjection");
        private static readonly ValueParameterKey<Vector3> LightDirectionKey =
            ParameterKeys.NewValue(new Vector3(0.35f, 0.85f, 0.40f), "LightDirection");
        private static readonly ValueParameterKey<Color4> ColorKey =
            ParameterKeys.NewValue(new Color4(1f, 1f, 1f, 1f), "Color");

        // MIT Stride snippets (ShaderBase / ShaderBaseStream) written to a temp
        // FileSystemProvider so the local EffectCompiler can resolve the parent.
        private const string ShaderBaseStreamSource =
            "shader ShaderBaseStream\n" +
            "{\n" +
            "    stage stream float4 ShadingPosition : SV_Position;\n" +
            "    stage stream float4 ColorTarget : SV_Target0;\n" +
            "};\n";

        private const string ShaderBaseSource =
            "shader ShaderBase : ShaderBaseStream\n" +
            "{\n" +
            "    stage void VSMain() {}\n" +
            "    stage void PSMain() {}\n" +
            "};\n";

        private const string LitCubeSource =
            "shader AetherViewportLit : ShaderBase\n" +
            "{\n" +
            "    stage stream float4 Position : POSITION;\n" +
            "    stage stream float3 Normal : NORMAL;\n" +
            "    stage stream float2 TexCoord : TEXCOORD0;\n" +
            "    cbuffer PerDraw {\n" +
            "        stage float4x4 WorldViewProjection;\n" +
            "        stage float3 LightDirection;\n" +
            "        stage float4 Color;\n" +
            "    }\n" +
            "    stage override void VSMain()\n" +
            "    {\n" +
            "        streams.ShadingPosition = mul(streams.Position, WorldViewProjection);\n" +
            "    }\n" +
            "    stage override void PSMain()\n" +
            "    {\n" +
            "        float ndl = saturate(dot(normalize(streams.Normal), LightDirection));\n" +
            "        streams.ColorTarget = Color * (0.22 + 0.78 * ndl);\n" +
            "        streams.ColorTarget.a = 1.0;\n" +
            "    }\n" +
            "};\n";
    }
}
