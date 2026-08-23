Include ..\AGENTS.md

# Resource Monitor — Mod-Specific Agent Instructions

## Identity
- **Assembly:** `resourcemonitor`
- **Namespace:** `Calloatti.ResourceMonitor`
- **ModId:** `Calloatti.ResourceMonitor`
- **Framework:** Bindito DI
- **Publicizer:** `Timberborn.GameDistricts`, `Timberborn.BlueprintSystem`, `Timberborn.CoreUI` are publicized via `CommonModSettings.props`, with `DoNotPublicize` for `DistrictBuilding.Reassigned*`, `RadioToggle.RadioButtonSelected`, and `ComponentSpec.EqualityContract`/`PrintMembers` (see csproj)
- **Min Game Version:** 1.0.0.0 — uses `timberborn-decompiled-1.0.*`

## What This Mod Does
Adds a resource monitor building that displays real-time good amounts and throughput. Includes banner setter for visual feedback, goods dropdown selector, and entity panel fragment.

## Entity Panel Fragment Ordering
- Entity panel content fragments sort ascending by `Order`; vanilla constants: Top=0, Middle=1000, Bottom=2000, Footer=3000 (see `Timberborn.EntityPanelSystem.cs`)
- Vanilla automation modules register middle fragments on top of the counter settings fragment: `TransmitterFragment` (state/light + usages) at order 100 → 1100, `AutomatableFragment` at Bottom+100 → 2100
- Our monitor fragment must render ABOVE the transmitter status/usage fragment, matching vanilla `ResourceCounterFragment` placement
- V1.0: `AddMiddleFragment(_fragment)` → 1000 (correct)
- V1.1: MUST use `AddMiddleFragment(_fragment, 50)` → 1050. Do NOT use `100` — that ties with `TransmitterFragment` (1100) and the stable-sort tie lets the automation fragment render above ours (observed bug)

## Source Architecture (`Version-1.0/Source/`)

| File | Role |
|---|---|
| `ResourceMonitorModStarter.cs` | Entry point — `IModStarter` |
| `ResourceMonitorConfigurator.cs` | DI configurator |
| `ResourceMonitor.cs` | Core monitor component |
| `ResourceMonitorFragment.cs` | Entity panel UI fragment |
| `ResourceMonitorBannerSetter.cs` | Banner visual customization |
| `ResourceMonitorGoodsDropdownProvider.cs` | Goods dropdown data provider |
| `ResourceMonitorSpec.cs` | ComponentSpec record |

## Version Folders
- `Version-1.0` — targets game 1.0.x.x
- `Version-1.1` — targets game 1.1.x.x

## Hard Rule
DO NOT EVER TOUCH THE DEPLOY FOLDER.

BUILD DOES EVERYTHING, NEVER EVER MESS WITH THE DEPLOY PROCESS.
