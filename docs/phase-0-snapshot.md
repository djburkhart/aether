# Aether Engine — Phase 0 snapshot
**Date:** 22 Aug 2026 (America/New_York)

Public-repo audit only. Nothing cloned. No build attempted yet.

## Verdict

The hybrid plan is still the right one. Stride is a usable **runtime**. It is not a usable **tools host**. SonyWWS ATF / LevelEditor / SLED are the tools DNA, and they are frozen mid-2010s with **zero** public Avalonia or modern-.NET ports.

Phase 0 work is therefore: stand up a new org/monorepo (the name `AetherEngine` is taken), fork the three Sony trees, attribute Apache 2.0, and start a build/reuse catalog. Do not wait on Stride's Avalonia Game Studio.

## Org / naming

| Name | Status |
|---|---|
| [github.com/AetherEngine](https://github.com/AetherEngine) | **Taken.** Zig/SDL3 engine, last push 21 Aug 2026. Unrelated. |
| [github.com/AETHER-ENGINE](https://github.com/AETHER-ENGINE) | Taken, empty/unrelated. |
| [github.com/Resolvora](https://github.com/Resolvora) | **Does not exist.** Available as an org. |
| [github.com/djburkhart](https://github.com/djburkhart) | Your user account (Resolvora LLC). No engine/ATF repos yet. |

Recommendation: new org that is **not** `AetherEngine`. `Resolvora` is free. Product name can stay Aether Engine.

## SonyWWS sources

All three live under [SonyWWS](https://github.com/SonyWWS). Apache-2.0. No GitHub Releases, no Actions/AppVeyor, not archived.

| Repo | Last commit | Stack | Vendors ATF? |
|---|---|---|---|
| [ATF](https://github.com/SonyWWS/ATF) | 8 Nov 2016 | .NET Framework 4.0, WinForms + WPF, MEF, SharpDX D2D, OpenGL | (is ATF) |
| [LevelEditor](https://github.com/SonyWWS/LevelEditor) | 9 Jan 2017 | ATF + WinForms + **C++ DX11** viewport | Yes, subset in `ATF/` |
| [SLED](https://github.com/SonyWWS/SLED) | 26 Jun 2015 | ATF + WinForms + C++ Lua debugger (5.1.4 / 5.2.3) | **No** — expects sibling `wws_atf` |

No public Avalonia / net6 / net8 / net9 port of any of them.

### High-reuse (keep, retarget)

- **ATF:** `Framework/Atf.Core` (`Dom/`, adapters, schema loaders, `ITransactionContext`), `Atf.Gui/Applications` (command/undo, documents, selection). `DevTools/DomGen`.
- **LevelEditor:** `LevelEditorCore` (commands, property editing, services, `GameEngineProxy`), `LevelEditor/DomNodeAdapters`, schemas, `CodeGenDom`.
- **SLED:** `src/sleddebugger` + `sledcore` + `sledluaplugin` (protocol/hooks), `tool/SledShared` (DOM/plugin/services). The language-plugin split is the reusable idea; the WinForms IDE is not.

### Rewrite

All visual UI (WinForms/WPF), DX11 native interop (`LevelEditorNativeRendering`), SharpDX D2D, DockPanelSuite, MEF catalogs, old `.vs2010.sln` project files, Collada/ATGI importers.

### License / attribution

Vanilla Apache 2.0. No `NOTICE` files. Headers: “Copyright © 2014 Sony Computer Entertainment America LLC.” Keep license, mark modified files, retain copyright notices. §6: no Sony / PlayStation trademarks except to describe origin. Third-party notices live under each repo's `ThirdParty/` (and Lua's `wws_lua/license.txt`).

## Stride as runtime

[stride3d/stride](https://github.com/stride3d/stride) — MIT, .NET Foundation, **7,790** stars.

- Last `master` commit: 14 Aug 2026. Stable **4.3.0.2507** (22 Nov 2025). Pre **4.4.0-beta5** (14 Aug 2026).
- TFM: `net10.0` (+ windows/android/ios/macos).
- Graphics: D3D11/12 + Vulkan. macOS is **MoltenVK, not Metal**.
- Physics: `Stride.BepuPhysics` ships; Bullet still default, being phased out.
- Scripting: **C# only**. Assembly reload. VS/Rider debugger. **No Lua.**
- Asset compiler: Assimp via Silk.NET. CLI: `stride` global tool.

### Avalonia Game Studio — not a backbone yet

Official rewrite is on branch [`xplat-editor`](https://github.com/stride3d/stride/tree/xplat-editor), last commit **29 May 2026** (~11 weeks behind master). Runnable prototype. Not the product.

Blocker: [in-editor game rendering #2741](https://github.com/stride3d/stride/issues/2741) still open, no merged PR. Scene/prefab editors depend on it.

WPF Game Studio still hosts the engine as a Win32 child HWND (`GameEngineHost`). That path is Windows-only.

**Reuse from Stride:** runtime, Quantum (typed graph / property / undo), `ITransactionStack`, asset compiler.

**Build ourselves:** Avalonia viewport host, ATF-style DOM/schema, docking shell, plugin model (DI + `AssemblyLoadContext`), SLED-class Lua + DAP debugger.

No public ATF + Stride hybrid exists.

## Phase 0 next actions

1. Pick a GitHub home that is not `AetherEngine`.
2. Fork ATF, LevelEditor, SLED. Add a root `NOTICE` / ATTRIBUTION for SonyWWS.
3. Attempt an ATF `Atf.Core` retarget to `net10.0` (logic-only, no GUI). That is the first real build signal.
4. Catalog DOM / command / property types vs UI-tied types (this snapshot is the starting map).
5. Skeleton docs + CI on the new monorepo. Do not block on Stride `xplat-editor`.

## MVP still stands

Cross-platform editor, scene viewport + properties + undo, basic terrain/object placement, C#/Lua scripting with debugger, play-in-editor, glTF import.

The viewport is the longest pole. Treat it as Aether work, not a Stride gift.
