# Revit24-AJ_Revit Family Placer

> Batch-place MEP families in Revit 2024 from a CSV of coordinates, or let the add-in generate a grid inside each Space. Handles ceiling-, face- and level-hosted families automatically.

[![Revit 2024](https://img.shields.io/badge/Revit-2024-blue.svg)](https://www.autodesk.com/products/revit/overview)
[![.NET Framework 4.8](https://img.shields.io/badge/.NET-4.8-purple.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net48)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Latest release](https://img.shields.io/github/v/release/ajayabnave-ctrl/Revit24-AJ_FamilyPlacer)](../../releases/latest)

<!-- TODO: replace with a real GIF of CSV placement -->
![CSV placement demo](docs/demo-csv.gif)

<!-- TODO: replace with a real GIF of grid placement -->
![Grid placement demo](docs/demo-grid.gif)

---

## What it does

Placing hundreds of lighting fixtures, diffusers, sprinklers or devices by hand is slow and error-prone. FamilyPlacer places them in one run and tells you which points it skipped and why.

Two placement modes:

1. **CSV import** - read exact `X, Y, Z` points from a file (for example, exported from DIALux, Dynamo or a spreadsheet).
2. **Space grid** - generate a grid of points inside Revit `Space` boundaries, with a configurable inward offset and grid spacing.

## Features

- **Space boundary check** - points are tested with `Space.IsPointInSpace()`, so nothing lands outside irregular walls.
- **Automatic hosting detection** - reads the family's hosting behaviour and places accordingly:
  - **Ceiling-based**: finds the ceiling face above each point using 3D ray casting (`ReferenceIntersector`).
  - **Face-based**: creates sketch planes on the selected reference plane.
  - **Level-based**: places at the chosen level's elevation.
- **Unit handling** - input in `mm`, `cm`, `m`, `ft` or `in`; converted to Revit internal units with the Revit 2024 `ForgeTypeId` / `UnitUtils` APIs.
- **Summary report** - shows how many points were placed and how many were skipped.
- **Ribbon button** - adds a **Place Families** button on startup.

---

## Install (no Visual Studio needed)

1. Download `FamilyPlacer-<version>.zip` from the [latest release](../../releases/latest).
2. Close Revit.
3. Unzip, then right-click `install.ps1` and choose **Run with PowerShell**. It copies the files to `C:\ProgramData\Autodesk\Revit\Addins\2024\` and writes the `.addin` manifest with the correct path.
4. Start Revit 2024. You should see the **Place Families** button on the ribbon.

**Manual install:** copy `FamilyPlacer.dll` to `C:\ProgramData\Autodesk\Revit\Addins\2024\FamilyPlacer\`, then copy `FamilyPlacer.addin` to `C:\ProgramData\Autodesk\Revit\Addins\2024\` and make sure its `<Assembly>` path points to the DLL.

> Windows may block downloaded DLLs. If the add-in does not load, right-click the DLL, open **Properties** and tick **Unblock**.

---

## Usage

1. Open your MEP model in Revit 2024.
2. Click **Place Families** on the ribbon.
3. Choose the data source: **Yes** for a CSV file, **No** for the automatic Space grid.
4. Select the **Level**, **Space** and **Reference Plane** (as the family's hosting requires).
5. Enter the clearance offset and grid spacing (grid mode).
6. Pick the **Family** and **Type**.
7. Click **OK**. A report shows placed and skipped points.

### CSV format

One point per row. See [`samples/points.csv`](samples/points.csv).

| Column | Meaning |
|---|---|
| `X` | X coordinate |
| `Y` | Y coordinate |
| `Z` | Z coordinate (height) |

- Header row: **yes** (first row is `X,Y,Z`).
- Units: the unit you select in the dialog applies to all three columns. <!-- VERIFY against the code -->
- Coordinates are in the same coordinate system as the Revit model. <!-- VERIFY: internal origin vs shared coordinates -->
- Use a dot as the decimal separator.

---

## Known limitations

<!-- VERIFY each line against the code, then edit or delete -->
- Revit 2024 only for now. Other versions require a rebuild against that version's API.
- Ceiling-hosted placement needs a ceiling directly above each point. Points with none are skipped and listed in the report.
- Points outside the selected Space are skipped.
- Linked-model ceilings: not supported yet.

Found a problem? Please [open an issue](../../issues) with your Revit version, the family type and a sample of your CSV.

---

## Build from source

**Requirements:** Revit 2024, Visual Studio 2022 (.NET desktop development workload), .NET Framework 4.8.

1. Open `FamilyPlacer.sln`.
2. Check that the references to `RevitAPI.dll` and `RevitAPIUI.dll` point to your Revit 2024 install.
3. Set the configuration to `Release | x64` and build.

Details are in [GUIDE.md](GUIDE.md).

### Repository structure

```
Revit24-AJ_FamilyPlacer/
├── FamilyPlacer.sln
├── FamilyPlacer/
│   ├── FamilyPlacer.csproj
│   ├── App.cs                    # Ribbon registration (IExternalApplication)
│   ├── FamilyPlacerCommand.cs    # Placement logic and pickers (IExternalCommand)
│   └── UnitHelper.cs             # Unit conversion (ForgeTypeId / UnitUtils)
├── installer/
│   ├── FamilyPlacer.addin        # Revit manifest
│   └── install.ps1               # One-click installer
├── samples/
│   └── points.csv
├── docs/                         # Screenshots and GIFs
├── GUIDE.md
├── LICENSE
└── README.md
```

## License

MIT. See [LICENSE](LICENSE).

## Author

Ajay Abnave - BIM Manager and AEC technology professional, Pune, India.
More tools: [github.com/ajayabnave-ctrl](https://github.com/ajayabnave-ctrl)
