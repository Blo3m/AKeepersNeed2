# AKeepersNeed2

A [BepInEx](https://github.com/BepInEx/BepInEx) mod for **Graveyard Keeper 2**.

## Features

Everything is configured in-game through a native-styled menu (default hotkey **F1**) with
sideways-scrollable tabs.

**Player**
- **Energy regen** — passive energy regeneration while awake (energy per 5s)
- **No energy drain** — energy never decreases

**Drops**
- **Resource drops** — multiply loot from destroyed/harvested world objects
- **Tech points** — multiply technology-point rewards
- **Craft output** — multiply craft-station output quantity

**Crafting**
- **Free crafting** — craft without the required items; nothing is consumed (includes
  alchemy and tool durability)
- **Instant crafting** — crafts finish immediately (garden growing, star and autopsy crafts
  keep their normal time)
- **Free building** — build without the required items; nothing is consumed (includes
  town buildings)

**Items**
- **Item adder** — search or filter every item by category and add any amount to your
  inventory

**Map**
- **Reveal map** — shows every zone and area name as if fully explored (display-only)
- **Unlock milestones** — draws every milestone as activated, so you can teleport to it
  (display-only)
- **Show NPCs** — named NPCs appear on the map as their face, moving live while the
  map is open; hover a face to see their name
  - **Shift-click NPC to teleport** — shift-click a face to teleport next to that NPC
- **Shift-click map teleport** — shift-click anywhere on the map to teleport to the nearest
  walkable spot (outdoors only)

### Profiles

Save different sets of options as named profiles and switch between them at any time.
Switching takes effect immediately.

- Create, rename, delete, and reset profiles from the **Settings** tab. **Default** always
  exists and can't be renamed or deleted.
- Edits are saved to the active profile automatically.
- Switch with **F2** / **F3** (previous / next), or give any profile its own hotkey. A short
  on-screen notice confirms the switch.
- Every hotkey can be rebound in the Settings tab (Esc cancels, Backspace unbinds).

### Config files

- `BepInEx/config/AKeepersNeed2.cfg` — global settings: menu hotkey, UI scale, profile keys,
  and the active profile.
- `BepInEx/config/AKeepersNeed2/profiles/<name>.cfg` — one readable file per profile. Copy
  them to share or back up profiles; any `.cfg` dropped in this folder is picked up on the
  next launch.

## Building

Requires the [.NET SDK](https://dotnet.microsoft.com/download) and a local
Graveyard Keeper 2 install.

1. Clone the repository.
2. Run `./build.ps1` — the game DLLs are sourced from your local install (located via
   Steam), and the built plugin is copied into `<game>/BepInEx/plugins/` after a prompt.

```powershell
./build.ps1                      # build (Debug) + deploy prompt
./build.ps1 -NoDeploy            # build only
./build.ps1 -Configuration Release
```

Output: `bin/Debug/net461/AKeepersNeed2.dll`.

If your game isn't found automatically, create `GameReferences.props` next to the
`.csproj` with `GameManagedDir` pointing at your `GraveyardKeeper2_Data/Managed` folder:

```xml
<Project>
  <PropertyGroup>
    <GameManagedDir>C:\Path\To\Graveyard Keeper 2\GraveyardKeeper2_Data\Managed</GameManagedDir>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="Assembly-CSharp"><HintPath>$(GameManagedDir)\Assembly-CSharp.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="Assembly-CSharp-firstpass"><HintPath>$(GameManagedDir)\Assembly-CSharp-firstpass.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="LazyBearTechnology"><HintPath>$(GameManagedDir)\LazyBearTechnology.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="UnityEngine.UI"><HintPath>$(GameManagedDir)\UnityEngine.UI.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="Unity.TextMeshPro"><HintPath>$(GameManagedDir)\Unity.TextMeshPro.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="AstarPathfindingProject"><HintPath>$(GameManagedDir)\AstarPathfindingProject.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="PackageTools"><HintPath>$(GameManagedDir)\PackageTools.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="Drawing"><HintPath>$(GameManagedDir)\Drawing.dll</HintPath><Private>false</Private></Reference>
  </ItemGroup>
</Project>
```
