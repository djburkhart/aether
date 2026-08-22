# Aether Engine

Tools-first, open-source C# game engine. The authoring tools that helped ship The Last of Us and Killzone titles, modernized into a full cross-platform engine.

This repo is the new monorepo. The SonyWWS originals stay in snapshot forks:

- [djburkhart/ATF](https://github.com/djburkhart/ATF)
- [djburkhart/LevelEditor](https://github.com/djburkhart/LevelEditor)
- [djburkhart/SLED](https://github.com/djburkhart/SLED)

## Status

Phase 1. The ATF tools core is hosted in a real Avalonia desktop window (`src/Aether.Editor`): menu bar, Dock.Avalonia layout, UsingDom object list, property pane, HistoryContext undo, and File Open/Save of UsingDom XML. This is an application shell, not the full editor. No Stride viewport. See [PORTING.md](PORTING.md).

Phase 0 (merged): `src/Aether.Atf.Core`, `src/Aether.Atf.Commands`, `src/Aether.Atf.PropertyEditing`, `src/Aether.Atf.DomGen` / `aether-domgen`, and the headless UsingDom sample.

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

`dotnet run --project src/Aether.Editor` starts the desktop shell (needs a display). File > Open / Save / Save As / New persist UsingDom XML via Core `DomXmlReader` / `DomXmlWriter`. The committed sample document is `testdata/atf/UsingDom/ogre-adventure-ii.xml`. `--headless-session` checks selection / property edit / undo and an Open → edit → Save As → reopen round-trip without opening a window.

## Docs

- [Phase 0 snapshot](docs/phase-0-snapshot.md)
- [Port notes](PORTING.md)
