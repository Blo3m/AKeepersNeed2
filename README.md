# AKeepersNeed2

A [BepInEx](https://github.com/BepInEx/BepInEx) mod for **Graveyard Keeper 2**.

## Features

Everything is configured in-game through a native-styled menu with sideways-scrollable tabs.
The menu hotkey (default **F1**) opens and closes it, and Esc also closes it. Esc first
backs out of whatever you're in the middle of: it closes an open dialog, leaves a text field,
and while rebinding a key it unbinds that key. Pages taller than the window scroll vertically.
Drag the menu by its title bar to move it; Settings → Menu Position → Reset puts it back on the
right. When the mod is installed, the main menu shows
"A Keepers Need 2 loaded (v…)" under the game's version.

**Player**
- **Energy regen** — passive energy regeneration while awake (energy per 5s)
- **Infinite energy** — energy never decreases
- **Instant sleep** — energy is full as soon as you fall asleep; you wake up about 2 seconds later
- **Infinite health** — you take no HP damage
- **Infinite stamina** — combat stamina (sword/bow attacks) never drains
- **Ignore gathering mastery** — chop, mine and dig objects above your mastery level, as if
  you just met the requirement
- **Gathering speed** — chop, mine, dig and harvest with fewer swings (1–20×)
  - **One hit** — every swing finishes the object
- **Teleport to cursor** — press a key (default middle mouse, rebindable) to teleport to the
  nearest walkable spot under the cursor, outdoors or indoors
- **Movement speed** — walk faster or slower (0.5–5×), always or as a sprint: hold or toggle a
  key (unbound by default). Optionally ignore the game's slowdown while attacking
- **Infinite durability** — tools and items used up by durability in crafts never wear down
- **Inventory slots** — set how many slots your inventory has (1–200, with confirmation)
- **Money and happiness** — view and set your money and happiness (asks for confirmation, since
  it changes your save)
- **Trade income** — vendors pay more for what you sell (1–50×), optionally even when they
  can't afford it
- **Other income** — multiply money from everything except trading, like quests and sermons (1–50×)

**World**
- **Day length** — how many real minutes a game day lasts (1–60, vanilla 5). Only the clock
  changes; crafts and crops keep their speed
- **Freeze time** — stop the clock (it still runs while you sleep)
- **Skip time** — jump forward to the next sunrise, noon, sunset or midnight (with confirmation);
  time events fire as if the time had passed
- **Weather** — pick a weather (clear, wind, rain from light to storm, mist) and set it; shows once
  the menu closes
- **Lock weather** — stop the weather from changing on its own; story weather still plays, then
  your weather comes back
- **Zone quality** — set the quality of the graveyard, church and other zones, and the town (with
  confirmation; milestone rewards pay out as if earned)

**Progression** (foldable sections)
- **Tech points** — view and set your red, green and blue tech points (with confirmation)
- **Reputation** — pick an NPC and view or set your reputation with them (with confirmation)
- **Reputation multiplier** — multiply reputation gained with NPCs and districts (1–20×)
- **Tech** — search and filter every tech by tab; unlock one, a whole tab or everything
  (optionally including hidden story and reputation techs), with confirmation
- **Recipes** — unlock all crafting recipes, buildings, town buildings or alchemy formulas (with
  confirmation)
- **Quests** — every active quest with its giver, hand-in and description; give its hand-in items,
  or force-complete it (with a strong warning)

**Drops**
- **Resource drops** — multiply loot from destroyed/harvested world objects
- **Tech points** — multiply technology-point rewards
- **Craft output** — multiply craft-station output quantity

**Crafting**
- **Free crafting** — craft without the required items; nothing is consumed (includes
  alchemy and tool durability)
- **Instant crafting** — your crafts finish immediately (garden growing, star, autopsy and
  zombie crafts keep their normal time)
- **Ignore crafting mastery** — run crafts above your mastery level, including star crafts and
  autopsies, as if you just met the requirement
- **Free building** — build without the required items; nothing is consumed (includes
  town buildings)

**Zombies**
- **Zombie craft speed** — zombies work crafting stations faster (1–50×)
- **Zombie gather speed** — zombies chop, mine and harvest faster (1–50×)
- **Zombie walk speed** — every zombie walks faster or slower (0.5–10×)
- **Max mastery** — zombies work with enough mastery for every job at full output (not saved;
  the boosted value shows in the zombie window while on)
- **Porter capacity** — porters carry 4–50 slots; applies to new porters, with a button to resize
  existing ones (with confirmation)
- **Zombie list** — every placed zombie with its face, job, station and zone; search by name and
  filter by job. Open any zombie in the game's own zombie window
- **Zombie editor** — expand a zombie to give it its own craft/gather/walk speed and porter
  capacity, edit its talents and tech points, learn or remove perks (optionally hidden ones) and
  ignore its perk cap. Changes are applied together after a confirmation

**Garden**
- **Growth speed** — crops grow faster (1–100×)
- **Instant grow** — crops finish growing right after planting
- **Ignore garden mastery** — plant and grow crops without the gardening mastery they need

**Fishing**
- **Instant bite** — fish bite as soon as you cast
- **Auto-hook** — fish are hooked for you when they bite
- **Auto-win** — a hooked fish is caught at once, no fight
- **No line snap** — the line never breaks
- **No escape** — a hooked fish can't get away
- **Extra fish** — each catch gives more fish (1–50)
- **Infinite stock** — fishing spots never run out

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
- Every hotkey can be rebound in the Settings tab (Esc or Backspace unbinds; the menu key can't be unbound).

### Config files

- `BepInEx/config/AKeepersNeed2.cfg` — global settings: menu hotkey, UI scale, menu position,
  profile keys,
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
