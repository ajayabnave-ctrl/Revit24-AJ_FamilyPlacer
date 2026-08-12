# Revit24-AJ_FamilyPlacer 🚀

> **Revit 2024 C# add-in for automated MEP family placement using coordinate-based CSV input and space-aware grid generation, with hosting detection and Revit unit conversion.**

[![Revit 2024](https://img.shields.io/badge/Revit-2024-blue.svg)](https://www.autodesk.com/products/revit/overview)
[![Framework](https://img.shields.io/badge/.NET-4.8-purple.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net48)
[![Language](https://img.shields.io/badge/C%23-7.3-green.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## 📌 Overview

**Revit24-AJ_FamilyPlacer** is a powerful Autodesk Revit 2024 Add-in written in C# designed to streamline and automate the batch placement of MEP family instances (lighting fixtures, air diffusers, sprinklers, devices, etc.).

It features two placement modes:
1. **Coordinate-Based CSV Import**: Read exact `X, Y, Z` target points directly from external CSV files.
2. **Space-Aware Grid Engine**: Automatically calculates grid array placement points inside Revit `Space` boundaries with user-definable inward offsets and grid intervals.

---

## ✨ Key Features

* 📍 **Dual Data Sources**: Flexible placement using either pre-calculated CSV coordinates or automatic grid calculation within MEP space boundaries.
* 🛡️ **Space Boundary Verification**: Uses `Space.IsPointInSpace()` point-in-polygon verification to prevent placing components outside irregular space walls.
* 🎯 **Smart Hosting Detection**: Automatically detects family hosting behavior (`FAMILY_HOSTING_BEHAVIOR` parameter):
  * **Ceiling-based**: Uses 3D raycasting (`ReferenceIntersector`) to detect intersecting ceiling faces directly above placement points.
  * **Face-based**: Creates sketch planes on selected `ReferencePlane` geometry.
  * **Level-based**: Places instances at specified level elevations.
* 📏 **Revit 2024 Unit Conversion**: Uses modern `ForgeTypeId` and `UnitUtils` APIs to support seamless conversion between user display units (`mm`, `cm`, `m`, `ft`, `in`) and Revit internal feet units.
* 🎨 **Ribbon Integration**: Automatically registers an **MEP Tools** tab and **Place Families** button on Revit startup.

---

## 📁 Repository Structure

```
Revit24-AJ_FamilyPlacer/
├── FamilyPlacer/
│   ├── FamilyPlacer.csproj         # Visual Studio C# Project (.NET 4.8 / x64)
│   ├── App.cs                      # IExternalApplication (Ribbon UI registration)
│   ├── FamilyPlacerCommand.cs      # IExternalCommand (Core placement logic & UI pickers)
│   └── UnitHelper.cs              # Unit conversion utility (ForgeTypeId / UnitUtils)
├── installer/
│   └── FamilyPlacer.addin          # Revit Add-in manifest file
├── GUIDE.md                        # Developer build guide and detailed user manual
├── LICENSE                         # MIT License
└── README.md                       # Project documentation
```

---

## 🛠️ Installation & Setup

### Prerequisites
* **Autodesk Revit 2024**
* **Visual Studio 2022** with `.NET Desktop Development` workload
* **.NET Framework 4.8**

### 1. Build from Source
1. Open `FamilyPlacer.sln` in Visual Studio 2022.
2. Ensure reference paths for `RevitAPI.dll` and `RevitAPIUI.dll` point to your Revit 2024 installation:
   `C:\Program Files\Autodesk\Revit 2024\RevitAPI.dll`
3. Set configuration to `Release | x64` and click **Build Solution** (`Ctrl+Shift+B`).

### 2. Deploy Add-in to Revit 2024
Copy the compiled assembly and `.addin` manifest file to Revit's Addins folder:

* **Copy DLL**: `FamilyPlacer.dll` → `C:\ProgramData\Autodesk\Revit\Addins\2024\FamilyPlacer\`
* **Copy Manifest**: `installer/FamilyPlacer.addin` → `C:\ProgramData\Autodesk\Revit\Addins\2024\`

*(Note: Ensure the `<Assembly>` path inside `FamilyPlacer.addin` points to the absolute location of `FamilyPlacer.dll`)*

---

## 📖 How to Use

1. Launch Autodesk Revit 2024 and open your MEP model.
2. Navigate to the **MEP Tools** ribbon tab and click **Place Families**.
3. **Data Source Selection**:
   * Choose **YES** to select a CSV file containing `X, Y, Z` coordinates.
   * Choose **NO** to use automatic Space Grid calculation.
4. Select the target **Level**, **Space**, and **Reference Plane**.
5. Input clearance offset and grid intervals.
6. Select your target **Family** and **Type**.
7. Click **OK** to execute batch placement. A summary report displaying successful vs. skipped points will be shown.

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.
