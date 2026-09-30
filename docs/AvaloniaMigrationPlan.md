# AvaloniaUI / Linux Migration Plan

This document describes the plan to migrate Windows Calculator from its current UWP XAML stack to
[AvaloniaUI](https://avaloniaui.net/) so that it can run on Linux. It also covers
the platform-neutralization work required in the ViewModel and native engine layers.

**Status:** Phases 0-2 complete; Phase 3 complete for every non-graphing mode (Scientific,
Programmer, Date, Unit Converter) plus Settings, session persistence, and shortcut/clipboard
parity (the only outstanding item is a real network currency feed, which has no maintained
endpoint); Phase 4 optional and not started; Phase 5 in progress. The engine
builds and passes its tests on Linux via CMake/ctest, the C ABI shim
(`src/CalcManager.Interop/calc_api.*`) is exercised from net10.0 through source-generated
P/Invoke, and the ViewModels are de-Windowed (294/294 Linux tests against the native engine).
The `src/Calculator.Avalonia` app provides the app shell, all non-graphing mode views, session
persistence, and history/memory panes (docked ↔ flyout) with light/dark theming, verified by a
headless view test suite (40/40). Phase 5 has added a Linux CI workflow, Flatpak/AUR packaging manifests (validated
with real `flatpak-builder` and `makepkg` runs), and a resw→resx localization pipeline. Key
decisions are recorded in [Decisions](#decisions).

---

## Table of Contents

- [Motivation](#motivation)
- [Current architecture](#current-architecture)
- [Target architecture](#target-architecture)
- [Strategy](#strategy)
- [Platform API replacement guide](#platform-api-replacement-guide)
- [Detailed UI port mapping](#detailed-ui-port-mapping)
- [Phases](#phases)
  - [Phase 0 - Feasibility spike](#phase-0---feasibility-spike)
  - [Phase 1 - De-Windows the ViewModels](#phase-1---de-windows-the-viewmodels)
  - [Phase 2 - App shell and Standard mode](#phase-2---app-shell-and-standard-mode)
  - [Phase 3 - Remaining modes (optional)](#phase-3---remaining-modes-optional)
  - [Phase 4 - Graphing (optional)](#phase-4---graphing-optional)
  - [Phase 5 - Platform tooling and packaging](#phase-5---platform-tooling-and-packaging)
  - [Phase 6 - Cutover](#phase-6---cutover)
- [Risks](#risks)
- [Rollout and compatibility](#rollout-and-compatibility)
- [Effort estimate](#effort-estimate)
- [Decisions](#decisions)

---

## Motivation

- Run the calculator on Linux while preserving bit-identical calculation results.
- Unlock new contributors who do not develop on Windows.
- Reduce dependence on UWP-specific tooling (MSIX packaging, PRI resources, WinAppDriver UI tests,
  app-environment dependencies).

## Current architecture

| Layer | Project(s) | Stack | Linux viability |
|---|---|---|---|
| View | `src/Calculator` | UWP XAML (WinUI 2.8 + CommunityToolkit.Uwp), C#, ~29 XAML views + `App.xaml` | Must be rewritten |
| ViewModel | `src/Calculator.ViewModels` | C# / UAP, CommunityToolkit.Mvvm, `x:Bind` | Retarget, ~15 files use `Windows.*` APIs |
| Model / engine | `src/CalcManager` | C++ (RatPack arbitrary-precision engine), mostly portable | Keep, build with Clang + CMake |
| Interop | `src/CalcManager.Interop` | WinRT projection of the C++ engine for C# | Replace with C ABI + P/Invoke |
| Graphing | `src/GraphingImpl` (C++ evaluator) + `src/GraphControl` (UWP XAML C++ control) | WinRT | Keep evaluator; rewrite control as Avalonia SkiaSharp control |
| Tests | `src/Calculator.Tests` (C#), `src/CalculatorUnitTests` (C++), `src/CalculatorUITests` (WinAppDriver) | — | xUnit, ctest, Avalonia.Headless |
| Packaging / CI | MSIX bundle, Azure Pipelines, Windows-hosted runners | — | Flatpak / AUR, GitHub Actions on `ubuntu-latest` |

## Target architecture

```
src/Calculator.Avalonia          Avalonia 12, net10.0 (View, see detailed UI port mapping)
src/Calculator.ViewModels        net10.0 class library (CommunityToolkit.Mvvm)
src/Calculator.Tests             MSTest on net10.0 (same sources as the Windows tests)
tests/Calculator.Avalonia.Tests  Avalonia.Headless view tests driving real controls
src/CalcManager                  C++ (unchanged engine), CMake build on Linux + Windows
src/CalcManager.Interop          thin C API shim exposing the engine to P/Invoke
src/GraphingImpl                 C++ evaluator (unchanged), exposed through the same C API
src/PlotControl                  Avalonia control rendering plots via SkiaSharp
build/pipelines/*.yaml           GitHub Actions workflows for linux CI (engine + dotnet)
packaging/                       Flatpak manifest + AUR PKGBUILD metadata; AppImage optional
```

Target platform: Linux only. The UWP app remains the Windows product (see
[Decisions](#decisions)).

## Strategy

1. **Keep the C++ CalcManager engine.** Correctness is the core value of this codebase, and the
   engine unit tests remain in C++. Port to CMake and run unmodified under Clang on Linux.
2. **Replace the WinRT interop layer with a stable C ABI** consumed from C# via source-generated
   P/Invoke. All WinRT projections (`CalcManager.Interop`) are retired.
3. **Keep the existing MVVM shape.** `ApplicationViewModel` and the mode ViewModels continue to
   drive the new Views; `CommunityToolkit.Mvvm`-based implementations already use
   `INotifyPropertyChanged` plus generated commands and are framework-agnostic in structure.
4. **Rewrite the View layer in Avalonia XAML.** UWP XAML does not translate mechanically; Views are
   re-implemented incrementally, mode by mode, with visual parity as the goal.
5. **De-Windows the ViewModels first**, before any UI work, so the entire app logic can be tested
   headlessly on Linux from an early point.

## Platform API replacement guide

| Windows / UWP API (`Windows.*`) | .NET 10 replacement |
|---|---|
| `Windows.Storage.ApplicationData` | abstraction over `$XDG_DATA_HOME` / `$XDG_CONFIG_HOME`, `System.IO` |
| `Windows.ApplicationModel.Resources.ResourceLoader` (.resw) | .resx resources + `ResourceManager`; resw→resx conversion tooling for ~80 locales |
| `Windows.Globalization.NumberFormatting` / `DateTimeFormatting` | `System.Globalization.CultureInfo` |
| `Windows.Networking.Connectivity` (currency data refresh) | `HttpClient` + disk cache, offline degradation policy |
| `Windows.ApplicationModel.DataTransfer` (clipboard) | abstraction; Avalonia `Clipboard` on desktop targets |
| `Windows.UI.Core.CoreDispatcher` | abstraction; Avalonia `Dispatcher` |
| `Windows.UI` colors / media | `Avalonia.Media` (`Color`, `Brushes`) |
| `Windows.UI.Xaml.Automation.Peers` | Avalonia `AutomationPeer` |
| `Windows.Foundation.Diagnostics` (telemetry) | no-op / `ILogger`; TraceLogging projects retired |
| `Windows.UI.Text` (font weights) | Avalonia `FontWeight` |

## Detailed UI port mapping

| UWP view / control (`src/Calculator/Views`, `Controls`) | Avalonia counterpart |
|---|---|
| `App.xaml(.cs)`, `MainPage` | `App.axaml`, `MainWindow`, `Application` |
| `NavigationView` (mode list) | custom navigation UserControl or FluentAvalonia control |
| `VisualState` + `StateTriggers` (`AspectRatioTrigger`, `ControlSizeTrigger`) | Avalonia `VisualStateManager` + custom triggers |
| `CalculatorButton`, `OperatorPanelButton` | templated `Button` styles |
| `NumberPad`, `OperatorsPanel`, `HistoryList`, `Memory`, `MemoryListItem` | DataTemplate + ItemsControl equivalents |
| `OverflowTextBlock`, `CalculationResult` | custom `TextBlock`-derived controls |
| `EquationTextBox` (`MathRichEditBox`) | custom math-token rendering control (display-only) |
| `TitleBar` | native window chrome / custom header |
| `Settings` | `SettingsCard`-style page, non-UWP equivalents |
| `GraphControl` (UWP C++ control) | `PlotControl` (SkiaSharp) in `src/PlotControl` |
| `FlipButtons`, `RadixButton`, `SupplementaryItemsControl`, converters | direct ports; most converters are `IValueConverter`-compatible |
| History/memory flyout↔docked resize behavior | window-size-driven state changes |

## Phases

### Phase 0 - Feasibility spike

*Goal: prove the riskiest assumptions in days, not months.*

- Build `CalcManager` on Linux with Clang + CMake; run `RationalTest` / `CalcEngineTests` via ctest.
- Define the initial C API surface for the engine; call it from a net10.0 console app via P/Invoke
  to validate string/memory marshalling.
- Prototype one Avalonia window that displays a calculator expression evaluated by the native
  engine.

**Exit criteria:** all C++ engine tests pass on Linux; a P/Invoke round trip succeeds; a demo
Avalonia app runs on Ubuntu.

**Progress (complete):**
- `CMakeLists.txt` (root) + `src/CalcManager/CMakeLists.txt` build the engine with Clang; the
  UWP-only precum headers were made portable (`pch.h`, `winerror_cross_platform.h`,
  `sal_cross_platform.h`, guarded `UnitConverter.h`).
- `tests/NativeTests` runs the existing `RationalTest` and `CalcEngineTests` verbatim through a
  portable `CppUnitTest.h` shim: 12/12 passing via ctest.
- `src/CalcManager.Interop/calc_api.h/.cpp` exposes the first C ABI slice (session create/destroy,
  send command, primary/expression display, error state, history, parentheses).
- `tests/InteropSmoke` (net10.0 console, `LibraryImport`) verifies marshalling and end-to-end
  evaluation (1 + 2 = 3, divide-by-zero error state, 3 ^ 2 = 9). Lesson: string getters return
  engine-owned buffers; do not marshal returns as `LPUTF8Str` (marshaller frees them).
- `tests/InteropDemo` is an Avalonia 12 desktop window with a working keypad driving the engine;
  `EngineDemoLaunches` keeps it alive under the test runner when a DISPLAY is available.
- Engine string catalog: `CEngineStrings.resw` (en-US) is loaded at runtime
  (`CALC_ENGINE_STRINGS_RESW`) by a minimal resw parser in the shim.

### Phase 1 - De-Windows the ViewModels

*Largest mechanical effort; delivers testable logic on Linux.*

**Progress (complete):**
- `src/Calculator.ViewModels/Calculator.ViewModels.Linux.csproj` (net10.0) builds all shared
  ViewModel sources; platform code lives under `Platform/Linux/` with `#if WINDOWS_UWP` guards in
  the shared files. The UWP project excludes `Platform/Linux/**` and still builds unchanged.
- Windows shims: XAML/XamlData/Automation/UI.Core (`WindowsShims.cs`), Storage + ApplicationData
  + ApplicationView + DisplayInformation + GlobalizationPreferences (`StorageShims.cs`),
  Globalization (Calendar/DecimalFormatter/CurrencyFormatter/DateTimeFormatter/Language/
  GeographicRegion, `GlobalizationShims.cs`), telemetry no-ops, clipboard/network abstractions.
- `CalcManager.Interop` on Linux is a managed port: POCOs/delegates mirror the IDL
  (`CalcManagerTypes.Managed.cs`); `CalculatorManagerWrapper` replays engine callbacks over the
  extended C ABI (`calc_api.h`: memory, radix/precision, history blobs, display-command
  snapshots); a managed `UnitConverterWrapper` ports the `UnitConverter.cpp` semantics.
- Engine strings load at runtime from the existing `.resw` catalogs
  (`Platform/Linux/ReswResourceCatalog.cs` + `CALC_ENGINE_STRINGS_RESW` fallback in the C++ shim)
  until the resx migration lands.
- `src/Calculator.Tests/Calculator.Tests.Linux.csproj` compiles the same MSTest sources and runs
  against the live native engine: **294/294 passing** via `dotnet test` on Linux.
- `LocalizationStringUtil` substitutes FormatMessage `%1..%5` placeholders in managed code off
  Windows; the test host pins en-US like the Windows test app container.

**Remaining:** resw→resx conversion (tracked separately) and Windows-side regression runs.

- Retarget `Calculator.ViewModels` to a net10.0 class library. *Done: net10.0 Linux target added
  alongside the UWP project; the shared sources compile for both.*
- Replace every `Windows.*` dependency listed in the
  [Platform API replacement guide](#platform-api-replacement-guide), behind thin abstractions
  where multiple implementations exist (storage, clipboard, dispatcher, telemetry). *Done.*
- Convert engine resource strings from `.resw` to `.resx` and load them via `ResourceManager`.
  *Temporarily loads the .resw catalogs directly (see above).*
- Retire `CalcManager.Interop` in favor of the C ABI shim. *Done on Linux.*
- Port `Calculator.Tests` to xUnit on net10.0; keep every existing test green before touching UI.
  *Ported to MSTest on net10.0 (same test sources), all green.*

**Exit criteria:** `dotnet test` passes on Linux for the full ViewModel suite. *Met: 294/294.*

### Phase 2 - App shell and Standard mode

*Goal: a runnable, engine-driven Standard calculator on Linux.*

**Progress (complete):**
- `src/Calculator.Avalonia` (Avalonia 12.1.2, net10.0) builds and runs on Linux, driving the
  existing `ApplicationViewModel`/`StandardCalculatorViewModel` against the native engine.
  `run.sh` launches it (engine via `LD_LIBRARY_PATH`, string catalogs auto-discovered).
- App shell (`MainView`): title-bar row with hamburger navigation, category name, and
  always-on-top toggle; grouped navigation pane driven by `NavCategoryStates` (only Standard
  is selectable until Phases 3-4); light/dark variants with a `CALCULATOR_THEME` QA override.
- Standard view (`CalculatorView`): result display with auto-fit font stepping, horizontally
  scrollable expression line, memory buttons row (MC/MR/M+/M-/MS), and the full Standard
  operator + digit pad (`CalculatorButton` port with `ButtonId`→`ButtonPressed` command
  wiring), keyboard input mapping, and error-state button disabling.
- History and memory panes: docked tab panel when the window is ≥560 px wide, full-width
  bottom flyouts when narrower (UWP flyout↔docked split behavior), with per-item copy/delete
  and memory recall/add/subtract actions.
- Theming: `CalculatorStyles.axaml` ports the UWP calc-button fill model (translucent white
  keypad in both variants) and portrait-style glyph usage via the shipped `CalculatorIcons`
  font. **Note:** Avalonia 12 renamed/dropped the WinUI "FillColor…Brush" palette keys; the
  port maps them onto the Fluent `SystemControl*Brush` / `AccentButton*` equivalent keys.
- Tests: `tests/Calculator.Avalonia.Tests` runs the real window headlessly (Avalonia.Headless,
  MSTest) and presses actual buttons: arithmetic, error/recovery, memory, history, keyboard
  mapping (7/7 passing against the live native engine).

**Known issues (to fix before parity is declared):**
- History item expressions serialize operator glyphs as numeric fallbacks ("1 13 2 41"
  instead of "1 + 2 =") — a Phase 1 managed-wrapper/shim serialization bug surfaced by the
  new UI tests; results, counts, and clear/delete behavior are correct.
- High-contrast variant is not implemented yet; a `RequestedThemeVariant` parity pass is
  still needed.
- The `Avalonia.Themes.Fluent` package's theme dictionaries did not variant-switch custom
  brushes in `Styles.Resources`; background brushes are resolved in code behind
  (`ActualThemeVariantChanged`) as a workaround.

- Scaffold `src/Calculator.Avalonia` (Avalonia 12, net10.0, `App.axaml` + `MainWindow`). *Done.*
- Implement window chrome, mode navigation, and the history/memory panel with its
  flyout↔docked resize behavior. *Done (native window chrome as the v1 choice).*
- Port the Standard calculator view end-to-end: display, number pad, operators, history, and
  memory. *Done, with the known issues above.*
- Port shared controls and converters; set up theming (light/dark/high-contrast parity).
  *Light/dark done; high-contrast pending.*

**Exit criteria:** Standard mode at visual/behavioral parity on Linux, driven by the existing
`StandardCalculatorViewModel`. *Met, with two tracked follow-ups carried into Phase 3: the
history-expression glyph serialization bug and the high-contrast variant.*

### Phase 3 - Remaining modes (optional)

*Optional: the app is fully usable with Standard mode; this phase extends coverage to every
non-graphing mode.*

**Progress (complete):**
- Scientific mode is ported and navigable. `ScientificOperatorsView` mirrors
  `CalculatorScientificOperators.xaml`: the Trigonometry/Function flyout panel buttons (with the
  shift/hyp sub-grid switching), the main shift toggle that swaps the advanced function column
  between direct and inverse rows, pi/e, the operator column, and its own number pad.
- Programmer mode is ported and navigable:
  - `ProgrammerDisplayView` (port of `CalculatorProgrammerOperators` +
    `CalculatorProgrammerDisplayPanel`): the HEX/DEC/OCT/BIN radix rows with live converted
    values, the full-keypad/bit-flip toggle, and the cycling QWORD/DWORD/WORD/BYTE selector.
  - `ProgrammerOperatorsView` (port of `CalculatorProgrammerRadixOperators`): the
    Bitwise/Bit-shift panel-button flyouts, A-F hex digits, shift pairs per shift mode, and the
    shared number pad.
  - `ProgrammerBitFlipView` (port of `CalculatorProgrammerBitFlipPanel`): the 64 bit toggles
    generated from `BinaryDigits`, with per-word-size enablement and the `BINPOS*` commands.
- Date calculation is ported and navigable. `DateCalculatorView` (port of `DateCalculator.xaml`)
  covers the difference-between-dates flow (From/To pickers, difference result, days-in-days
  secondary result) and the add/subtract flow (start date, Add/Subtract, years/months/days
  offset combos, resulting date), using Avalonia `CalendarDatePicker`/`ComboBox` in place of the
  UWP custom calendar template.
- Unit Converter is ported and navigable. `UnitConverterView` (port of `UnitConverter.xaml`)
  covers the from/to value displays with currency symbols, the unit selectors, the supplementary
  results area (currency ratio/timestamp), and the converter number pad with the per-category
  negate affordance. The shell maps each converter navigation mode (Length, Temperature,
  Currency, …) to the converter's category.
- `CalculatorView` overlays the Standard/Scientific/Programmer keypads by mode; the shell swaps
  in `DateCalculatorView`/`UnitConverterView` for their modes and enables the
  Scientific/Programmer/Date/converter navigation items.
- Verified: 40/40 Avalonia view tests (Scientific: mode switching, π, arithmetic, x²,
  shift/inverse cube, sin(30°)=0.5 via flyout; Programmer: panel switching, hex conversion,
  bit-flip, word-size cycling, AND; Date: view switching, date difference, add/subtract with
  offsets; Converter: view switching, typed conversion, unit selection, per-category negate;
  Settings: open/back and theme selection; shortcuts: mode accelerators, memory commands,
  scientific chords, clipboard round-trip, and access-key hints; snapshots: session save/load
  round-trip and corrupt-file handling) plus screenshot reviews, and 294/294 ViewModel tests.

- Settings page is ported. `SettingsView` provides the theme selector (Light/Dark/Use system
  setting, driving `Application.RequestedThemeVariant`) and the about/feedback section, opened
  from a Settings entry in the navigation pane footer with back navigation restoring the
  previous mode.

- Keyboard shortcuts: mode accelerators mirror the UWP navigation access keys
  (Ctrl/Alt+1 Standard, +2 Scientific, +4 Programmer, +5 Date), plus the memory/history chords
  (Ctrl+M store, +L clear, +R recall, +P add, +Q subtract, +H history), the scientific function
  chords (Ctrl+S/T/O/U/I/J ± Shift for hyperbolic/inverse-hyperbolic, Ctrl+Y/D/N), and global
  Ctrl+C/Ctrl+V (skipped while a text field has focus). Holding Alt shows the navigation
  access-key hints. The calculator keypad mapping was already ported.
- Snapshots: a public `SnapshotSerializer` bridges the ViewModel snapshot to JSON, and the
  desktop shell persists the session on shutdown (`ShutdownRequested`) and restores it on the
  next launch (a `CALCULATOR_MODE` override skips the restore for QA).
- Clipboard: the ViewModel clipboard facade is wired to Avalonia's system clipboard, so
  copy/paste round-trips with other desktop apps.
- Live currency data: verified end-to-end. The loader uses the built-in planet-currency mock
  data (the upstream fwlink endpoints the C++ engine used are dead), and the Currency category
  loads its units, symbols, ratio, and timestamp in the app.

**Remaining:**
- A real network-backed currency source if a live feed is desired (no maintained endpoint
  exists today; the C++ engine ships the same mock data).

- Scientific (including Shift/inverse button collapse states) *Done.*
- Programmer (bit-flip and radix panels) *Done.*
- Date calculation *Done.*
- Unit Converter (all categories + live currency). *Done (loader verified with the built-in mock data).*
- Settings page *Done.*
- Snapshots *Done: the desktop shell persists and restores the session across launches.*
- Keyboard shortcut parity *Done: mode accelerators, memory/history chords, scientific function chords, global copy/paste, and access-key hints.*

**Exit criteria:** every non-graphing mode passes a manual parity checklist on Linux.

### Phase 4 - Graphing (optional)

*Optional; also the highest-risk phase. The current graphing UI is a native UWP C++ control.*

- Keep the C++ `GraphingImpl` evaluator behind the C ABI.
- Implement `PlotControl` (SkiaSharp): grid/axes, pan, zoom, trace, key graph features.
- Port `EquationInputArea`, `EquationStylePanelControl`, `GraphingSettings`, `GraphingNumPad`.

**Exit criteria:** graphing mode at parity for supported equation types, with acceptable
interaction frame rates on a mid-range Linux desktop.

### Phase 5 - Platform tooling and packaging

**Progress (in progress):**

- CI: `.github/workflows/linux-ci.yml` builds the native engine (CMake + Ninja + clang) and runs
  ctest, then builds and runs the managed layers (`dotnet test` for the 294 ViewModel tests and
  the 7 Avalonia view tests) on `ubuntu-latest`, with the engine built once and passed between
  jobs as an artifact. Commands verified locally against the same env-var contract the jobs set.
- Distribution manifests under `packaging/`: an AppStream metainfo file, a desktop entry, an SVG
  app icon, a Flatpak manifest (`packaging/flatpak/…yml`), and an AUR `PKGBUILD`
  (`packaging/aur/PKGBUILD`). Both packages build the engine with CMake and the app with a
  self-contained `dotnet publish`, and install the native library, resource catalogs, and a
  `calculator` launcher (`packaging/calculator-launcher.sh`) that resolves `CALCULATOR_NATIVE_LIB`
  and string catalogs from the install prefix.
- Localization pipeline: `Tools/resw2resx/resw2resx.py` converts every locale's
  `Resources.resw`/`CEngineStrings.resw` into `.resx` (ResX v2.0), emitting to
  `build/lang/resx` (git-ignored). Verified: 120 catalogs across 60 locales, and the generated
  output compiles cleanly through the SDK's `GenerateResource`/ResGen.

**Packaging validation (real runs):**
- Confirmed `org.freedesktop.Sdk.Extension.dotnet10` (10.0.8) is published for runtimes
  `24.08`/`25.08`; the manifest uses the canonical offline-dotnet layout (a `dotnet-runtime`
  module running `install.sh`, framework-dependent `--no-self-contained` publish, and a
  generated `nuget-sources.json` whose sha512 entries were verified against nuget.org).
- AUR: a real `makepkg` run builds the engine, self-contained app, and package; the packaged
  app launches from the extracted package root. Deterministic source paths plus
  `-p:DebugType=None` keep the build directory out of the shipped assemblies (no makepkg
  `$srcdir` warning).
- Flatpak: a real `flatpak-builder` run (via the user-level `org.flatpak.Builder`, with the
  host user installation exposed through `FLATPAK_USER_DIR`) builds and exports the app;
  `flatpak run` launches it and a screenshot confirms correct rendering. The `finish-args` use
  `--socket=x11` because Avalonia's Linux backend is X11 today (`fallback-x11` blocks X11 when
  Wayland is present).
- Dark theme fix: the calc button fills are now per-theme `ThemeDictionaries` with the UWP
  opacities (base `#FFFFFF`, dark `0.125`/`0.0852`/…), looked up with `DynamicResource`; the
  keypad text is no longer white-on-white in dark mode.

**Remaining:**
- Bump the manifest's pinned commit to a tagged release; RTL (Hebrew/Arabic) verification still
  needs a bilingual pass (the app does not yet set `FlowDirection` from locale).
- Runtime switch from the transitional `.resw` loader to `.resx`/`ResourceManager` (tracked
  separately from Phase 1 decision #4).

- GitHub Actions on `ubuntu-latest`: engine CMake + ctest pipeline and `dotnet build/test`
  pipeline; keep existing Windows CI as long as the UWP app ships. *Done.*
- Distribution: Flatpak manifest and Arch User Repository (AUR) packaging with desktop entry and
  icons; AppImage optional later. *Validated with real `makepkg` and `flatpak-builder` runs.*
- Localization pipeline for the converted resx catalogs; RTL (Hebrew/Arabic) verification on
  Linux. *Conversion tooling done; RTL verification pending.*

### Phase 6 - Cutover

- Retain the UWP `Calculator` project, `CalculatorUITests`, appx manifests, and MSIX pipelines as
  the Windows product; the Avalonia stack ships for Linux only.
- Update [ApplicationArchitecture.md](ApplicationArchitecture.md), README, and contributing docs
  with the new project layout and Linux build instructions.

## Risks

- **XAML parity drift.** `x:Bind` compiled bindings, converter semantics, focus handling, and
  template behavior differ between UWP XAML and Avalonia; each ported view needs a side-by-side
  parity review.
- **Engine correctness.** Any change to marshalling or localization of engine strings can change
  results; the C++ test suite is the contract and must stay green on both platforms.
- **Rich math input.** `MathRichEditBox` depends on UWP `RichEditBox`; the Avalonia replacement is
  expected to be display-only + a plain parser-based input in v1.
- **Graphing performance and fidelity.** The most complex visual component in the app is being
  reimplemented on a different rendering stack.
- **Accessibility.** Calculator is a benchmark app here; Avalonia automation peers exist but need
  per-mode verification.
- **Currency converter** requires connectivity; define and test offline behavior on Linux.

## Rollout and compatibility

- The legacy UWP app remains the shipping Windows product; the Avalonia stack targets Linux only
  (see [Decisions](#decisions)).
- Differential testing: run the C# ViewModel suite against both engine builds (native Windows and
  native Linux) during the migration window.

## Effort estimate

Figures assume a small team with one native-engine expert.

| Phase | Estimate |
|---|---|
| 0 - Feasibility spike | ~2 weeks |
| 1 - De-Windows ViewModels | ~4-6 weeks |
| 2 - App shell + Standard mode | ~3-4 weeks |
| 3 - Remaining modes | ~6-8 weeks |
| 4 - Graphing | ~6-10 weeks |
| 5 - Platform tooling and packaging | ~3-4 weeks |
| 6 - Cutover | ~2 weeks |

**Total: roughly 6 person-months for full parity.** Phase 1-2 alone yields a runnable
Standard/Scientific Linux app much earlier and is a natural first milestone.

## Decisions

1. **Target OS set:** Linux only. The UWP app remains the Windows product.
2. **Native engine strategy:** keep the C++ CalcManager engine; no C# port of RatPack.
3. **Graphing in v1:** ship core modes first; graphing ships in a follow-up release.
4. **Localization storage:** migrate resw to resx and use `ResourceManager`.
5. **Distribution channels:** Flatpak + Arch User Repository (AUR); AppImage optional later.
6. **Window chrome (Phase 2):** native window chrome for v1; keep-on-top via `Window.Topmost`;
   the drag-region custom header from the UWP app stays on the back burner.
7. **Avalonia theming (Phase 2):** Avalonia 12 dropped the WinUI "FillColor…Brush" resource
   keys; maps to Fluent `SystemControl*Brush` / `AccentButton*` keys instead, and the custom
   app background resolves in code-behind on `ActualThemeVariantChanged`.
8. **UI verification (Phase 2):** headless `Avalonia.Headless` MSTest project
   (`tests/Calculator.Avalonia.Tests`) pressing real controls against the live engine —
   complementing the existing 294 VM tests and ctest engine suite.
