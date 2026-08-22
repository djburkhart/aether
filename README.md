# Aether Engine

Tools-first, open-source C# game engine. The authoring tools that helped ship The Last of Us and Killzone titles, modernized into a full cross-platform engine.

This repo is the new monorepo. The SonyWWS originals stay in snapshot forks:

- [djburkhart/ATF](https://github.com/djburkhart/ATF)
- [djburkhart/LevelEditor](https://github.com/djburkhart/LevelEditor)
- [djburkhart/SLED](https://github.com/djburkhart/SLED)

## Status

Phase 1. The ATF tools core is hosted in a real Avalonia desktop window (`src/Aether.Editor`): menu bar, Dock.Avalonia layout, UsingDom object list, CircuitEditor node graph, property pane, HistoryContext undo, File Open/Save, and a host-level plugin loader (`src/Aether.Plugins`: DI + AssemblyLoadContext). ATF assemblies still use MEF internally. This is an application shell, not the full editor. No Stride viewport. See [PORTING.md](PORTING.md).

Phase 0 (merged): `src/Aether.Atf.Core`, `src/Aether.Atf.Commands`, `src/Aether.Atf.PropertyEditing`, `src/Aether.Atf.DomGen` / `aether-domgen`, and the headless UsingDom sample.

CircuitEditor first slice: `src/Aether.Atf.Circuit` (portable graph interfaces + DOM adapters) and `src/Aether.Circuit` (CircuitEditor schema loader, runtime module types, DomXml helpers). The committed sample is `testdata/atf/CircuitEditor/Example.circuit`.

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

`dotnet run --project src/Aether.Editor` starts the desktop shell (needs a display). File > Open / Save / Save As / New persist UsingDom XML and CircuitEditor `.circuit` files via Core `DomXmlReader` / `DomXmlWriter`. Committed samples: `testdata/atf/UsingDom/ogre-adventure-ii.xml` and `testdata/atf/CircuitEditor/Example.circuit`. Host plugins load from `plugins/` next to the executable (the sample `Hello Aether` contribution becomes a dock pane). `--headless-session` checks UsingDom selection / property edit / undo / XML round-trip, sample plugin DI, then CircuitEditor load (9 modules / 8 wires), property edit, add And+wire, and save/reopen.

## Docs

- [Phase 0 snapshot](docs/phase-0-snapshot.md)
- [Port notes](PORTING.md)
