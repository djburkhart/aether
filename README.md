# Aether Engine

Tools-first, open-source C# game engine. The authoring tools that helped ship The Last of Us and Killzone titles, modernized into a full cross-platform engine.

This repo is the new monorepo. The SonyWWS originals stay in snapshot forks:

- [djburkhart/ATF](https://github.com/djburkhart/ATF)
- [djburkhart/LevelEditor](https://github.com/djburkhart/LevelEditor)
- [djburkhart/SLED](https://github.com/djburkhart/SLED)

## Status

Phase 1. The ATF tools core is hosted in a real Avalonia desktop window (`src/Aether.Editor`): menu bar, Dock.Avalonia DCC layout (center Viewport, tools around it), UsingDom object list, CircuitEditor node graph, TimelineEditor tracks/intervals, LevelEditor object hierarchy, C# / Lua Script pane, live Viewport presenter, property pane, HistoryContext undo, File Open/Save, and a host-level plugin loader (`src/Aether.Plugins`: DI + AssemblyLoadContext). ATF assemblies still use MEF internally. This is an application shell, not the full editor. The Viewport presents Stride GPU frames via render-to-texture when a device exists, otherwise a software cube (ubuntu CI has no Vulkan). #2741 is still open. See [PORTING.md](PORTING.md).

Phase 0 (merged): `src/Aether.Atf.Core`, `src/Aether.Atf.Commands`, `src/Aether.Atf.PropertyEditing`, `src/Aether.Atf.DomGen` / `aether-domgen`, and the headless UsingDom sample.

CircuitEditor first slice: `src/Aether.Atf.Circuit` (portable graph interfaces + DOM adapters) and `src/Aether.Circuit` (CircuitEditor schema loader, runtime module types, DomXml helpers). The committed sample is `testdata/atf/CircuitEditor/Example.circuit`.

TimelineEditor first slice: `src/Aether.Atf.Timeline` (portable group/track/interval interfaces) and `src/Aether.Timeline` (TimelineEditor adapters, schema loader, DomXml helpers). The committed sample is `testdata/atf/TimelineEditor/100.timeline`.

LevelEditor first slice: `src/Aether.LevelEditor.Core` (portable GameObject / transform / hierarchy interfaces and a no-op `IGameEngineProxy`) and `src/Aether.Level` (LevelEditor adapters, schema loader, DomXml helpers). The committed sample is `testdata/atf/LevelEditor/LightTest.lvl`. No 3D viewport.

Scripting first slice: `src/Aether.Scripting` hosts C# (Roslyn) and Lua (MoonSharp) in-process. Scripts talk to a loaded document through `ScriptDocument` (`ListObjects` / `GetAttribute` / `SetAttribute` / `Log`). `IDebugger` pauses Run on a breakpoint (wait handle + Continue). The Avalonia Script pane uses AvaloniaEdit with a gutter. Not the WinForms SLED IDE and not a DAP product. Committed samples: `testdata/scripts/resize-bill.csx` and `resize-bill.lua`.

Preferred runtime is [Stride](https://github.com/stride3d/stride). Preferred editor UI is Avalonia. Stride's official Avalonia Game Studio is not ready enough to be our tools host; we build the authoring layer.

Stride viewport: `src/Aether.Stride` references `Stride.Engine` **4.4.0-beta5**. The center Viewport copies frames into an Avalonia `WriteableBitmap`. `StrideRttPresenter` creates a long-lived `GraphicsDevice` (no `Game.Run` loop) and reads an offscreen target; ubuntu CI has no Vulkan so the software cube stays live. When a device exists, `StrideGameEngine` implements `IGameEngineProxy` and LightTest.lvl GameObjects appear as cube placeholders (transforms follow `ITransformable`). Without a device, `NullGameEngine` stays the backend and the bound scene is still a CPU snapshot for headless. One `ViewportCamera` (yaw / pitch / distance / target) drives LookAt for pick, the translate gizmo, and RTT: right-drag or Alt+left orbits, middle-drag or Shift+right pans, wheel zooms. A click in the Viewport Image CPU-picks the nearest placeholder (`ViewportSceneCamera` ray vs cube AABB) and sets Level selection; a miss clears it. A selected GameObject shows a transform gizmo (`W` translate / `E` rotate / `R` scale, or the Viewport toolbar). Dragging writes `ITransformable.Translation`, `Rotation`, or `Scale` through `HistoryContext` so Undo restores the old values. Play / Pause / Stop (F5 / F6 / Shift+F5, Game menu, Viewport toolbar) tick the engine with `UpdateType.GamePlay` / `Paused` / `Editing`. Play snapshots `ITransformable` TRS; Stop restores that snapshot outside History. While Playing, PointLight yaws as a GamePlay-only proof (no physics). Gizmos and pick-to-move are disabled until Stop. Headless exposes `LevelSession.Select(name)` / `PickAt` / `BeginAxisDrag` + `ApplyAxisDelta` / `BeginRotateDrag` + `ApplyRotateDelta` / `BeginScaleDrag` + `ApplyScaleDelta`, `ViewportSession.OrbitBy` / `PanBy` / `ZoomBy`, and `LevelSession.Play` / `Pause` / `Stop`. On Windows with D3D/Vulkan, `--headless-session` should print `viewport path: stride-rtt` plus bound-scene object counts, the same PointLight pick lines, a camera orbit that moves PointLight's pick pixel, PointLight translate X / rotate Y / scale X before / after / after Undo, and Play / Pause / Stop with PointLight yaw + TRS restore. #2741 is still open (this is not an official Avalonia Game control).

## License

Apache 2.0. Heavy attribution to Sony Computer Entertainment America LLC. See [NOTICE](NOTICE) and [LICENSE](LICENSE).

## Build

Requires the .NET 10 SDK.

```bash
dotnet build Aether.sln -c Release
dotnet run -c Release --project src/Aether.Editor
dotnet run -c Release --project src/Aether.Editor -- --headless-session
dotnet run -c Release --project samples/UsingDom
```

`dotnet run --project src/Aether.Editor` starts the desktop shell (needs a display). File > Open / Save / Save As / New persist UsingDom XML, CircuitEditor `.circuit`, TimelineEditor `.timeline`, and LevelEditor `.lvl` files via Core `DomXmlReader` / `DomXmlWriter`. File Open of `.csx` / `.lua` loads the Script pane; File Save still applies to the last-activated document. Committed samples: `testdata/atf/UsingDom/ogre-adventure-ii.xml`, `testdata/atf/CircuitEditor/Example.circuit`, `testdata/atf/TimelineEditor/100.timeline`, `testdata/atf/LevelEditor/LightTest.lvl`, and `testdata/scripts/resize-bill.{csx,lua}`. Host plugins load from `plugins/` next to the executable (the sample `Hello Aether` contribution becomes a dock pane). `--headless-session` checks UsingDom selection / property edit / undo / XML round-trip, sample plugin DI, CircuitEditor load (9 modules / 8 wires), TimelineEditor load (10 tracks / 60 intervals), LevelEditor load (10 game objects / PointLight translate) plus a bound scene (count and PointLight by name, including after name/translate edit), a Viewport CPU pick of PointLight (name + projected pixel; miss clears selection), a Viewport translate of PointLight along +X (`BeginAxisDrag` / `ApplyAxisDelta`) with Undo restoring X, a Viewport rotate of PointLight around +Y (`BeginRotateDrag` / `ApplyRotateDelta`) with Undo restoring Rotation, a Viewport scale of PointLight along +X (`BeginScaleDrag` / `ApplyScaleDelta`) with Undo restoring Scale, a documented Viewport orbit + zoom that moves PointLight's pick pixel (then pick + gizmo +X / Undo again), Play / Pause / Stop of the bound Level (`LevelSession.Play` / `Pause` / `Stop`) with GamePlay yaw of PointLight and Stop restoring the pre-play TRS outside History (then gizmo +X / Undo again), C# and Lua scripts that set Bill Size to 14, pause/continue on a breakpoint before that write, then a live Viewport presenter (frameCount ≥ 1) plus the DCC dock (Viewport center, tools around it).

## Docs

- [Phase 0 snapshot](docs/phase-0-snapshot.md)
- [Port notes](PORTING.md)
