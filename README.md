# Aether Engine

Tools-first, open-source C# game engine. The authoring tools that helped ship The Last of Us and Killzone titles, modernized into a full cross-platform engine.

This repo is the new monorepo. The SonyWWS originals stay in snapshot forks:

- [djburkhart/ATF](https://github.com/djburkhart/ATF)
- [djburkhart/LevelEditor](https://github.com/djburkhart/LevelEditor)
- [djburkhart/SLED](https://github.com/djburkhart/SLED)

## Status

Phase 0. First build signal: retarget ATF `Atf.Core` (DOM, schema, commands, adapters) to `net10.0`. No GUI. No DX11.

Preferred runtime is [Stride](https://github.com/stride3d/stride). Preferred editor UI is Avalonia. Stride's official Avalonia Game Studio is not ready enough to be our tools host; we build the authoring layer.

## License

Apache 2.0. Heavy attribution to Sony Computer Entertainment America LLC. See [NOTICE](NOTICE) and [LICENSE](LICENSE).

## Docs

- [Phase 0 snapshot](docs/phase-0-snapshot.md)
