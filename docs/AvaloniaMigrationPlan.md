# AvaloniaUI / Linux Migration Plan

This document describes the plan to migrate Windows Calculator from its current UWP XAML stack to
[AvaloniaUI](https://avaloniaui.net/) so that it can run on Linux. It also covers
the platform-neutralization work required in the ViewModel and native engine layers.

**Status:** Phases 0-1 complete — the engine builds and passes its tests on Linux via
CMake/ctest, the C ABI shim (`src/CalcManager.Interop/calc_api.*`) is exercised from net10.0
through source-generated P/Invoke, an Avalonia 12 demo window evaluates expressions with the
native engine (Phase 0), and the ViewModels have been de-Windowed: they build for net10.0
alongside the UWP project and the full test suite passes on Linux against the live native engine
(Phase 1, 294/294 tests). Key decisions are recorded in [Decisions](#decisions).

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
src/Calculator.ViewModels.Tests  xUnit on net10.0
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

- Scaffold `src/Calculator.Avalonia` (Avalonia 12, net10.0, `App.axaml` + `MainWindow`).
- Implement window chrome, mode navigation, and the history/memory panel with its
  flyout↔docked resize behavior.
- Port the Standard calculator view end-to-end: display, number pad, operators, history, and
  memory.
- Port shared controls and converters; set up theming (light/dark/high-contrast parity).

**Exit criteria:** Standard mode at visual/behavioral parity on Linux, driven by the existing
`StandardCalculatorViewModel`.

### Phase 3 - Remaining modes (optional)

*Optional: the app is fully usable with Standard mode; this phase extends coverage to every
non-graphing mode.*

- Scientific (including Shift/inverse button collapse states), Programmer (bit-flip and radix
  panels), Date calculation, and Unit Converter (all categories + live currency).
- Settings page, snapshots, keyboard shortcut parity.

**Exit criteria:** every non-graphing mode passes a manual parity checklist on Linux.

### Phase 4 - Graphing (optional)

*Optional; also the highest-risk phase. The current graphing UI is a native UWP C++ control.*

- Keep the C++ `GraphingImpl` evaluator behind the C ABI.
- Implement `PlotControl` (SkiaSharp): grid/axes, pan, zoom, trace, key graph features.
- Port `EquationInputArea`, `EquationStylePanelControl`, `GraphingSettings`, `GraphingNumPad`.

**Exit criteria:** graphing mode at parity for supported equation types, with acceptable
interaction frame rates on a mid-range Linux desktop.

### Phase 5 - Platform tooling and packaging

- GitHub Actions on `ubuntu-latest`: engine CMake + ctest pipeline and `dotnet build/test`
  pipeline; keep existing Windows CI as long as the UWP app ships.
- Distribution: Flatpak manifest and Arch User Repository (AUR) packaging with desktop entry and
  icons; AppImage optional later.
- Localization pipeline for the converted resx catalogs; RTL (Hebrew/Arabic) verification on
  Linux.

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
