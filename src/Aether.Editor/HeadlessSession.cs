using System;
using System.ComponentModel;
using System.IO;
using System.Linq;

using Aether.Circuit;
using Aether.Level;
using Aether.Editor.Dock;
using Aether.Scripting;
using Aether.Stride;

using Stride.Engine;

using Dock.Model.Controls;
using Aether.Timeline;
using Aether.Plugins;

using Microsoft.Extensions.DependencyInjection;

using Sce.Atf.Adaptation;
using Sce.Atf.Controls.PropertyEditing;
using Sce.Atf.VectorMath;

using LevelEditorCore;

using UsingDom;

using PropertyDescriptor = System.ComponentModel.PropertyDescriptor;

namespace Aether.Editor
{
    /// <summary>
    /// Display-free smoke for CI / <c>dotnet run -- --headless-session</c>.
    /// Constructs the same EditorSession the window hosts and proves selection,
    /// ATF descriptors, DomNode mutation, HistoryContext undo, and
    /// DomXml Open/Save round-trip.</summary>
    internal static class HeadlessSession
    {
        public static int Run()
        {
            EditorSession session = new EditorSession();
            Console.WriteLine("schema: {0}", session.SchemaPath);
            Console.WriteLine("objects:");
            foreach (GameObjectItem item in session.Objects)
                Console.WriteLine("  {0}", item.Display);

            int code = ProveEditUndo(session);
            if (code != 0)
                return code;

            code = ProveRoundTrip(session);
            if (code != 0)
                return code;

            code = ProvePlugins(session);
            if (code != 0)
                return code;

            code = ProveCircuit(session);
            if (code != 0)
                return code;

            code = ProveTimeline(session);
            if (code != 0)
                return code;

            code = ProveLevel(session);
            if (code != 0)
                return code;

            code = ProveScripts(session);
            if (code != 0)
                return code;

            code = ProveDebugger(session);
            if (code != 0)
                return code;

            return ProveStride(session);
        }

        public static int WriteFixture()
        {
            string? testdata = GameDocument.FindUsingDomTestdataDirectory();
            if (testdata == null)
            {
                Console.Error.WriteLine("Error: could not find testdata/atf/UsingDom to write the fixture.");
                return 1;
            }

            string dest = Path.Combine(testdata, GameDocument.SampleDocumentFileName);
            EditorSession session = new EditorSession();
            session.SaveAs(dest);
            Console.WriteLine("wrote fixture: {0}", dest);
            return 0;
        }

        private static int ProveEditUndo(EditorSession session)
        {
            GameObjectItem? bill = Find(session, "Bill");
            if (bill == null)
            {
                Console.Error.WriteLine("Error: UsingDom document is missing Bill.");
                return 2;
            }

            session.SelectedObject = bill;
            if (session.PropertyTarget == null)
            {
                Console.Error.WriteLine("Error: selection did not produce an ICustomTypeDescriptor target.");
                return 3;
            }

            PropertyDescriptor? size = FindDescriptor(session, "Size");
            if (size == null)
            {
                Console.Error.WriteLine("Error: selected Bill is missing Size descriptor.");
                return 4;
            }

            object? before = size.GetValue(session.PropertyTarget);
            Console.WriteLine("Bill Size before: {0}", before);
            PropertyUtils.SetProperty(bill.Node, size, 14);
            object? after = size.GetValue(session.PropertyTarget);
            Console.WriteLine("Bill Size after edit: {0}", after);
            if (!Equals(after, 14))
            {
                Console.Error.WriteLine("Error: property edit did not change the DomNode.");
                return 5;
            }

            if (!session.CanUndo)
            {
                Console.Error.WriteLine("Error: HistoryContext did not record the edit.");
                return 6;
            }

            session.Undo();
            object? undone = size.GetValue(session.PropertyTarget ?? (object)bill.Node);
            Console.WriteLine("Bill Size after undo: {0}", undone);
            if (!Equals(undone, before))
            {
                Console.Error.WriteLine("Error: undo did not restore Size.");
                return 7;
            }

            Console.WriteLine("headless edit/undo ok");
            return 0;
        }

        private static int ProveRoundTrip(EditorSession session)
        {
            string? fixture = GameDocument.FindSampleDocumentPath();
            if (fixture == null)
            {
                Console.Error.WriteLine("Error: could not find testdata/atf/UsingDom/ogre-adventure-ii.xml");
                return 8;
            }

            Console.WriteLine("fixture: {0}", fixture);
            session.Open(fixture);
            if (session.IsDirty || session.CanUndo)
            {
                Console.Error.WriteLine("Error: Open should clear dirty state and undo history.");
                return 9;
            }

            if (Find(session, "Bill") == null || Find(session, "Sally") == null || Find(session, "Mr. Oak") == null)
            {
                Console.Error.WriteLine("Error: opened fixture is missing Bill, Sally, or Mr. Oak.");
                return 10;
            }

            GameObjectItem bill = Find(session, "Bill")!;
            session.SelectedObject = bill;
            PropertyDescriptor? size = FindDescriptor(session, "Size");
            if (size == null)
            {
                Console.Error.WriteLine("Error: opened Bill is missing Size descriptor.");
                return 11;
            }

            object? openedSize = size.GetValue(session.PropertyTarget);
            Console.WriteLine("Bill Size from fixture: {0}", openedSize);
            if (!Equals(openedSize, 12))
            {
                Console.Error.WriteLine("Error: fixture Bill Size should be 12.");
                return 12;
            }

            PropertyUtils.SetProperty(bill.Node, size, 14);
            if (!session.IsDirty)
            {
                Console.Error.WriteLine("Error: property edit should mark the document dirty.");
                return 13;
            }

            string temp = Path.Combine(Path.GetTempPath(), "aether-usingdom-roundtrip-" + Guid.NewGuid().ToString("N") + ".xml");
            try
            {
                session.SaveAs(temp);
                if (session.IsDirty)
                {
                    Console.Error.WriteLine("Error: Save As should clear dirty state.");
                    return 14;
                }

                session.New();
                if (session.FilePath != null || session.CanUndo)
                {
                    Console.Error.WriteLine("Error: New should clear the file path and history.");
                    return 15;
                }

                session.Open(temp);
                GameObjectItem? reopened = Find(session, "Bill");
                if (reopened == null)
                {
                    Console.Error.WriteLine("Error: reopened document is missing Bill.");
                    return 16;
                }

                session.SelectedObject = reopened;
                PropertyDescriptor? reopenedSize = FindDescriptor(session, "Size");
                object? value = reopenedSize?.GetValue(session.PropertyTarget);
                Console.WriteLine("Bill Size after reopen: {0}", value);
                if (!Equals(value, 14))
                {
                    Console.Error.WriteLine("Error: round-trip did not preserve Bill Size 14.");
                    return 17;
                }

                if (session.CanUndo)
                {
                    Console.Error.WriteLine("Error: Open should start with an empty undo history.");
                    return 18;
                }
            }
            finally
            {
                try { File.Delete(temp); } catch (IOException) { }
            }

            Console.WriteLine("headless round-trip ok");
            return 0;
        }

        private static int ProvePlugins(EditorSession session)
        {
            Console.WriteLine("plugins directory: {0}", session.PluginHost.Directory);
            Console.WriteLine("loaded plugins: {0}", session.LoadedPlugins.Count);
            foreach (LoadedPlugin plugin in session.LoadedPlugins)
                Console.WriteLine("  {0}", plugin.Display);

            if (session.LoadedPlugins.Count == 0)
            {
                Console.Error.WriteLine("Error: no plugins loaded from {0}", session.PluginHost.Directory);
                return 20;
            }

            IEditorContribution? hello = session.Contributions.FirstOrDefault(c => c.Id == "hello-aether");
            if (hello == null)
            {
                Console.Error.WriteLine("Error: IEditorContribution 'hello-aether' was not registered.");
                return 21;
            }

            IEditorContribution? fromDi = session.PluginHost.Services
                .GetServices<IEditorContribution>()
                .FirstOrDefault(c => c.Id == "hello-aether");
            if (fromDi == null)
            {
                Console.Error.WriteLine("Error: DI did not resolve IEditorContribution 'hello-aether'.");
                return 22;
            }

            Console.WriteLine("contribution: {0} — {1}", hello.Title, hello.Description);
            Console.WriteLine("headless plugins ok");
            return 0;
        }

        private static int ProveCircuit(EditorSession session)
        {
            string? fixture = CircuitDocuments.FindSampleDocumentPath();
            if (fixture == null)
            {
                Console.Error.WriteLine("Error: could not find testdata/atf/CircuitEditor/Example.circuit");
                return 30;
            }

            Console.WriteLine("circuit fixture: {0}", fixture);
            session.Open(fixture);
            if (session.ActiveKind != EditorDocumentKind.Circuit)
            {
                Console.Error.WriteLine("Error: Open of Example.circuit did not activate the circuit document.");
                return 31;
            }

            int modules = session.Circuit.Nodes.Count;
            int wires = session.Circuit.Wires.Count;
            Console.WriteLine("circuit modules: {0}", modules);
            Console.WriteLine("circuit wires: {0}", wires);
            if (modules != CircuitDocuments.ExampleModuleCount || wires != CircuitDocuments.ExampleConnectionCount)
            {
                Console.Error.WriteLine(
                    "Error: Example.circuit should have {0} modules and {1} wires, got {2}/{3}.",
                    CircuitDocuments.ExampleModuleCount,
                    CircuitDocuments.ExampleConnectionCount,
                    modules,
                    wires);
                return 32;
            }

            CircuitNodeItem? and1 = session.Circuit.Find("And_1");
            if (and1 == null)
            {
                Console.Error.WriteLine("Error: Example.circuit is missing And_1.");
                return 33;
            }

            session.Circuit.SelectedNode = and1;
            if (session.PropertyTarget == null)
            {
                Console.Error.WriteLine("Error: selecting And_1 did not produce an ICustomTypeDescriptor target.");
                return 34;
            }

            PropertyDescriptor? id = FindDescriptor(session, "ID");
            if (id == null)
            {
                Console.Error.WriteLine("Error: selected And_1 is missing ID descriptor.");
                return 35;
            }

            object? idValue = id.GetValue(session.PropertyTarget);
            Console.WriteLine("And_1 ID: {0}", idValue);
            if (!Equals(idValue, "And_1"))
            {
                Console.Error.WriteLine("Error: And_1 ID should be And_1.");
                return 36;
            }

            PropertyDescriptor? name = FindDescriptor(session, "Name");
            if (name == null)
            {
                Console.Error.WriteLine("Error: selected And_1 is missing Name descriptor.");
                return 37;
            }

            object? before = name.GetValue(session.PropertyTarget);
            PropertyUtils.SetProperty(and1.Module.DomNode, name, "And gate");
            object? after = name.GetValue(session.PropertyTarget);
            Console.WriteLine("And_1 Name after edit: {0}", after);
            if (!Equals(after, "And gate"))
            {
                Console.Error.WriteLine("Error: circuit property edit did not change the module label.");
                return 38;
            }

            if (!session.CanUndo)
            {
                Console.Error.WriteLine("Error: circuit HistoryContext did not record the edit.");
                return 39;
            }

            session.Undo();
            object? undone = name.GetValue(session.PropertyTarget ?? (object)and1.Module.DomNode);
            Console.WriteLine("And_1 Name after undo: {0}", undone);
            if (!Equals(undone, before))
            {
                Console.Error.WriteLine("Error: circuit undo did not restore Name.");
                return 40;
            }

            session.AddCircuitAnd();
            int afterAddModules = session.Circuit.Nodes.Count;
            int afterAddWires = session.Circuit.Wires.Count;
            Console.WriteLine("circuit after add: {0} modules, {1} wires", afterAddModules, afterAddWires);
            if (afterAddModules != CircuitDocuments.ExampleModuleCount + 1 ||
                afterAddWires != CircuitDocuments.ExampleConnectionCount + 1)
            {
                Console.Error.WriteLine("Error: Add And + wire should add one module and one wire.");
                return 41;
            }

            string temp = Path.Combine(Path.GetTempPath(), "aether-circuit-roundtrip-" + Guid.NewGuid().ToString("N") + ".circuit");
            try
            {
                session.SaveAs(temp);
                session.Circuit.LoadExample();
                session.Open(temp);
                if (session.Circuit.Nodes.Count != afterAddModules || session.Circuit.Wires.Count != afterAddWires)
                {
                    Console.Error.WriteLine(
                        "Error: reopened circuit should have {0} modules and {1} wires, got {2}/{3}.",
                        afterAddModules,
                        afterAddWires,
                        session.Circuit.Nodes.Count,
                        session.Circuit.Wires.Count);
                    return 42;
                }

                if (session.Circuit.Find("And_1") == null)
                {
                    Console.Error.WriteLine("Error: reopened circuit is missing And_1.");
                    return 43;
                }
            }
            finally
            {
                try { File.Delete(temp); } catch (IOException) { }
            }

            Console.WriteLine("headless circuit ok");
            return 0;
        }

        private static int ProveTimeline(EditorSession session)
        {
            string? fixture = TimelineDocuments.FindSampleDocumentPath();
            if (fixture == null)
            {
                Console.Error.WriteLine("Error: could not find testdata/atf/TimelineEditor/100.timeline");
                return 50;
            }

            Console.WriteLine("timeline fixture: {0}", fixture);
            session.Open(fixture);
            if (session.ActiveKind != EditorDocumentKind.Timeline)
            {
                Console.Error.WriteLine("Error: Open of 100.timeline did not activate the timeline document.");
                return 51;
            }

            int tracks = session.Timeline.TrackCount;
            int intervals = session.Timeline.Intervals.Count;
            Console.WriteLine("timeline tracks: {0}", tracks);
            Console.WriteLine("timeline intervals: {0}", intervals);
            if (tracks != TimelineDocuments.ExampleTrackCount || intervals != TimelineDocuments.ExampleIntervalCount)
            {
                Console.Error.WriteLine(
                    "Error: 100.timeline should have {0} tracks and {1} intervals, got {2}/{3}.",
                    TimelineDocuments.ExampleTrackCount,
                    TimelineDocuments.ExampleIntervalCount,
                    tracks,
                    intervals);
                return 52;
            }

            TimelineIntervalItem? clip = session.Timeline.Find("Interval");
            if (clip == null)
            {
                Console.Error.WriteLine("Error: 100.timeline is missing Interval.");
                return 53;
            }

            session.Timeline.SelectedInterval = clip;
            if (session.PropertyTarget == null)
            {
                Console.Error.WriteLine("Error: selecting Interval did not produce an ICustomTypeDescriptor target.");
                return 54;
            }

            PropertyDescriptor? name = FindDescriptor(session, "Name");
            if (name == null)
            {
                Console.Error.WriteLine("Error: selected Interval is missing Name descriptor.");
                return 55;
            }

            object? before = name.GetValue(session.PropertyTarget);
            Console.WriteLine("Interval Name before: {0}", before);
            PropertyUtils.SetProperty(clip.Interval.DomNode, name, "Clip");
            object? after = name.GetValue(session.PropertyTarget);
            Console.WriteLine("Interval Name after edit: {0}", after);
            if (!Equals(after, "Clip"))
            {
                Console.Error.WriteLine("Error: timeline property edit did not change the interval name.");
                return 56;
            }

            if (!session.CanUndo)
            {
                Console.Error.WriteLine("Error: timeline HistoryContext did not record the edit.");
                return 57;
            }

            session.Undo();
            object? undone = name.GetValue(session.PropertyTarget ?? (object)clip.Interval.DomNode);
            Console.WriteLine("Interval Name after undo: {0}", undone);
            if (!Equals(undone, before))
            {
                Console.Error.WriteLine("Error: timeline undo did not restore Name.");
                return 58;
            }

            session.AddTimelineInterval();
            int afterAdd = session.Timeline.Intervals.Count;
            Console.WriteLine("timeline after add: {0} intervals", afterAdd);
            if (afterAdd != TimelineDocuments.ExampleIntervalCount + 1)
            {
                Console.Error.WriteLine("Error: Add Interval should add one interval.");
                return 59;
            }

            string temp = Path.Combine(Path.GetTempPath(), "aether-timeline-roundtrip-" + Guid.NewGuid().ToString("N") + ".timeline");
            try
            {
                session.SaveAs(temp);
                session.Timeline.LoadExample();
                session.Open(temp);
                if (session.Timeline.Intervals.Count != afterAdd ||
                    session.Timeline.TrackCount != TimelineDocuments.ExampleTrackCount)
                {
                    Console.Error.WriteLine(
                        "Error: reopened timeline should have {0} intervals and {1} tracks, got {2}/{3}.",
                        afterAdd,
                        TimelineDocuments.ExampleTrackCount,
                        session.Timeline.Intervals.Count,
                        session.Timeline.TrackCount);
                    return 60;
                }

                if (session.Timeline.Find("Interval") == null)
                {
                    Console.Error.WriteLine("Error: reopened timeline is missing Interval.");
                    return 61;
                }
            }
            finally
            {
                try { File.Delete(temp); } catch (IOException) { }
            }

            Console.WriteLine("headless timeline ok");
            return 0;
        }

        private static int ProveLevel(EditorSession session)
        {
            string? fixture = LevelDocuments.FindSampleDocumentPath();
            if (fixture == null)
            {
                Console.Error.WriteLine("Error: could not find testdata/atf/LevelEditor/LightTest.lvl");
                return 70;
            }

            Console.WriteLine("level fixture: {0}", fixture);
            session.Open(fixture);
            if (session.ActiveKind != EditorDocumentKind.Level)
            {
                Console.Error.WriteLine("Error: Open of LightTest.lvl did not activate the level document.");
                return 71;
            }

            int objects = session.Level.GameObjectCount;
            int top = session.Level.TopLevelCount;
            Console.WriteLine("level game objects: {0}", objects);
            Console.WriteLine("level top-level: {0}", top);
            if (objects != LevelDocuments.ExampleGameObjectCount || top != LevelDocuments.ExampleTopLevelCount)
            {
                Console.Error.WriteLine(
                    "Error: LightTest.lvl should have {0} game objects ({1} top-level), got {2}/{3}.",
                    LevelDocuments.ExampleGameObjectCount,
                    LevelDocuments.ExampleTopLevelCount,
                    objects,
                    top);
                return 72;
            }

            LevelNodeItem? light = session.Level.Find("PointLight");
            if (light == null)
            {
                Console.Error.WriteLine("Error: LightTest.lvl is missing PointLight.");
                return 73;
            }

            var gob = light.Node.As<LevelEditorCore.IGameObject>();
            if (gob == null)
            {
                Console.Error.WriteLine("Error: PointLight did not adapt to IGameObject.");
                return 74;
            }

            float tx = gob.Translation.X;
            Console.WriteLine("PointLight translate X: {0}", tx);
            if (Math.Abs(tx - LevelDocuments.ExamplePointLightTranslateX) > 0.0001f)
            {
                Console.Error.WriteLine(
                    "Error: PointLight translate X should be {0}, got {1}.",
                    LevelDocuments.ExamplePointLightTranslateX,
                    tx);
                return 75;
            }

            int sceneCode = ProveBoundScene(
                session,
                expectedCount: objects,
                expectedName: "PointLight",
                expectedTranslateX: tx,
                afterLabel: "load");
            if (sceneCode != 0)
                return sceneCode;

            session.Level.SelectedNode = light;
            if (session.PropertyTarget == null)
            {
                Console.Error.WriteLine("Error: selecting PointLight did not produce an ICustomTypeDescriptor target.");
                return 76;
            }

            PropertyDescriptor? name = FindDescriptor(session, "Name");
            if (name == null)
            {
                Console.Error.WriteLine("Error: selected PointLight is missing Name descriptor.");
                return 77;
            }

            object? before = name.GetValue(session.PropertyTarget);
            Console.WriteLine("PointLight Name before: {0}", before);
            PropertyUtils.SetProperty(light.Node, name, "KeyLight");
            object? after = name.GetValue(session.PropertyTarget);
            Console.WriteLine("PointLight Name after edit: {0}", after);
            if (!Equals(after, "KeyLight"))
            {
                Console.Error.WriteLine("Error: level property edit did not change the object name.");
                return 78;
            }

            sceneCode = ProveBoundScene(
                session,
                expectedCount: objects,
                expectedName: "KeyLight",
                expectedTranslateX: tx,
                afterLabel: "name edit");
            if (sceneCode != 0)
                return sceneCode;

            if (!session.CanUndo)
            {
                Console.Error.WriteLine("Error: level HistoryContext did not record the edit.");
                return 79;
            }

            session.Undo();
            object? undone = name.GetValue(session.PropertyTarget ?? (object)light.Node);
            Console.WriteLine("PointLight Name after undo: {0}", undone);
            if (!Equals(undone, before))
            {
                Console.Error.WriteLine("Error: level undo did not restore Name.");
                return 80;
            }

            sceneCode = ProveBoundScene(
                session,
                expectedCount: objects,
                expectedName: "PointLight",
                expectedTranslateX: tx,
                afterLabel: "name undo");
            if (sceneCode != 0)
                return sceneCode;

            LevelNodeItem? lightAfterUndo = session.Level.Find("PointLight");
            var gobAfterUndo = lightAfterUndo != null
                ? lightAfterUndo.Node.As<IGameObject>()
                : null;
            if (gobAfterUndo == null)
            {
                Console.Error.WriteLine("Error: PointLight missing after name undo; cannot prove translate bind.");
                return 83;
            }

            float movedX = tx + 1.5f;
            session.Level.History.DoTransaction(
                () =>
                {
                    gobAfterUndo.Translation = new Vec3F(movedX, gobAfterUndo.Translation.Y, gobAfterUndo.Translation.Z);
                },
                "Translate PointLight");
            Console.WriteLine("PointLight translate X after edit: {0}", gobAfterUndo.Translation.X);
            sceneCode = ProveBoundScene(
                session,
                expectedCount: objects,
                expectedName: "PointLight",
                expectedTranslateX: movedX,
                afterLabel: "translate edit");
            if (sceneCode != 0)
                return sceneCode;

            session.Undo();
            Console.WriteLine("PointLight translate X after undo: {0}", gobAfterUndo.Translation.X);
            sceneCode = ProveBoundScene(
                session,
                expectedCount: objects,
                expectedName: "PointLight",
                expectedTranslateX: tx,
                afterLabel: "translate undo");
            if (sceneCode != 0)
                return sceneCode;

            session.AddLevelGameObject();
            int afterAdd = session.Level.GameObjectCount;
            Console.WriteLine("level after add: {0} game objects", afterAdd);
            if (afterAdd != LevelDocuments.ExampleGameObjectCount + 1)
            {
                Console.Error.WriteLine("Error: Add GameObject should add one game object.");
                return 81;
            }

            sceneCode = ProveBoundScene(
                session,
                expectedCount: afterAdd,
                expectedName: "PointLight",
                expectedTranslateX: tx,
                afterLabel: "add");
            if (sceneCode != 0)
                return sceneCode;

            string temp = Path.Combine(Path.GetTempPath(), "aether-level-roundtrip-" + Guid.NewGuid().ToString("N") + ".lvl");
            try
            {
                session.SaveAs(temp);
                session.Level.LoadExample();
                session.Open(temp);
                if (session.Level.GameObjectCount != afterAdd ||
                    session.Level.Find("PointLight") == null)
                {
                    Console.Error.WriteLine(
                        "Error: reopened level should have {0} game objects and PointLight, got {1}.",
                        afterAdd,
                        session.Level.GameObjectCount);
                    return 82;
                }

                sceneCode = ProveBoundScene(
                    session,
                    expectedCount: afterAdd,
                    expectedName: "PointLight",
                    expectedTranslateX: tx,
                    afterLabel: "reopen");
                if (sceneCode != 0)
                    return sceneCode;
            }
            finally
            {
                try { File.Delete(temp); } catch (IOException) { }
            }

            Console.WriteLine("headless level ok");
            return 0;
        }

        private static int ProveBoundScene(
            EditorSession session,
            int expectedCount,
            string expectedName,
            float expectedTranslateX,
            string afterLabel)
        {
            BoundLevelScene scene = session.Level.BoundScene;
            Console.WriteLine("bound scene backend: {0}", session.Level.SceneBackend);
            Console.WriteLine("bound scene objects after {0}: {1}", afterLabel, scene.Count);
            BoundSceneObject? found = scene.Find(expectedName);
            Console.WriteLine(
                "bound scene {0}: {1}",
                expectedName,
                found != null ? "yes (translate X: " + found.Translation.X + ")" : "no");

            if (scene.Count != expectedCount)
            {
                Console.Error.WriteLine(
                    "Error: bound scene after {0} should have {1} GameObjects, got {2}.",
                    afterLabel,
                    expectedCount,
                    scene.Count);
                return 84;
            }
            if (found == null)
            {
                Console.Error.WriteLine(
                    "Error: bound scene after {0} is missing {1}.",
                    afterLabel,
                    expectedName);
                return 85;
            }
            if (Math.Abs(found.Translation.X - expectedTranslateX) > 0.0001f)
            {
                Console.Error.WriteLine(
                    "Error: bound scene {0} translate X after {1} should be {2}, got {3}.",
                    expectedName,
                    afterLabel,
                    expectedTranslateX,
                    found.Translation.X);
                return 86;
            }

            if (session.Level.Engine is StrideGameEngine stride)
            {
                Console.WriteLine("stride scene objects after {0}: {1}", afterLabel, stride.EntityCount);
                Console.WriteLine(
                    "stride scene {0}: {1}",
                    expectedName,
                    stride.HasEntity(expectedName) ? "yes" : "no");
                if (stride.EntityCount != expectedCount)
                {
                    Console.Error.WriteLine(
                        "Error: Stride scene after {0} should have {1} entities, got {2}.",
                        afterLabel,
                        expectedCount,
                        stride.EntityCount);
                    return 87;
                }
                if (!stride.HasEntity(expectedName))
                {
                    Console.Error.WriteLine(
                        "Error: Stride scene after {0} is missing {1}.",
                        afterLabel,
                        expectedName);
                    return 88;
                }

                Entity? entity = stride.FindEntity(expectedName);
                if (entity != null &&
                    Math.Abs(entity.Transform.Position.X - expectedTranslateX) > 0.0001f)
                {
                    Console.Error.WriteLine(
                        "Error: Stride entity {0} translate X after {1} should be {2}, got {3}.",
                        expectedName,
                        afterLabel,
                        expectedTranslateX,
                        entity.Transform.Position.X);
                    return 89;
                }
            }
            else
            {
                Console.WriteLine("stride scene: not bound (NullGameEngine; no GraphicsDevice)");
            }

            return 0;
        }

        private static int ProveScripts(EditorSession session)
        {
            string? csharp = ScriptFiles.FindSampleCSharpPath();
            string? lua = ScriptFiles.FindSampleLuaPath();
            if (csharp == null || lua == null)
            {
                Console.Error.WriteLine("Error: could not find testdata/scripts/resize-bill.csx or .lua");
                return 90;
            }

            session.New();
            Console.WriteLine("csharp fixture: {0}", csharp);
            ScriptResult cs = session.Script.RunFile(csharp);
            Console.WriteLine("csharp output: {0}", cs.Output);
            if (!cs.Succeeded)
            {
                Console.Error.WriteLine("Error: C# script failed: {0}", cs.Output);
                return 91;
            }

            object? size = BillSize(session);
            Console.WriteLine("Bill Size after C#: {0}", size);
            if (!Equals(size, ScriptFiles.ExpectedBillSize))
            {
                Console.Error.WriteLine("Error: C# script should set Bill Size to {0}.", ScriptFiles.ExpectedBillSize);
                return 92;
            }

            session.New();
            Console.WriteLine("lua fixture: {0}", lua);
            ScriptResult luaResult = session.Script.RunFile(lua);
            Console.WriteLine("lua output: {0}", luaResult.Output);
            if (!luaResult.Succeeded)
            {
                Console.Error.WriteLine("Error: Lua script failed: {0}", luaResult.Output);
                return 93;
            }

            object? luaSize = BillSize(session);
            Console.WriteLine("Bill Size after Lua: {0}", luaSize);
            if (!Equals(luaSize, ScriptFiles.ExpectedBillSize))
            {
                Console.Error.WriteLine("Error: Lua script should set Bill Size to {0}.", ScriptFiles.ExpectedBillSize);
                return 94;
            }

            if (session.Script.Debugger.Breakpoints.Count != 0)
            {
                Console.Error.WriteLine("Error: debugger should start with no breakpoints.");
                return 95;
            }

            Console.WriteLine("headless scripts ok");
            return 0;
        }

        private static int ProveDebugger(EditorSession session)
        {
            string? csharp = ScriptFiles.FindSampleCSharpPath();
            string? lua = ScriptFiles.FindSampleLuaPath();
            if (csharp == null || lua == null)
            {
                Console.Error.WriteLine("Error: could not find testdata/scripts/resize-bill.csx or .lua");
                return 100;
            }

            int code = ProvePauseContinue(session, csharp, "C#");
            if (code != 0)
                return code;
            return ProvePauseContinue(session, lua, "Lua");
        }

        private static int ProvePauseContinue(EditorSession session, string path, string label)
        {
            session.New();
            session.Script.Debugger.ClearBreakpoints();
            session.Script.Debugger.SetBreakpoint(path, ScriptFiles.SampleWriteLine);

            object? before = BillSize(session);
            Console.WriteLine("{0} Bill Size before run: {1}", label, before);
            if (!Equals(before, ScriptFiles.DefaultBillSize))
            {
                Console.Error.WriteLine("Error: {0} fixture Bill Size should be {1}.", label, ScriptFiles.DefaultBillSize);
                return 101;
            }

            System.Threading.Tasks.Task<ScriptResult> task = session.Script.BeginRunFile(path);
            if (!session.Script.Debugger.WaitUntilPaused(15000))
            {
                Console.Error.WriteLine("Error: {0} script did not pause on line {1}.", label, ScriptFiles.SampleWriteLine);
                session.Script.Continue();
                task.Wait(5000);
                return 102;
            }

            PauseInfo? pause = session.Script.Debugger.CurrentPause;
            Console.WriteLine("{0} paused at line {1} ({2})", label, pause?.Line, pause?.LanguageId);
            if (pause == null || pause.Line != ScriptFiles.SampleWriteLine)
            {
                Console.Error.WriteLine("Error: {0} pause line should be {1}.", label, ScriptFiles.SampleWriteLine);
                session.Script.Continue();
                task.Wait(5000);
                return 103;
            }

            object? paused = BillSize(session);
            Console.WriteLine("{0} Bill Size while paused: {1}", label, paused);
            if (!Equals(paused, ScriptFiles.DefaultBillSize))
            {
                Console.Error.WriteLine("Error: {0} breakpoint should pause before SetAttribute (Size still {1}).", label, ScriptFiles.DefaultBillSize);
                session.Script.Continue();
                task.Wait(5000);
                return 104;
            }

            bool sawSize = false;
            foreach (WatchValue watch in pause.Watches)
            {
                if (string.Equals(watch.Name, "Bill.size", StringComparison.OrdinalIgnoreCase))
                    sawSize = true;
            }
            if (!sawSize)
            {
                Console.Error.WriteLine("Error: {0} pause watches are missing Bill.size.", label);
                session.Script.Continue();
                task.Wait(5000);
                return 105;
            }

            session.Script.Continue();
            if (!task.Wait(15000))
            {
                Console.Error.WriteLine("Error: {0} script did not finish after Continue.", label);
                return 106;
            }
            if (!task.Result.Succeeded)
            {
                Console.Error.WriteLine("Error: {0} script failed after Continue: {1}", label, task.Result.Output);
                return 107;
            }

            object? after = BillSize(session);
            Console.WriteLine("{0} Bill Size after Continue: {1}", label, after);
            if (!Equals(after, ScriptFiles.ExpectedBillSize))
            {
                Console.Error.WriteLine("Error: {0} Continue should set Bill Size to {1}.", label, ScriptFiles.ExpectedBillSize);
                return 108;
            }

            if (session.Script.Debugger.IsPaused)
            {
                Console.Error.WriteLine("Error: {0} debugger should not stay paused after Continue.", label);
                return 109;
            }

            Console.WriteLine("headless {0} pause/continue ok", label);
            return 0;
        }

        private static int ProveStride(EditorSession session)
        {
            StrideHostResult result = session.Viewport.Result;
            Console.WriteLine("stride package: {0} {1}", StrideHost.PackageId, StrideHost.PackageVersion);
            Console.WriteLine("stride engine loaded: {0}", result.EngineLoaded);
            Console.WriteLine("stride game constructed: {0}", result.GameConstructed);
            Console.WriteLine("stride headless context: {0}", result.HeadlessContextAvailable);
            Console.WriteLine("stride window: {0}", result.WindowTypeName ?? "(none)");
            Console.WriteLine("stride gpu present: {0}", result.StrideGpuPresent);
            if (!string.IsNullOrEmpty(result.PresentError))
                Console.WriteLine("stride device error: {0}", result.PresentError);

            if (!result.EngineLoaded)
            {
                Console.Error.WriteLine("Error: Stride.Engine did not load.");
                return 110;
            }
            if (!result.GameConstructed)
            {
                Console.Error.WriteLine("Error: Stride Game was not constructed. {0}", result.LoadError);
                return 111;
            }
            if (!result.HeadlessContextAvailable)
            {
                Console.Error.WriteLine("Error: GameContextHeadless is not available.");
                return 112;
            }
            if (!result.WindowCreated || string.IsNullOrEmpty(result.WindowTypeName))
            {
                Console.Error.WriteLine("Error: GameContextHeadless did not create a GameWindow.");
                return 114;
            }

            ViewportPresenter presenter = session.Viewport.Presenter;
            for (int i = 0; i < 8; i++)
                presenter.Tick(0.05 * (i + 1));

            Console.WriteLine("stride-rtt: {0}", StrideRttPresenter.StatusLine);
            Console.WriteLine("stride-rtt placeholders: {0}", StrideRttPresenter.PlaceholderCount);
            Console.WriteLine("viewport path: {0}", presenter.ActivePath);
            Console.WriteLine("viewport frames: {0}", presenter.FrameCount);
            Console.WriteLine("viewport size: {0}x{1}", presenter.Width, presenter.Height);
            Console.WriteLine("viewport live: {0}", presenter.IsLiveControl);
            Console.WriteLine("level engine: {0}", session.Level.Engine.GetType().Name);
            Console.WriteLine("bound scene backend: {0}", session.Level.SceneBackend);
            Console.WriteLine("bound scene objects: {0}", session.Level.BoundScene.Count);

            if (presenter.FrameCount < 1)
            {
                Console.Error.WriteLine("Error: viewport presenter produced no frames.");
                return 115;
            }
            if (!presenter.HasNonEmptyFrame)
            {
                Console.Error.WriteLine("Error: viewport bitmap is empty.");
                return 116;
            }
            if (presenter.ActivePath != ViewportPresenter.SoftwarePath &&
                presenter.ActivePath != ViewportPresenter.StrideRttPath)
            {
                Console.Error.WriteLine("Error: viewport present path must be the live control, not status-only.");
                return 117;
            }
            if (!session.Viewport.IsLivePresent)
            {
                Console.Error.WriteLine("Error: ViewportSession.IsLivePresent should be true.");
                return 118;
            }

            var factory = new EditorDockFactory(session);
            IRootDock layout = factory.CreateLayout();
            DockLayoutInfo dock = EditorDockFactory.Describe(layout);
            Console.WriteLine("dock center: {0}", dock.CenterDocumentId);
            Console.WriteLine("dock ids: {0}", string.Join(",", dock.Ids));

            if (dock.CenterDocumentId != "Viewport" ||
                dock.CenterDocumentIds.Count != 1 ||
                dock.CenterDocumentIds[0] != "Viewport")
            {
                Console.Error.WriteLine("Error: center document must be Viewport only.");
                return 119;
            }
            string[] required = { "Viewport", "Objects", "Level", "Script", "Properties", "History" };
            foreach (string id in required)
            {
                if (!dock.Has(id))
                {
                    Console.Error.WriteLine("Error: dock layout is missing {0}.", id);
                    return 120;
                }
            }

            if (presenter.ActivePath == ViewportPresenter.StrideRttPath)
            {
                Console.WriteLine("stride gpu: presenting via stride-rtt (offscreen Texture.GetData)");
                if (StrideRttPresenter.PlaceholderCount < 1)
                {
                    Console.Error.WriteLine("Error: stride-rtt is live but no Level placeholders are bound.");
                    return 121;
                }
            }
            else
                Console.WriteLine("stride gpu: software-writeablebitmap fallback (expected on ubuntu CI without Vulkan)");

            if (session.Level.BoundScene.Count < 1)
            {
                Console.Error.WriteLine("Error: Level bind produced an empty bound scene.");
                return 122;
            }
            if (session.Level.BoundScene.Find("PointLight") == null)
            {
                Console.Error.WriteLine("Error: bound scene is missing PointLight after viewport ticks.");
                return 123;
            }

            Console.WriteLine("headless stride ok");
            return 0;
        }

        private static object? BillSize(EditorSession session)
        {
            GameObjectItem? bill = Find(session, "Bill");
            if (bill == null)
                return null;
            session.SelectedObject = bill;
            PropertyDescriptor? size = FindDescriptor(session, "Size");
            return size?.GetValue(session.PropertyTarget);
        }

        private static GameObjectItem? Find(EditorSession session, string name)
        {
            foreach (GameObjectItem item in session.Objects)
            {
                if (item.Name == name)
                    return item;
            }
            return null;
        }

        private static PropertyDescriptor? FindDescriptor(EditorSession session, string name)
        {
            foreach (PropertyDescriptor descriptor in session.PropertyEditing.PropertyDescriptors)
            {
                if (descriptor.Name == name)
                    return descriptor;
            }
            return null;
        }
    }
}
