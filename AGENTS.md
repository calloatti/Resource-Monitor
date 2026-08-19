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
