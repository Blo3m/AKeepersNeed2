# AKeepersNeed2

A [BepInEx](https://github.com/BepInEx/BepInEx) mod for **Graveyard Keeper 2**.

## Features

Configurable in-game via a native-styled menu (default hotkey **F1**), persisted to
`BepInEx/config/AKeepersNeed2.cfg`:

- **Energy regen** — passive energy regeneration while awake (energy per 5s)
- **No energy drain** — energy never decreases
- **Resource drops** — multiply loot from destroyed/harvested world objects
- **Tech points** — multiply technology-point rewards
- **Craft output** — multiply craft-station output quantity

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
  </ItemGroup>
</Project>
```
