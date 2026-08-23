# ATF Atf.Core → net10.0 port notes

Phase 0 first build signal. This is a retarget of SonyWWS `Framework/Atf.Core` into a modern SDK-style class library. The DOM architecture is unchanged. The Avalonia host is a later Phase 1 slice (`src/Aether.Editor`, below).

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

- `Atf.Gui.WinForms`, `Atf.Gui.Wpf`
- SharpDX / Direct2D / DirectWrite / OpenGL
- DockPanelSuite, WinForms/WPF shells, MEF UI catalogs
- The rest of `Framework/Atf.Gui` (listers, property-grid editors, D2D, printing)
- Stride runtime integration

Command / undo / document-service logic is now in `src/Aether.Atf.Commands` (see below). Core still owns the data-side pieces those commands sit on: `ITransactionContext`, `Selection<T>`, `IDocument`, `IValidationContext`.

## Remaining blockers / debt

These do **not** prevent `dotnet build`. They are the next cuts or cleanups.

1. **Obsolete APIs (4 warnings)**
   - `SYSLIB0005`: `Assembly.GlobalAssemblyCache` in `EmbeddedResourceStringLocalizer` (GAC is gone; the property is always false on modern .NET).
   - `SYSLIB0051`: formatter-based `Exception` serialization ctor in `Dom/AnnotationException`.
   - `SYSLIB0013`: `Uri.EscapeUriString` in `Dom/AttributeType`.
2. **Windows path semantics:** `PathUtil.GetCanonicalPath` no longer unwraps `subst` drives. Callers that depended on that should use a Windows-only helper later if needed.
3. **GDI+ matrix conversion:** `Matrix3x2F(System.Drawing.Drawing2D.Matrix)` is compiled out. Re-enable with `SYSTEM_DRAWING_COMMON` + `System.Drawing.Common` if a Windows tools host needs it.
4. **Telemetry / crash reporting:** no replacement for Recap / `CrashLogger` / `AtfUsageLogger`. Fine for an open-source engine; wire a modern logger if desired.
5. **MEF vs later DI:** Core and Commands MEF work. A future tools host may want `Microsoft.Extensions.DependencyInjection` + `AssemblyLoadContext`; that is a later cut, not required here.
6. **Command host:** `CommandServiceBase` is still abstract (`RunContextMenu` is the UI hook). WinForms/WPF `CommandService` subclasses were not ported. An Avalonia host should subclass the base.
7. **No unit tests yet.** ATF’s tests lived in a separate Framework project and were not part of this build signal.

---

# ATF Atf.Gui Applications → net10.0 command slice

Second Phase 0 build signal. Logic-only command / undo / history / document-service types from `Framework/Atf.Gui` retargeted as `src/Aether.Atf.Commands`. Depends on `Aether.Atf.Core`. No GUI assemblies.

## Source

| Item | Value |
|---|---|
| Upstream area | `Framework/Atf.Gui/Applications` plus `Atf.Gui/Dom` history adapters and `Atf.Gui/Input` |
| Destination | `src/Aether.Atf.Commands` (`Aether.Atf.Commands.csproj`) |
| Destination TFM | `net10.0` |
| Assembly name | `Aether.Atf.Commands` |
| Namespaces | `Sce.Atf`, `Sce.Atf.Applications`, `Sce.Atf.Input`, `Sce.Atf.Dom`, `Sce.Atf.Controls.PropertyEditing` (kept) |

Verified by compiling `Aether.sln` with the .NET 10 SDK (`10.0.400`).

## Cut line (verified)

The hypothesis held: a logic-only project compiles if WinForms menu/toolbar/status hosts are left behind.

**The command architecture is not WinForms.** `ICommandService` / `CommandServiceBase` / `CommandInfo` use ATF’s own `Sce.Atf.Input.Keys` (copied from WinForms `Keys`, already abstracted). `RunContextMenu(IEnumerable, Point)` is the only UI hook on the service; it is `abstract` on `CommandServiceBase`. Concrete `CommandService` types live in `Atf.Gui.WinForms` and `Atf.Gui.Wpf` and were not copied.

`Point` and `Color` come from inbox `System.Drawing.Primitives`. `System.Drawing.Common` is not referenced.

## What compiled

87 C# files. Includes:

- **Command service:** `ICommandClient`, `ICommandService`, `CommandServiceBase`, `CommandInfo`, `CommandId`, `MenuInfo`, `CommandState`, `CommandVisibility`, `StandardCommand` / `StandardMenu` / `StandardCommandGroup`
- **Undo stack:** `Command`, `CommandHistory`, `CommandCount`, `CompositeCommand`, list/property/selection commands
- **Standard clients:** `StandardEditHistoryCommands`, `StandardFileCommands` (this *is* `IDocumentService`), `StandardSelectionCommands`, `StandardLockCommands`, `StandardShowCommands`, `RecentDocumentCommands`, `HelpAboutCommand` (abstract `ShowHelpAbout`)
- **Registries:** `DocumentRegistry`, `ContextRegistry`
- **Context interfaces:** `ISelectionContext`, `IHistoryContext`, `IInstancingContext`, `INamingContext`, `IVisibilityContext`, `IViewingContext`, insertion/property/help/coloring
- **Dialog contracts (no hosts):** `IFileDialogService`, `IMessageBoxService`, `ISettingsService`
- **Watchers:** `FileWatcherService`, `DirectoryWatcherService` (`ISynchronizeInvoke` is optional)
- **DOM adapters:** `HistoryContext`, `GlobalHistoryContext`, `MultipleHistoryContext`, `SelectionContext`, `EditingContext`
- **Input:** `Keys`, `KeyEventArgs`, `KeysUtil`, plus mouse/key event arg types used by the command layer
- **Support:** `BoundPropertyDescriptor`, `FileFilterBuilder`, `Resources` (name table only), `ImageResourceAttribute`, `CursorResourceAttribute`

`StandardFileCommands` is the document service implementation. There is no separate `DocumentService` class in Atf.Gui; WinForms only supplied file-dialog and command-host subclasses.

## What was adapted

| File | Change |
|---|---|
| `Resources.cs` | Static constructor assigns image/cursor *names* from attributes. It no longer reflects for WinForms/WPF `ResourceUtil` (GDI+ load). |
| `Applications/HelpAboutCommand.cs` | Removed unused `using Sce.Atf.Controls` (About dialog is WinForms). |
| `Applications/IStatusImage.cs` | `Image` is `object` instead of `System.Drawing.Image`. Status-bar hosts can store any bitmap-like object. |

## What was excluded

UI hosts and adjacent GUI:

- `Atf.Gui.WinForms` / `Atf.Gui.Wpf` entirely (`CommandService`, `FileDialogService`, `StatusService`, About dialog)
- `StatusService`, `PluginManagerForm` / `PluginManagerService`
- `AssetLister`, `GridControlAdapter`, lister `ItemInfo`, search/replace toolstrips
- `TextPrintDocument`, `CanvasPrintDocument`, `IPrintableDocument`
- `WindowLayoutService` / `IDockStateProvider` / `StandardDockAreas`
- `ISearchableContextUI`, `IPaletteService`, `IThumbnailResolver`
- `SettingsServiceBase` (`BinaryFormatter` + `PresentUserSettings` UI). `ISettingsService` remains; `CommandServiceBase` imports it optionally.
- `StandardViewCommands` — constructor requires `ScriptingService` (IronPython / `Microsoft.Scripting`)
- `ScriptingService`, NetworkTarget*, VersionControl UI, Sony WebServices
- Property-grid editors (`ColorEditor`, …), D2D, DirectWrite, `User32`/`Gdi32`

## Remaining blockers / debt (commands)

1. **No concrete `ICommandService` host.** A tools host must subclass `CommandServiceBase` and implement `RunContextMenu`.
2. **No concrete `IFileDialogService` / `IMessageBoxService`.** `StandardFileCommands` needs them at runtime (MEF imports). Provide Avalonia (or headless) implementations later.
3. **`IStatusImage.Image` type change** — WinForms status panels that assigned `System.Drawing.Image` need a one-line adapter.
4. **`SettingsServiceBase` not ported.** Keyboard-shortcut persistence in `CommandServiceBase.Initialize` is a no-op until an `ISettingsService` is supplied.
5. **`StandardViewCommands` / scripting** still behind the IronPython dependency.
6. **No unit tests.**

---

# ATF property-editing logic → net10.0

Third Phase 0 build signal. Logic-only property-editing types from `Framework/Atf.Gui` retargeted as `src/Aether.Atf.PropertyEditing`. Depends on `Aether.Atf.Core` and `Aether.Atf.Commands`. No GUI assemblies.

## Source

| Item | Value |
|---|---|
| Upstream area | `Framework/Atf.Gui/Controls/PropertyEditing`, `Framework/Atf.Gui/Dom` property-descriptor adapters, `IAnnotatedParams` |
| Destination | `src/Aether.Atf.PropertyEditing` (`Aether.Atf.PropertyEditing.csproj`) |
| Destination TFM | `net10.0` |
| Assembly name | `Aether.Atf.PropertyEditing` |
| Namespaces | `Sce.Atf`, `Sce.Atf.Applications`, `Sce.Atf.Controls.PropertyEditing`, `Sce.Atf.Dom` (kept) |

Verified by compiling `Aether.sln` with the .NET 10 SDK (`10.0.400`).

## Layout decision

A **new project** is cleaner than folding this into Core or Commands.

- **Not Core.** Core already owns the DOM-side TypeDescriptor pieces (`CustomTypeDescriptorNodeAdapter`, `IPropertyValueValidator`, `BindingAdapterObject`, `ObservableDomNodeAdapter`). The Atf.Gui descriptors sit *above* that and need `IPropertyEditingContext`, `ISelectionContext`, and `Sce.Atf.Input.Keys`. Pulling those into Core would invert the Commands dependency.
- **Not Commands.** Commands only needed `BoundPropertyDescriptor` for settings persistence. Moving that type here would cycle: `CommandServiceBase` needs it, and `SelectionPropertyEditingContext` needs Commands types.
- **Commands reference is required.** `IPropertyEditingContext` and `ISelectionContext` live in Commands. `PropertyUtils.IsEditKey` uses ATF’s own `Keys`. Nothing else from Commands is needed at compile time.

`BoundPropertyDescriptor` and `IPropertyEditingContext` stay in Commands. They are **not** duplicated here.

## Cut line (verified)

The hypothesis held. ATF’s property framework is TypeDescriptor / custom descriptor / editing-context / converter logic. WinForms `PropertyGrid` / `PropertyView` / `IPropertyEditor` and WPF value editors are the UI skin.

`IPropertyEditor` lives in `Atf.Gui.WinForms` (`GetEditingControl` returns `System.Windows.Forms.Control`) and was not copied. `PropertyEditorControlContext` and `TypeDescriptorContext` are also WinForms. Schema annotations still store an `object editor` on `Sce.Atf.Dom.PropertyDescriptor`; a later Avalonia host can supply its own editor objects.

`Color` comes from inbox `System.Drawing.Primitives` (`IntColorConverter` uses `Color.FromArgb` / `ToArgb`). `System.Drawing.Common` is not referenced.

## What compiled

34 C# files. No source adaptations were required.

- **DOM descriptors (above Core):** `Sce.Atf.Dom.PropertyDescriptor`, `AttributePropertyDescriptor`, `ChildPropertyDescriptor`, `ChildAttributePropertyDescriptor`, `ChildAttributeCollectionPropertyDescriptor`, `MultiPropertyDescriptor`, `ObservableCustomTypeDescriptorNodeAdapter`, `DomNodeTypeExtensions.RegisterDescriptor`
- **Editing contexts:** `PropertyEditingContext`, `SelectionPropertyEditingContext` (adapts any `ISelectionContext`, raises `Reloaded` via `IValidationContext`)
- **Descriptor adapters:** `UnboundPropertyDescriptor`, `PropertyCollectionWrapper`, `IDynamicTypeDescriptor`, `IPropertyCustomSorter`
- **Converters:** `BoundedFloatConverter`, `BoundedIntConverter`, `EnumTypeConverter`, `ExclusiveEnumTypeConverter`, `FlagsTypeConverter`, `FloatArrayConverter`, `IntColorConverter`, `IntEnumTypeConverter`, `ReadOnlyConverter`, `UniformFloatArrayConverter`
- **Utils / events:** `PropertyUtils`, `PropertyEditedEventArgs`, `PropertyErrorEventArgs`, `IAnnotatedParams` (schema-annotation hook used by converters and `PropertyDescriptor.ParseXml`)
- **Grid metadata (no controls):** `PropertyGridMode`, `PropertySorting`, `CustomizeAttribute`
- **Control contracts (no implementations):** `IPropertyEditingControlOwner`, `ICacheablePropertyControl`
- **Lists:** `SortableBindingList<T>` (inbox `BindingList` / `IBindingListView`)

`PropertyDescriptor.ParseXml` still constructs editor/converter instances from schema annotation type names via `Activator.CreateInstance` + `IAnnotatedParams.Initialize`. WinForms editor type names in existing schemas will fail at runtime until a portable editor is registered; that is expected.

## What was adapted

None. Copied sources compiled against `net10.0` as-is. Sony copyright headers are unchanged. The new `.csproj` is the only new file that is not a copy.

## What was excluded

### From `Atf.Gui/Controls/PropertyEditing` (UI or already ported)

| File | Reason |
|---|---|
| `ColorEditor.cs` | Extends `System.Drawing.Design.ColorEditor` (GDI+ `UITypeEditor`) |
| `DateTimeEditor.cs` | Extends `System.ComponentModel.Design.DateTimeEditor` |
| `EmbeddedPropertyView.cs` | Commented-out WinForms leftover |
| `PropertyChangedExtensions.cs` | `internal`; only used by circuit-graph UI |
| `BoundPropertyDescriptor.cs` | Already in `Aether.Atf.Commands` |

### From `Atf.Gui/Dom` (not this slice)

| File | Reason |
|---|---|
| `CustomTypeDescriptorNodeAdapter.cs` | Older leftover. The live type is already in `Aether.Atf.Core`. |
| `BindingAdapter.cs` | WPF `Path=As.DomDocument.*` helper around Core’s `BindingAdapterObject` |

### Adjacent Atf.Gui (not property logic)

- `GroupAttribute` — `DataBoundListView` column grouping
- `EnumDisplayUtil` — Core already has `EnumUtil` + `DisplayStringAttribute`

### WinForms / WPF property-grid skin (not copied)

`Atf.Gui.WinForms/Controls/PropertyEditing/` (`IPropertyEditor`, `PropertyGrid`, `PropertyGridView`, `PropertyView`, `PropertyEditorControlContext`, `BoundedIntEditor` / `BoundedFloatEditor`, `EnumUITypeEditor`, `FlagsUITypeEditor`, `ColorPickerEditor`, `NumericEditor`, `GridControl`, collection/array editors, …) and `Atf.Gui.Wpf/Controls/PropertyEditing/` value editors.

### LevelEditor

`LevelEditorCore` PropertyEditing is **not** in this PR. ATF first.

## Remaining blockers / debt (property editing)

1. **No property-grid host.** `IPropertyEditor` / WinForms `PropertyGrid` were left behind. An Avalonia (or headless) host must bind to `IPropertyEditingContext` + descriptors.
2. **Schema `editor=` type names** still point at WinForms `UITypeEditor` types. Converters (`IAnnotatedParams`) work; visual editors do not exist in this assembly.
3. **`IPropertyValueValidator`** is already in Core and was not wired into these descriptors (same as upstream Atf.Gui).
4. **No unit tests.**

---

# ATF DomGen → net10.0 schema codegen

Fourth Phase 0 build signal. SonyWWS `DevTools/DomGen` retargeted as a library plus a `dotnet` CLI. Reuses `Aether.Atf.Core` `XmlSchemaTypeLoader`. No Visual Studio custom-tool host.

## Source

| Item | Value |
|---|---|
| Upstream area | `DevTools/DomGen` (`SchemaLoader`, `SchemaGen`, `Program`) |
| Related, not ported | `DevTools/CustomToolDomGen` (VS COM custom tool), `DevTools/MsBuildUtils` (VS project parser, not schema codegen) |
| Destination library | `src/Aether.Atf.DomGen` (`Aether.Atf.DomGen.csproj`) |
| Destination CLI | `src/Aether.Atf.DomGen.Cli` (`aether-domgen`) |
| Destination TFM | `net10.0` |
| Namespaces | `DomGen` (library, kept), `Aether.Atf.DomGen.Cli` (host) |

Verified by compiling `Aether.sln` with the .NET 10 SDK (`10.0.400`) and running `aether-domgen --check` against ATF’s UsingDom `game.xsd` / `GameSchema.cs`.

## Layout decision

**Library + CLI.** The emit API is useful without a process (tests, later MSBuild). The original `DomGen.exe` host is a four-argument argv parser plus an optional MD5 cache; that belongs in a tool, not in Core.

- **Not folded into Core.** `XmlSchemaTypeLoader` already lives there. DomGen is a *consumer* that walks loaded types and writes C#.
- **Not a VS custom tool.** `CustomToolDomGen` is `BaseCodeGeneratorWithSite` + HKLM COM registration for VS 2002–2010. Left behind. `aether-domgen` produces the same C#.
- **Not MsBuildUtils.** That project parses `.csproj` / `.sln` XML. It is not schema codegen.

## Cut line (verified)

The hypothesis held. DomGen is XML schema walk + C# text emit. `SchemaLoader` subclasses Core’s `XmlSchemaTypeLoader` and captures `<sce.domgen>` annotations. `SchemaGen.Generate` writes the static schema class (and optional adapters/enums). The only IDE-specific piece was `CustomToolDomGen`.

## What compiled

- **Library:** `SchemaLoader`, `SchemaGen`, `SchemaGenOptions`
- **CLI:** `aether-domgen` (`PackAsTool`, command name `aether-domgen`)
- **Smoke compile:** `tests/Aether.Atf.DomGen.Generated` compiles the committed UsingDom `GameSchema.cs` against Core

`aether-domgen testdata/atf/UsingDom/game.xsd testdata/atf/UsingDom/GameSchema.cs Game.UsingDom UsingDom --check` matches the ATF-generated fixture (CRLF-normalized). That is the same invocation as `Samples/UsingDom/Schemas/GenSchemaDef.bat`.

## What was adapted

| File | Change |
|---|---|
| `SchemaGen.cs` | `SchemaGenOptions` overload so hosts are not forced to fake argv. `-enums` uses public `StringEnumRule.Values` instead of reflecting private `m_values`. Generated text always uses `\n`. Enum members keep a trailing comma (legal C#; avoids the original `Length-1` newline strip). |
| `SchemaLoader.cs` | Added the missing Sony copyright header. |
| `Program.cs` (CLI) | New non-interactive host: flags, `--help` with examples, `--stdout`, `--dry-run`, `--check`, original positional argv, original `-a` / `-annotatedOnly` / `-enums` / `-cache`. Exit 1 on usage errors (upstream printed usage and returned 0). `[STAThread]` dropped. |

## What was excluded

| Item | Reason |
|---|---|
| `CustomToolDomGen` | Visual Studio COM custom tool (`Microsoft.CustomTool`, registry) |
| `MsBuildUtils` | VS project/solution parser, not DomGen |
| `Localization` DevTool | Resource localization, not schema codegen |
| LevelEditor `CodeGenDom` | LevelEditor is out of this PR. ATF UsingDom `game.xsd` is the smoke schema. |
| `DomGen/schemas/colladaschema_131.xsd`, `atgi.xsd` | Large importer schemas; not needed to prove emit |
| ATF `Test/UnitTests/DomGen/TestSchemaGen.cs` | NUnit + multi-file include/import fixtures. `--check` on UsingDom is the Phase 0 smoke. |

## Remaining blockers / debt (DomGen)

1. **No MSBuild task yet.** Call `aether-domgen` from a target or use the library from a later task.
2. **`--cache` MD5** is the original hash; fine for up-to-date checks, not a security construct.
3. **Unit tests** for `-annotatedOnly` / imports (`test_customized.xsd`) were not ported.
4. **`dotnet tool install`** is not published to NuGet in this PR; run the project (`dotnet run --project src/Aether.Atf.DomGen.Cli`).

---

# UsingDom headless sample

Phase 0 stack proof. `samples/UsingDom` loads ATF `Samples/UsingDom` `game.xsd`, uses DomGen `GameSchema` types, builds the same in-memory document ATF’s sample constructs in code, and edits attributes through `PropertyEditingContext` / `AttributePropertyDescriptor`.

UsingDom has **no XML instance file** in the ATF repo. The document is `CreateGameUsingDomNode()`: game “Ogre Adventure II”, ogre Bill (size 12, strength 100), dwarf Sally (age 32, experience 55), tree “Mr. Oak”. This sample uses that graph, then writes the edited DOM with `DomXmlWriter`.

## What compiled

- `samples/UsingDom.Document` — shared `GameSchemaLoader`, `GameDocument.CreateOgreAdventureII`, transactioning attribute descriptors
- `GameSchemaLoader` — file-path load of `testdata/atf/UsingDom/game.xsd`, `GameSchema.Initialize`, `ObservableCustomTypeDescriptorNodeAdapter`, `HistoryContext`, `SelectionContext`, descriptor registration
- `Program` — ATF document construction, property-edit before/after on Bill and Sally, XML dump
- Linked `GameSchema.cs` fixture (not a second copy). Sample build runs `aether-domgen --check` so the fixture cannot drift.

## What was adapted

| File | Change |
|---|---|
| `GameSchemaLoader.cs` | Load from testdata path instead of embedded `ResourceStreamResolver`. Register property-editing extensions/descriptors. Dropped Game/Ogre/Dwarf adapters and validators (not needed for this proof). |
| `Program.cs` | Same document as ATF `CreateGameUsingDomNode`. Added `PropertyEditingContext` + `PropertyUtils.SetProperty` and XML print. No `data\game.xml` side-effect. |

## What was excluded

- UsingDom `DomNodeAdapters` (Game / GameObject / Ogre / Dwarf)
- WinForms/WPF, Avalonia, Stride
- Invented schemas or fake documents

## Run

```bash
dotnet build Aether.sln -c Release
dotnet run -c Release --project samples/UsingDom
```

CI runs the same `dotnet run` after the DomGen `--check` smoke.

---

# Phase 1 Avalonia application shell

First tools-host slice. A real Avalonia 12 desktop window that hosts the already-ported ATF core against the UsingDom document. Not the full editor: no palette, no viewport, no MEF catalog. Open/Save of UsingDom XML is in this slice.

## Source

| Item | Value |
|---|---|
| Destination | `src/Aether.Editor` (`Aether.Editor.csproj`) |
| Shared document | `samples/UsingDom.Document` (schema load + Ogre Adventure II graph) |
| Destination TFM | `net10.0` |
| UI | Avalonia 12.1.0, `StartWithClassicDesktopLifetime` |
| Docking | [Dock.Avalonia](https://github.com/wieslawsoltes/Dock) 12.1.0.4 + `Dock.Model.Mvvm` + `Dock.Avalonia.Themes.Fluent` |
| Property grid | [bodong.Avalonia.PropertyGrid](https://github.com/bodong1987/Avalonia.PropertyGrid) 12.0.4.1 |
| Window title | `Aether` or the open filename, plus ` *` when dirty (ATF origin is in Help > About and `NOTICE` only) |

Verified by compiling `Aether.sln` with the .NET 10 SDK (`10.0.400`) on Linux. `dotnet run --project src/Aether.Editor -- --headless-session` constructs the same `EditorSession` the window hosts. A display is required to open the desktop window; this change does not claim the GUI was clicked in CI.

## Why these libraries

**Dock.Avalonia** is the maintained Avalonia docking system (wieslawsoltes). This slice uses the documented view-model + `DataTemplate` pattern: `ObjectsDocument` / `PropertiesTool` / `HistoryTool` subclasses of Dock `Document`/`Tool`, mapped in `App.axaml`. `Document.Content` is not a property on `Dock.Model.Mvvm` 12.1; do not invent a splitter-only “dock”.

**bodong.Avalonia.PropertyGrid** is a maintained Avalonia grid that binds `DataContext` and documents support for `ICustomTypeDescriptor` / custom `PropertyDescriptor`s. The pane binds to `node.As<ICustomTypeDescriptor>()` (`ObservableCustomTypeDescriptorNodeAdapter`), not the raw `DomNode`. WinForms `PropertyGrid` was not ported.

## What compiled

- Main window, File / Edit / Help menus, Dock.Avalonia layout (Objects document + Properties + History tools)
- Object list: Bill (ogre), Sally (dwarf), Mr. Oak (tree)
- Selection → `SelectionContext` + `SelectionPropertyEditingContext`; property grid `DataContext` = ATF adapter
- `TransactioningAttributePropertyDescriptor` wraps `SetValue` in `HistoryContext.DoTransaction` so grid edits are undoable
- Edit > Undo/Redo and a History lister call `HistoryContext` / `CommandHistory` directly
- `EditorCommandService : CommandServiceBase` stubs `RunContextMenu` (no-op). Menus are Avalonia controls; ATF `ICommandService` is not the host.
- `--headless-session` smoke: select Bill, edit Size through descriptors, undo, then Open/Save As/reopen a UsingDom XML file, then resolve the sample plugin from DI

## File Open / Save

The smaller correct path is Core `DomXmlWriter` / `DomXmlReader` plus Avalonia `StorageProvider`. `GameDocument.WriteXml` / `ReadXml` share that format with the headless UsingDom sample.

`StandardFileCommands` / `IDocumentService` were **not** wired. That host needs MEF `IDocumentClient` (Open/Save/Show/Close), `IDocumentRegistry`, a live `ICommandService` command table, and `IFileDialogService`. This slice has one document and Avalonia file pickers; adding that stack would be a later command-host cut, not persistence.

| Item | Value |
|---|---|
| Format | ATF `DomXmlWriter` (UTF-8, tab indent, the same XML the UsingDom sample prints) |
| Fixture | `testdata/atf/UsingDom/ogre-adventure-ii.xml` (Ogre Adventure II / Bill / Sally / Mr. Oak) |
| New | `GameDocument.CreateOgreAdventureII()` |
| Dialogs | Avalonia `IStorageProvider` (not WinForms `FileDialogService`) |
| Dirty title | filename or `Aether`, plus ` *` when `HistoryContext.Dirty` |
| Shortcuts | Ctrl+N / Ctrl+O / Ctrl+S / Ctrl+Shift+S |

Open replaces the session graph, rebinds selection / property editing, and clears undo history. Save writes the current `DomNode` tree.

---

# Host plugins (DI + AssemblyLoadContext)

First slice of Aether’s modern plugin system. This replaces the **host-level** MEF catalog for *new* editor extensions. It does **not** rewrite `Aether.Atf.Core` / Commands / PropertyEditing off `System.ComponentModel.Composition`.

## Source

| Item | Value |
|---|---|
| Host library | `src/Aether.Plugins` |
| Sample plugin | `samples/plugins/Aether.SamplePlugin` |
| Load path | `<editor-output>/plugins/<PluginName>/<PluginName>.dll` |
| DI | `Microsoft.Extensions.DependencyInjection` 10.0.0 |
| Isolation | Collectible `AssemblyLoadContext` per plugin folder/assembly |
| Extension point | `IEditorContribution` — the shell adds a Dock.Avalonia tool pane |

The hypothesis held: discover `IPlugin` types, call `Configure(IServiceCollection)`, then `BuildServiceProvider`. Shared contracts (`Aether.Plugins`, `Microsoft.Extensions.*`) load from the default context so `IPlugin` / `IEditorContribution` identity matches the host. The sample plugin does not reference Avalonia; the host owns the dockable and the view.

## What compiled

- `IPlugin`, `IEditorContribution`, `PluginHost`, collectible `PluginLoadContext`
- Sample `HelloAetherPlugin` registers `hello-aether`
- Editor copies the sample dll into `plugins/Aether.SamplePlugin/` after build
- Dock layout: History / Plugins / Hello Aether tabs; Help > About lists loaded plugins
- `--headless-session` resolves `IEditorContribution` from the `ServiceProvider`

## Unload

ALC is collectible so unload is *possible* after the `ServiceProvider` and all plugin instances are unreachable. This slice **loads at startup only**. `PluginHost.Dispose` disposes the provider and calls `Unload`, but the running editor keeps the host for the process lifetime. No hot-reload.

## What was excluded / remaining gaps

- Rewriting ATF MEF (`Export` / `Import` on Core / Commands / PropertyEditing)
- `IDocumentType` / file-format plugins (the chosen point is `IEditorContribution`)
- Plugin marketplace, versioning policy, signed catalogs
- Unload-while-running / hot-reload-while-debugging
- Full `ICommandService` host (menus, keyboard-shortcut table, context menus). `RunContextMenu` is the remaining abstract UI hook.
- `StandardFileCommands` / `IDocumentClient` / `IDocumentRegistry` / `IFileDialogService` — persistence does not need them yet
- Palette, search, multi-document registry
- WinForms/WPF, SharpDX, Stride viewport
- GUI automation (needs a display). CI restore+build plus the headless session flag is the gate.

## Run

```bash
dotnet build Aether.sln -c Release
dotnet run -c Release --project src/Aether.Editor
dotnet run -c Release --project src/Aether.Editor -- --headless-session
```

## License / attribution

- Apache License 2.0 (`LICENSE`).
- Sony Computer Entertainment America LLC copyright headers are retained on copied sources.
- Modified files are marked.
- Root `NOTICE` is retained and names ATF / LevelEditor / SLED.

## Build

```bash
dotnet build Aether.sln -c Release
```

```bash
dotnet run -c Release --project src/Aether.Atf.DomGen.Cli -- \
  testdata/atf/UsingDom/game.xsd testdata/atf/UsingDom/GameSchema.cs \
  Game.UsingDom UsingDom --check
```

```bash
dotnet run -c Release --project samples/UsingDom
```

CI: `.github/workflows/ci.yml` on `ubuntu-latest` restores and builds `Aether.sln`, runs the DomGen `--check` smokes (UsingDom + CircuitEditor + TimelineEditor + LevelEditor), `dotnet run`s `samples/UsingDom`, then `src/Aether.Editor -- --headless-session` (edit/undo, XML round-trip, sample plugin DI, CircuitEditor graph, TimelineEditor, LevelEditor, C# and Lua scripts against UsingDom, then pause/continue on a breakpoint before Bill Size changes). Windows CI is not required; no Windows-only API remains in the compiled set.

---

# CircuitEditor first slice (schema + adapters + Avalonia graph)

First port of SonyWWS ATF CircuitEditor into Aether as a node-graph tool. Data + a usable Avalonia view. Not a visual-scripting product and not a WinForms port.

## Source

| Item | Value |
|---|---|
| Upstream sample | `Samples/CircuitEditor` (single project; there is no CircuitEditorCore) |
| Upstream graph types | `Framework/Atf.Gui/Controls/Adaptable/Graphs` (+ `Circuit/`) |
| Schema | `Samples/CircuitEditor/schemas/Circuit.xsd` (`http://sony.com/gametech/circuits/1_0`) |
| Sample document | `Samples/CircuitEditor/data/Example.circuit` (9 modules, 8 wires) |
| Destination graph | `src/Aether.Atf.Circuit` |
| Destination sample | `src/Aether.Circuit` |
| Fixtures | `testdata/atf/CircuitEditor/` (`Circuit.xsd`, generated `Schema.cs`, `Example.circuit`) |
| Destination TFM | `net10.0` |

The hypothesis held: CircuitEditor’s value is the schema + DomNode adapters + graph model. The WinForms `CircuitControl` / GDI / D2D renderers are disposable for this slice.

## Cut line

**Ported (data):** `IGraph` / `IGraphNode` / `IGraphEdge` / `IEdgeRoute`, `ICircuitPin` / `ICircuitElement` / `ICircuitElementType`, `Element`, `Wire`, `Pin`, `Circuit`, `ElementType`, `CircuitElementInfo`, `PinTarget`. Sample `Module` / `Connection` / `Circuit` / `Pin`. SchemaLoader registers those adapters plus `HistoryContext`, `SelectionContext`, and `ObservableCustomTypeDescriptorNodeAdapter`. Runtime module types (`buttonType`, `andType`, `lightType`, …) are still created in `ModuleCatalog` — they are **not** in the XSD; ATF defined them in `ModulePlugin`.

**Not ported:** `CircuitControl`, `CircuitRenderer`, `D2dCircuitRenderer`, `CircuitMagnifier`, WinForms palette / `IPaletteClient`, Group / GroupPin / templates / prototypes / layers / expressions, version migrator (`CircuitEditor1to2`), LevelEditor.

`ICircuitElementType.Image` is `object` (same pattern as `IStatusImage`). `Point` / `Size` / `Rectangle` come from inbox `System.Drawing.Primitives`.

## Node-graph view

Looked at maintained Avalonia node-graph controls first:

| Package | Why it was not used for this slice |
|---|---|
| NodifyAvalonia 6.6.0 | Targets Avalonia 11.1, not 12. |
| Nodify.Avalonia 2.0 / NodifyM.Avalonia | Avalonia 12, but they own a parallel node/connector VM. ATF connections are pin-index IDREFs on DomNodes, not Nodify connectors. Wrapping every Module/Wire would be a second graph model. |

This slice draws boxes + lines with a custom `CircuitGraphControl` (`Control.Render`). Click a module to select it; the existing PropertyGrid binds the module’s `ICustomTypeDescriptor`. Adding one And gate and one wire is enough beyond loading Example.circuit.

File Open detects `.circuit` (or a `circuit` XML root) and routes to the circuit session; UsingDom `.xml` stays on the game session. Both documents stay loaded. Undo/Save follow the last-activated document.

## Headless proof

`--headless-session` loads Example.circuit, asserts 9 modules / 8 wires, selects And_1, edits Name through ATF descriptors, undoes, adds one And + wire, Save As / reopen.

```bash
dotnet run -c Release --project src/Aether.Atf.DomGen.Cli -- \
  testdata/atf/CircuitEditor/Circuit.xsd testdata/atf/CircuitEditor/Schema.cs \
  http://sony.com/gametech/circuits/1_0 CircuitEditorSample --check
```

---

# TimelineEditor first slice (schema + adapters + Avalonia timeline)

First port of SonyWWS ATF TimelineEditor into Aether. Data + a usable Avalonia view. Not a sequencer product and not a WinForms port.

## Source

| Item | Value |
|---|---|
| Upstream sample | `Samples/TimelineEditor` (single project; there is no TimelineEditorCore / TimelineControls sample) |
| Upstream interfaces | `Framework/Atf.Gui/Controls/Timelines` |
| Schema | `Samples/TimelineEditor/schemas/timeline.xsd` (namespace `timeline`) |
| Sample document | `Samples/TimelineEditor/data/100.timeline` (3 groups, 10 tracks, 60 intervals, 4 markers) |
| Destination interfaces | `src/Aether.Atf.Timeline` |
| Destination sample | `src/Aether.Timeline` |
| Fixtures | `testdata/atf/TimelineEditor/` (`timeline.xsd`, generated `Schema.cs`, `100.timeline`) |
| Destination TFM | `net10.0` |

The hypothesis held: TimelineEditor’s value is the schema + interval/track/group adapters. The WinForms `TimelineControl` / GDI / D2D renderers are disposable for this slice.

## Cut line

**Ported (data):** `ITimeline` / `IGroup` / `ITrack` / `IInterval` / `IEvent` / `IKey` / `IMarker` / `ITimelineObject`. Sample `Timeline` / `Group` / `Track` / `Interval` / `Key` / `Marker` / `BaseEvent`. SchemaLoader registers those adapters plus `HistoryContext`, `SelectionContext`, and `ObservableCustomTypeDescriptorNodeAdapter`.

**Not ported:** `TimelineControl`, `TimelineRenderer`, `D2dTimelineControl` / `D2dTimelineRenderer`, palette / `NodeTypePaletteItem`, `TimelineValidator`, `TimelineContext`, hierarchical `ITimelineReference` / referenced documents, LevelEditor.

`IEvent.Color` uses inbox `System.Drawing.Color` (`System.Drawing.Primitives`). No `System.Drawing.Common`.

## Timeline view

Looked at maintained Avalonia timeline/gantt controls first:

| Package | Why it was not used for this slice |
|---|---|
| Avalonia.Controls.Charts `GanttChart` / `EventTimelineChart` | Avalonia Pro (paid). Binds a DateTime `ItemsSource` VM, not ATF float `start`/`length` on DomNodes. |

This slice draws rows + rectangles on a time scale with a custom `TimelineControl` (`Control.Render`). Click an interval to select it; the existing PropertyGrid binds the interval’s `ICustomTypeDescriptor`. Adding one interval is enough beyond loading `100.timeline`.

File Open detects `.timeline` (or a `timeline` XML root) and routes to the timeline session. UsingDom and Circuit documents stay loaded. Undo/Save follow the last-activated document.

## Headless proof

`--headless-session` loads `100.timeline`, asserts 10 tracks / 60 intervals, selects `Interval`, edits Name through ATF descriptors, undoes, adds one interval, Save As / reopen.

```bash
dotnet run -c Release --project src/Aether.Atf.DomGen.Cli -- \
  testdata/atf/TimelineEditor/timeline.xsd testdata/atf/TimelineEditor/Schema.cs \
  timeline TimelineEditorSample --check
```

---

# LevelEditor first slice (schema + GameObject data model)

First port of SonyWWS LevelEditor into Aether as a **data model**: GameObjects, transforms, hierarchy, resource-ref URIs. Not a 3D editor and not a DX11/WinForms port.

## Source

| Item | Value |
|---|---|
| Upstream | https://github.com/SonyWWS/LevelEditor (fork: https://github.com/djburkhart/LevelEditor) |
| Portable core | `LevelEditorCore` (Interfaces, Utils, GameEngineProxy types) |
| Sample adapters | `LevelEditor/DomNodeAdapters` |
| Schema | `LevelEditor/schemas/level_editor.xsd` + included `gap.xsd` (namespace `gap`) |
| Sample document | `SampleLevels/LightTest.lvl` (10 game objects, 7 top-level, one group) |
| Destination core | `src/Aether.LevelEditor.Core` |
| Destination sample | `src/Aether.Level` |
| Fixtures | `testdata/atf/LevelEditor/` (`level_editor.xsd`, `gap.xsd`, generated `Schema.cs`, `LightTest.lvl`) |
| Destination TFM | `net10.0` |

The hypothesis held: LevelEditorCore + DomNodeAdapters + schemas are portable. **GameEngineProxy is the cut line.** A no-op `NullGameEngine` implements `IGameEngineProxy` so adapters can sit behind it. Nothing pretends a renderer exists.

## Cut line

**Ported (data):** `IGame` / `IGameObject` / `IGameObjectFolder` / `IGameObjectGroup` / `ITransformable` / `INameable` / `IVisible` / `ILockable` / `IHierarchical` / `IGrid` / `ISchemaLoader` / `IGameEngineProxy`. `DomNodeUtil`, `TransformUtils.CalcTransform`, `EngineInfo` / `ResourceInfo`, `NullGameEngine`. Sample `Game` / `GameObject` / `GameObjectFolder` / `GameObjectGroup` / `Grid` / `TransformUpdater` / `ResourceReference` (URI only). SchemaLoader registers those adapters plus `HistoryContext`, `SelectionContext`, `UniqueIdValidator`, and `ObservableCustomTypeDescriptorNodeAdapter`.

**Not ported:** `LevelEditorNativeRendering`, `LvEdRenderingEngine`, `NativeInterop`, `NativeDesignControl`, WinForms `GameEditor` / ProjectLister / DesignView, camera controllers, manipulators, terrain gob adapters, prefab / curve / locator / layer / bookmark adapters, `CustomDomXmlReader` resource remapping, `IListable` (`ItemInfo` / `ImageList`), `IGrid.Project` (needs `Camera`), `CalcSnapFromOffset` / `RotateToVector` (AABB / `AxisSystemType`).

Vendored ATF under LevelEditor `ATF/` was **not** copied. Aether.Atf.Core / Commands / PropertyEditing already on main are the ATF layer.

## Hierarchy view

No 3D viewport. The Avalonia shell adds a Level pane: a TreeView of folders / groups / game objects. Select a GameObject; the existing PropertyGrid binds its `ICustomTypeDescriptor`. Adding one GameObject is enough beyond loading `LightTest.lvl`.

File Open detects `.lvl` (or a `game` XML root in namespace `gap`) and routes to the level session. UsingDom, Circuit, and Timeline documents stay loaded. Undo/Save follow the last-activated document.

## Headless proof

`--headless-session` loads `LightTest.lvl`, asserts 10 game objects / 7 top-level, checks PointLight translate X, selects PointLight, edits Name through ATF descriptors, undoes, adds one GameObject, Save As / reopen.

```bash
dotnet run -c Release --project src/Aether.Atf.DomGen.Cli -- \
  testdata/atf/LevelEditor/level_editor.xsd testdata/atf/LevelEditor/Schema.cs \
  gap LevelEditor --check
```

---

# SLED first slice (in-editor C# + Lua host)

First slice of modernizing SonyWWS SLED into Aether. **In-process** C# and Lua script hosting against a loaded Aether document. Not the WinForms SLED IDE. Not a DAP product. Not a port of C++ LibSledDebugger.

## Source

| Item | Value |
|---|---|
| Upstream | https://github.com/SonyWWS/SLED (fork: https://github.com/djburkhart/SLED) |
| High-reuse idea | Language-plugin split (`tool/SledShared/Plugin/ISledLanguagePlugin.cs`: name + extensions + id) |
| Also considered | SCMP / target comms, SledShared DOM/plugin/services |
| Disposable | `tool/SLED` WinForms IDE, `tool/SledSyntaxEditor`, `tool/SledCrashReporter` |
| Not used | `src/sleddebugger`, `src/sledluaplugin`, `src/sledcore` (C++ LibSledDebugger) |
| Bundled Lua (not used) | `wws_lua/lua-5.1.4`, `wws_lua/lua-5.2.3` |
| Destination host | `src/Aether.Scripting` |
| Destination UI | Script dock document in `src/Aether.Editor` |
| Fixtures | `testdata/scripts/resize-bill.csx`, `testdata/scripts/resize-bill.lua` |
| Destination TFM | `net10.0` |

SLED expected a sibling ATF tree. That ATF was **not** vendored. Aether.Atf.Core / Commands / PropertyEditing already on main are the ATF layer.

No SLED source files were copied. New host code is Resolvora/Aether. Sony headers stay on any future SLED-derived files; this slice has none.

## Hypothesis

MoonSharp + Roslyn Scripting API + a small `IScriptHost` is the SLED idea without the C++ target. **Verified.** The host is in-process; scripts bind a `ScriptDocument` over the loaded UsingDom `DomNode` (+ `HistoryContext`). LibSledDebugger / SCMP is not required for Run.

## MoonSharp vs NLua

| Engine | Why |
|---|---|
| **MoonSharp 2.0.0** (chosen) | Pure C#, restores on net10.0, no native Lua / C++ build. `CoreModules.Preset_HardSandbox` (no `os` / `io`). |
| NLua | Needs a native lua54 binary. Rejected for this slice so CI stays `dotnet build` only. |

SLED bundled Lua 5.1.4 / 5.2.3 for an in-game C++ target. Aether hosts Lua in-process instead.

C# is **not** a security sandbox. Roslyn can fully-qualify any referenced API. The sample globals are only `document` and `log`; the checked-in scripts do not import process/file APIs. Lua is HardSandbox.

## Cut line

**Built:** `IScriptLanguage` / `IScriptHost` / `IDebugger`. `CSharpScriptLanguage` (Roslyn `CSharpScript.RunAsync` + statement-line rewrite). `LuaScriptLanguage` (MoonSharp + host `IDebugger` on `GetAction`). `ScriptDocument` safe API: `ListObjects`, `GetAttribute`, `SetAttribute`, `Log`, `SnapshotWatches`. Script dock pane (AvaloniaEdit) with gutter breakpoints, Run, Continue, and a watch box. File Open of `.csx` / `.lua` loads that pane.

**Not built:** WinForms SLED IDE, syntax-editor control, crash reporter, C++ LibSledDebugger, SCMP target protocol, IronPython (ATF's leftover scripting note), a DAP listen-and-attach server, Visual Studio integration.

## Debugger

`IDebugger` records breakpoints and **honors them on Run**. Language hosts call `IStatementBreak.OnStatement` at a statement boundary (before the statement executes). `ScriptDebugger` blocks that run thread on a `ManualResetEventSlim`; `Continue` releases it. The UI starts Run on a worker thread so the dispatcher is not stuck in the wait. Headless does the same: `BeginRun` + `WaitUntilPaused` + inspect + `Continue`.

| Language | Hook |
|---|---|
| C# | Roslyn syntax rewrite inserts `__line(n);` before each statement (`n` is the original 1-based line). |
| Lua | MoonSharp `AttachDebugger`. `IsPauseRequested` is true when debugging; `GetAction` maps `SourceRef.FromLine` to `OnStatement`. |

Watches while paused are `ScriptDocument.SnapshotWatches()` (named objects + attributes) plus language and line. Locals from the Roslyn/MoonSharp frames are not dumped in this slice.

A DAP server is still deferred. Pause/continue does not need one.

The Script pane gutter (left of line numbers) toggles a breakpoint. Headless uses `IDebugger.SetBreakpoint(path, line)` with no GUI.

## Editor surface

Picked a **Script dock document** (tab alongside Objects / Circuit / Timeline / Level), not a fifth `EditorDocumentKind`.

- File Open of `.csx` / `.lua` loads source into the Script pane and does **not** change `ActiveKind`.
- File Save still applies to the last-activated UsingDom / circuit / timeline / level document.
- Run executes against the loaded UsingDom `Game` + its `HistoryContext` (the same walk works for Level GameObjects if a later slice binds that root).

The pane uses AvaloniaEdit (`Avalonia.AvaloniaEdit` 12.0.0). A TextBox would have been enough; AvaloniaEdit fit.

## Safe API

Scripts see `document` (and C# `log(string)`):

```
string[] ListObjects()
object GetAttribute(string objectName, string attributeName)
void SetAttribute(string objectName, string attributeName, object value)
void Log(string message)
```

`SetAttribute` converts the value to the attribute's current CLR type (Lua numbers arrive as `double`) and wraps the write in `HistoryContext.DoTransaction` when a history context is bound. Attribute names are schema names (`size`, not the property-grid display name `Size`). The API does not expose process, file, or network operations.

## Headless proof

`--headless-session` still proves UsingDom / plugins / Circuit / Timeline / Level, then:

1. `session.New()` (Ogre Adventure II, Bill Size 12)
2. Run `testdata/scripts/resize-bill.csx`, assert Bill Size == 14
3. `session.New()` again
4. Run `testdata/scripts/resize-bill.lua`, assert Bill Size == 14
5. Set a breakpoint on line 2 of `resize-bill.csx`, `BeginRun`, assert paused and Bill Size still 12, Continue, assert Size 14
6. Same pause/continue proof for `resize-bill.lua`

```bash
dotnet run -c Release --project src/Aether.Editor -- --headless-session
```
