# Aether Engine

Tools-first, open-source C# game engine. The authoring tools that helped ship The Last of Us and Killzone titles, modernized into a full cross-platform engine.

This repo is the new monorepo. The SonyWWS originals stay in snapshot forks:

- [djburkhart/ATF](https://github.com/djburkhart/ATF)
- [djburkhart/LevelEditor](https://github.com/djburkhart/LevelEditor)
- [djburkhart/SLED](https://github.com/djburkhart/SLED)

## Status

Phase 1. The ATF tools core is hosted in a real Avalonia desktop window (`src/Aether.Editor`): menu bar, Dock.Avalonia layout, UsingDom object list, CircuitEditor node graph, TimelineEditor tracks/intervals, LevelEditor object hierarchy, C# / Lua Script pane, property pane, HistoryContext undo, File Open/Save, and a host-level plugin loader (`src/Aether.Plugins`: DI + AssemblyLoadContext). ATF assemblies still use MEF internally. This is an application shell, not the full editor. No Stride viewport. See [PORTING.md](PORTING.md).

Phase 0 (merged): `src/Aether.Atf.Core`, `src/Aether.Atf.Commands`, `src/Aether.Atf.PropertyEditing`, `src/Aether.Atf.DomGen` / `aether-domgen`, and the headless UsingDom sample.

CircuitEditor first slice: `src/Aether.Atf.Circuit` (portable graph interfaces + DOM adapters) and `src/Aether.Circuit` (CircuitEditor schema loader, runtime module types, DomXml helpers). The committed sample is `testdata/atf/CircuitEditor/Example.circuit`.

TimelineEditor first slice: `src/Aether.Atf.Timeline` (portable group/track/interval interfaces) and `src/Aether.Timeline` (TimelineEditor adapters, schema loader, DomXml helpers). The committed sample is `testdata/atf/TimelineEditor/100.timeline`.

LevelEditor first slice: `src/Aether.LevelEditor.Core` (portable GameObject / transform / hierarchy interfaces and a no-op `IGameEngineProxy`) and `src/Aether.Level` (LevelEditor adapters, schema loader, DomXml helpers). The committed sample is `testdata/atf/LevelEditor/LightTest.lvl`. No 3D viewport.

Scripting first slice: `src/Aether.Scripting` hosts C# (Roslyn) and Lua (MoonSharp) in-process. Scripts talk to a loaded document through `ScriptDocument` (`ListObjects` / `GetAttribute` / `SetAttribute` / `Log`). The Avalonia Script pane uses AvaloniaEdit. Not the WinForms SLED IDE and not a DAP product. Committed samples: `testdata/scripts/resize-bill.csx` and `resize-bill.lua`.

Preferred runtime is [Stride](https://github.com/stride3d/stride). Preferred editor UI is Avalonia. Stride's official Avalonia Game Studio is not ready enough to be our tools host; we build the authoring layer.

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

`dotnet run --project src/Aether.Editor` starts the desktop shell (needs a display). File > Open / Save / Save As / New persist UsingDom XML, CircuitEditor `.circuit`, TimelineEditor `.timeline`, and LevelEditor `.lvl` files via Core `DomXmlReader` / `DomXmlWriter`. File Open of `.csx` / `.lua` loads the Script pane; File Save still applies to the last-activated document. Committed samples: `testdata/atf/UsingDom/ogre-adventure-ii.xml`, `testdata/atf/CircuitEditor/Example.circuit`, `testdata/atf/TimelineEditor/100.timeline`, `testdata/atf/LevelEditor/LightTest.lvl`, and `testdata/scripts/resize-bill.{csx,lua}`. Host plugins load from `plugins/` next to the executable (the sample `Hello Aether` contribution becomes a dock pane). `--headless-session` checks UsingDom selection / property edit / undo / XML round-trip, sample plugin DI, CircuitEditor load (9 modules / 8 wires), TimelineEditor load (10 tracks / 60 intervals), LevelEditor load (10 game objects / PointLight translate), then C# and Lua scripts that set Bill Size to 14.

## Docs

- [Phase 0 snapshot](docs/phase-0-snapshot.md)
- [Port notes](PORTING.md)
