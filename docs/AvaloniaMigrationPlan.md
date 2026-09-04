# AvaloniaUI / Linux Migration Plan

This document describes the plan to migrate Windows Calculator from its current UWP XAML stack to
[AvaloniaUI](https://avaloniaui.net/) so that it can run on Linux. It also covers
the platform-neutralization work required in the ViewModel and native engine layers.

**Status:** draft. Decision points are called out in [Open decisions](#open-decisions).

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
  - [Phase 3 - Remaining modes](#phase-3---remaining-modes)
  - [Phase 4 - Graphing](#phase-4---graphing)
  - [Phase 5 - Platform tooling and packaging](#phase-5---platform-tooling-and-packaging)
  - [Phase 6 - Cutover](#phase-6---cutover)
- [Risks](#risks)
- [Rollout and compatibility](#rollout-and-compatibility)
- [Effort estimate](#effort-estimate)
- [Open decisions](#open-decisions)

---

## Motivation

- Run the calculator on Linux (and, almost for free, macOS) while preserving bit-identical
  calculation results.
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
| Packaging / CI | MSIX bundle, Azure Pipelines, Windows-hosted runners | — | Flatpak / AppImage / deb, GitHub Actions on `ubuntu-latest` |

## Target architecture

```
src/Calculator.Avalonia          Avalonia 11, net8.0 (View, see detailed UI port mapping)
src/Calculator.ViewModels        net8.0 class library (CommunityToolkit.Mvvm)
src/Calculator.ViewModels.Tests  xUnit on net8.0
src/CalcManager                  C++ (unchanged engine), CMake build on Linux + Windows
src/CalcManager.Interop          thin C API shim exposing the engine to P/Invoke
src/GraphingImpl                 C++ evaluator (unchanged), exposed through the same C API
src/PlotControl                  Avalonia control rendering plots via SkiaSharp
build/pipelines/*.yaml           GitHub Actions workflows for linux CI (engine + dotnet)
packaging/                       Flatpak manifest, AppImage, deb metadata
```

Cross-platform targets for Avalonia: Windows, Linux, macOS (decided in
[Open decisions](#open-decisions)).

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

| Windows / UWP API (`Windows.*`) | .NET 8 replacement |
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
- Define the initial C API surface for the engine; call it from a net8.0 console app via P/Invoke
  to validate string/memory marshalling.
- Prototype one Avalonia window that displays a calculator expression evaluated by the native
  engine.

**Exit criteria:** all C++ engine tests pass on Linux; a P/Invoke round trip succeeds; a demo
Avalonia app runs on Ubuntu.

### Phase 1 - De-Windows the ViewModels

*Largest mechanical effort; delivers testable logic on Linux.*

- Retarget `Calculator.ViewModels` to a net8.0 class library.
- Replace every `Windows.*` dependency listed in the
  [Platform API replacement guide](#platform-api-replacement-guide), behind thin abstractions
  where multiple implementations exist (storage, clipboard, dispatcher, telemetry).
- Convert engine resource strings from `.resw` to `.resx` (or a runtime JSON equivalent that the
  native engine can also load).
- Retire `CalcManager.Interop` in favor of the C ABI shim.
- Port `Calculator.Tests` to xUnit on net8.0; keep every existing test green before touching UI.

**Exit criteria:** `dotnet test` passes on Linux for the full ViewModel suite.

### Phase 2 - App shell and Standard mode

- Scaffold `src/Calculator.Avalonia` (Avalonia 11, net8.0, `App.axaml` + `MainWindow`).
- Implement window chrome, mode navigation, and the history/memory panel with its
  flyout↔docked resize behavior.
- Port the Standard calculator view end-to-end: display, number pad, operators, history, and
  memory.
- Port shared controls and converters; set up theming (light/dark/high-contrast parity).

**Exit criteria:** Standard mode at visual/behavioral parity on Linux, driven by the existing
`StandardCalculatorViewModel`.

### Phase 3 - Remaining modes

- Scientific (including Shift/inverse button collapse states), Programmer (bit-flip and radix
  panels), Date calculation, and Unit Converter (all categories + live currency).
- Settings page, snapshots, keyboard shortcut parity.

**Exit criteria:** every non-graphing mode passes a manual parity checklist on Linux.

### Phase 4 - Graphing

*Highest-risk phase; the current graphing UI is a native UWP C++ control.*

- Keep the C++ `GraphingImpl` evaluator behind the C ABI.
- Implement `PlotControl` (SkiaSharp): grid/axes, pan, zoom, trace, key graph features.
- Port `EquationInputArea`, `EquationStylePanelControl`, `GraphingSettings`, `GraphingNumPad`.

**Exit criteria:** graphing mode at parity for supported equation types, with acceptable
interaction frame rates on a mid-range Linux desktop.

### Phase 5 - Platform tooling and packaging

- GitHub Actions on `ubuntu-latest`: engine CMake + ctest pipeline and `dotnet build/test`
  pipeline; keep existing Windows CI until cutover.
- Distribution: Flatpak manifest, AppImage, and deb packaging with desktop entry and icons.
- Localization pipeline for the converted resx catalogs; RTL (Hebrew/Arabic) verification on
  Linux.

### Phase 6 - Cutover

- Archive the UWP `Calculator` project, `CalculatorUITests`, appx manifests, and MSIX pipelines.
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

- The legacy UWP app remains the shipping Windows product until Phase 6 cutover criteria are met
  on the new stack.
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

## Open decisions

1. **Target OS set:** Linux-only first, or Windows + macOS from day one (recommended: target all
   three; cost is low once UWP-specific dependencies are removed)?
2. **Native engine strategy:** keep C++ CalcManager (recommended) vs. a full C# port of the RatPack
   engine?
3. **Graphing in v1:** ship core modes first and graphing in a follow-up release (recommended)?
4. **Localization storage:** migrate resw to resx, or adopt a runtime JSON resource scheme shared
   by C# and native code?
5. **Distribution channels:** Flatpak only, or AppImage + deb as well?
