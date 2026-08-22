# ATF Atf.Core → net10.0 port notes

Phase 0 first build signal. This is a retarget of SonyWWS `Framework/Atf.Core` into a modern SDK-style class library. The DOM architecture is unchanged. There is no GUI, no Stride runtime, and no Avalonia host in this change.

## Source

| Item | Value |
|---|---|
| Upstream | https://github.com/SonyWWS/ATF |
| Read-only fork used | https://github.com/djburkhart/ATF |
| Original project | `Framework/Atf.Core` (`Atf.Core.vs2010.csproj`) |
| Original TFM | .NET Framework 4.0 |
| Original namespace | `Sce.Atf` (kept) |
| Last meaningful ATF commit | 8 Nov 2016 (`ddf5a4309a509d9f762ed5402c6f214d6f003fd4` on the fork) |
| Destination | `src/Aether.Atf.Core` (`Aether.Atf.Core.csproj`) |
| Destination TFM | `net10.0` |
| Assembly name | `Aether.Atf.Core` |

Verified by compiling `Aether.sln` with the .NET 10 SDK (`10.0.400`).

## What compiled

Most of Atf.Core is portable. **204 of 211** original compile items build as-is or with a local adaptation. The portable subset includes:

- **DOM:** `DomNode`, `DomNodeType`, `DomNodeAdapter`, list/observable adapters, `CustomTypeDescriptorNodeAdapter`, `DomDocument`, `DomResource`
- **Schema:** `XmlSchemaTypeLoader`, `XmlSchemaTypeCollection`, `XmlAttributeInfo` / `XmlAttributeType`, attribute/child rules, validators (`IdValidator`, `UniqueIdValidator`, `ReferenceValidator`, `DataValidator`, `TransactionContext` validators, etc.)
- **Persistence:** `DomXmlReader`, `DomXmlWriter`, `DomNodeSerializer` (custom binary writer, not `BinaryFormatter`)
- **Adapters:** `Adaptation/` (`IAdaptable`, `IAdapter`, `Adapters`, `AdaptableSelection`, `AdaptablePath`, `BindingAdapterObject` as `CustomTypeDescriptor` — not WPF)
- **Transactions:** `ITransactionContext`, `InvalidTransactionException`, `Dom/TransactionContext`, `Dom/TransactionReporter`
- **Selection / documents / resources:** `Selection<T>`, `ActiveCollection<T>`, `IDocument`, `IResource*`, `ResourceService`, `FileSystemResourceFolder`
- **Search/replace:** `SearchAndReplace/` query predicates and `DomNodeQueryable`
- **Scene graph interfaces (data only):** `Rendering/IMesh`, `IScene`, `INode`, … — no SharpDX/OpenGL
- **Vector math:** `VectorMath/` (`Vec2F`/`Vec3F`/`Matrix4F`/…)
- **MEF-hosted services that do not pull GUI:** `Outputs`, `ConsoleOutputWriter`, `MefUtil`, `FileMoveService`, `ResourceService`
- **Utilities:** localization, `UniqueNamer`, `Multimap`, `Path`/`Tree`, `ArgParser`, `MathUtil`, etc.

MEF is **not** a blocker. The original `System.ComponentModel.Composition` APIs (`Export`, `Import`, `ImportMany`, `CompositionContainer`, `IPartImportsSatisfiedNotification`) compile against the [System.ComponentModel.Composition](https://www.nuget.org/packages/System.ComponentModel.Composition/9.0.8) NuGet package. `Microsoft.Extensions.DependencyInjection` was not introduced.

`PointF` / `RectangleF` come from inbox `System.Drawing.Primitives`. `System.Drawing.Common` (GDI+) is not referenced.

## What was adapted

Modified files carry a “Modified 2026 by Resolvora LLC / Aether Engine contributors” line under the original Sony copyright header.

| File | Change |
|---|---|
| `Aether.Atf.Core.csproj` | New SDK-style `net10.0` project. Defines `CS_4;PUBLIC` like the original Release|AnyCPU configuration. |
| `PathUtil.cs` | Replaced `shlwapi.dll` / `Kernel32.QueryDosDeviceW` P/Invoke with `System.IO.Path` (`GetRelativePath`, `GetFullPath`, `IsPathRooted`). `GetCanonicalPath` no longer resolves Windows `subst` devices. `GetCompactedPath` uses a portable ellipsis instead of `PathCompactPathEx`. |
| `VectorMath/Matrix3x2F.cs` | Implicit conversion from `System.Drawing.Drawing2D.Matrix` is compiled only when `SYSTEM_DRAWING_COMMON` is defined. The portable build does not define that symbol and does not reference GDI+. |
| `Resources/wws_atf.component` | Copied from the ATF repo root (originally an embedded resource via `..\..\wws_atf.component`) so `AtfVersion.GetVersion()` still finds the Sony component version string. |

No DOM types were redesigned. Namespace remains `Sce.Atf`.

## What was excluded

### Cut from Atf.Core (hard Windows / third-party)

These were in the original csproj and are **not** in the port:

| File | Reason |
|---|---|
| `CrashLogger.cs` | `Scea.CrashReporter` / `libcrashreport_net` (Sony Recap, Windows-only third-party) |
| `ServerLogger.cs` | `Scea.RecapConnection` (same third-party telemetry) |
| `AtfUsageLogger.cs` | WMI (`System.Management`), `Microsoft.Win32.Registry`, `Kernel32.GetPhysicalMemoryMB`, posts to an internal Sony Recap host |
| `LiveConnectService.cs` | `Wws.LiveConnect` + Bonjour, Windows-only subnet discovery |
| `Kernel32.cs` | `kernel32.dll` P/Invoke (`QueryDosDeviceW`, physical memory, `RtlCopyMemory`) |
| `Shell32.cs` | `Shell32.dll` browse/file-info P/Invoke; unused by other Core files |
| `Properties/AssemblyInfo.cs` | Replaced by SDK-generated assembly metadata; Sony copyright is set on the project |

`ICrashLogger` and `IServerLogger` **interfaces** remain. There is no Recap-backed implementation.

### Not copied (were never in the original compile list)

Leftover / unused sources next to the project, not compiled by `Atf.Core.vs2010.csproj`:

- Root duplicates of search/replace types (`IQueryMatch.cs`, `IQueryableContext.cs`, …) — the compiled copies live under `SearchAndReplace/`
- `Dom/DomNodeQueryable.cs`, `Dom/DomNodeQueryMatch.cs` — same; compiled copies are under `SearchAndReplace/`
- `Dom/XmlPersister.cs` — present on disk in ATF, not in the csproj

### Out of scope (GUI / later phases)

Not ported, per Phase 0 constraints:

- `Framework/Atf.Gui`, `Atf.Gui.WinForms`, `Atf.Gui.Wpf`
- SharpDX / Direct2D / DirectWrite / OpenGL
- DockPanelSuite, WinForms/WPF shells, MEF UI catalogs
- Command / undo / document-service implementations in `Atf.Gui/Applications` (`ICommandClient`, `ICommandService`, `HistoryContext`, …)
- Stride runtime integration

Command *abstractions* for a later tools host live in Atf.Gui, not Atf.Core. Core already has the data-side pieces those commands sit on: `ITransactionContext`, `Selection<T>`, `IDocument`, `IValidationContext`.

## Remaining blockers / debt

These do **not** prevent `dotnet build`. They are the next cuts or cleanups.

1. **Obsolete APIs (4 warnings)**
   - `SYSLIB0005`: `Assembly.GlobalAssemblyCache` in `EmbeddedResourceStringLocalizer` (GAC is gone; the property is always false on modern .NET).
   - `SYSLIB0051`: formatter-based `Exception` serialization ctor in `Dom/AnnotationException`.
   - `SYSLIB0013`: `Uri.EscapeUriString` in `Dom/AttributeType`.
2. **Windows path semantics:** `PathUtil.GetCanonicalPath` no longer unwraps `subst` drives. Callers that depended on that should use a Windows-only helper later if needed.
3. **GDI+ matrix conversion:** `Matrix3x2F(System.Drawing.Drawing2D.Matrix)` is compiled out. Re-enable with `SYSTEM_DRAWING_COMMON` + `System.Drawing.Common` if a Windows tools host needs it.
4. **Telemetry / crash reporting:** no replacement for Recap / `CrashLogger` / `AtfUsageLogger`. Fine for an open-source engine; wire a modern logger if desired.
5. **MEF vs later DI:** Core MEF works. Atf.Gui still uses MEF catalogs heavily. A future tools host may want `Microsoft.Extensions.DependencyInjection` + `AssemblyLoadContext`; that is a later cut, not required here.
6. **Command / undo layer:** still in Atf.Gui. Port a logic-only slice of `Atf.Gui/Applications` next if the editor shell needs it before Avalonia UI.
7. **No unit tests yet.** ATF’s tests lived in a separate Framework project and were not part of this build signal.

## License / attribution

- Apache License 2.0 (`LICENSE`).
- Sony Computer Entertainment America LLC copyright headers are retained on copied sources.
- Modified files are marked.
- Root `NOTICE` is retained and names ATF / LevelEditor / SLED.

## Build

```bash
dotnet build Aether.sln -c Release
```

CI: `.github/workflows/ci.yml` on `ubuntu-latest`. Windows CI is not required; no Windows-only API remains in the compiled set.
