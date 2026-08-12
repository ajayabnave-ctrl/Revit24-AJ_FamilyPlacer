# MEP Family Placer — Revit 2024 Add-In
## Complete Step-by-Step Guide
### For Developers (Build & Install) and Users (Workflow)

---

## PART A — PROJECT STRUCTURE

```
FamilyPlacer/                        ← Solution root folder
│
├── FamilyPlacer/                    ← C# project folder
│   ├── FamilyPlacer.csproj          ← .NET 4.8 project file (Revit 2024 target)
│   ├── App.cs                       ← IExternalApplication — registers ribbon tab/button
│   ├── FamilyPlacerCommand.cs       ← IExternalCommand — entry point, Step 1 check
│   ├── FamilyPlacerWizard.xaml      ← WPF wizard UI (Steps 2–11)
│   ├── FamilyPlacerWizard.xaml.cs   ← Wizard code-behind (all step logic)
│   ├── PlacementContext.cs          ← Data container shared across all steps
│   ├── SpaceGeometryHelper.cs       ← Space bounding box, grid point generation
│   ├── FamilyPlacementEngine.cs     ← Face-based & level-based placement
│   └── UnitHelper.cs               ← Revit 2024 ForgeTypeId unit conversion
│
└── installer/
    └── FamilyPlacer.addin           ← Revit manifest (copy to ProgramData)
```

---

## PART B — DEVELOPER SETUP (Build from Source)

### B.1  Prerequisites

| Software | Version | Where to Download |
|---|---|---|
| Visual Studio | 2022 Community or higher | visualstudio.microsoft.com |
| .NET SDK | 4.8 (Framework) | Already included with VS 2022 |
| Revit 2024 | Full install | Autodesk desktop app / subscription |
| Revit 2024 SDK | Optional but recommended | Autodesk Developer Network |

> **Check Revit is installed first** — the build references:
> `C:\Program Files\Autodesk\Revit 2024\RevitAPI.dll`
> `C:\Program Files\Autodesk\Revit 2024\RevitAPIUI.dll`

---

### B.2  Create the Visual Studio Solution

**Step 1 — Open Visual Studio 2022**
- Start Menu → Visual Studio 2022 → Open

**Step 2 — Create a new project**
- Click **Create a new project**
- Search for: `Class Library (.NET Framework)`
- Select **Class Library (.NET Framework)** → click **Next**

**Step 3 — Configure the project**
- Project name : `FamilyPlacer`
- Location     : choose any folder, e.g. `C:\Dev\RevitAddins\`
- Solution name: `FamilyPlacer`
- Framework    : **.NET Framework 4.8**
- Click **Create**

**Step 4 — Replace the generated project file**
- In Solution Explorer, right-click `FamilyPlacer` project → **Edit Project File**
- Replace everything with the contents of `FamilyPlacer.csproj` provided

---

### B.3  Add all source files

Copy (or recreate) each file into the `FamilyPlacer\FamilyPlacer\` project folder:

| File | How to add |
|---|---|
| `App.cs` | Project → Add → Existing Item |
| `FamilyPlacerCommand.cs` | Project → Add → Existing Item |
| `PlacementContext.cs` | Project → Add → Existing Item |
| `UnitHelper.cs` | Project → Add → Existing Item |
| `SpaceGeometryHelper.cs` | Project → Add → Existing Item |
| `FamilyPlacementEngine.cs` | Project → Add → Existing Item |
| `FamilyPlacerWizard.xaml` | Project → Add → New Item → **Window (WPF)** — name it `FamilyPlacerWizard.xaml`, then replace content |
| `FamilyPlacerWizard.xaml.cs` | Auto-created with XAML file; replace content |

> **WPF setup tip:** Right-click project → Properties → Application tab →
> Output type: **Class Library** (NOT Windows Application).
> WPF references (PresentationCore, PresentationFramework, WindowsBase) must be present.

---

### B.4  Add Revit API references

1. Solution Explorer → Right-click **References** → **Add Reference**
2. Click **Browse** → navigate to `C:\Program Files\Autodesk\Revit 2024\`
3. Select `RevitAPI.dll` → click **Add**
4. Repeat for `RevitAPIUI.dll`
5. For **each** of these two references:
   - Select reference in Solution Explorer
   - Properties panel → **Copy Local** = `False`
   - This prevents the DLL from being copied to output (Revit loads them itself)

---

### B.5  Build the add-in

1. Build menu → **Build Solution** (or `Ctrl+Shift+B`)
2. Check Output window — must end with:
   ```
   Build succeeded.
   0 Error(s)
   ```
3. Output DLL location:
   ```
   C:\Dev\RevitAddins\FamilyPlacer\FamilyPlacer\bin\Debug\FamilyPlacer.dll
   ```

---

### B.6  Install into Revit 2024

**Step 1 — Create the Revit addins deployment folder** (if it does not exist)
```
C:\ProgramData\Autodesk\Revit\Addins\2024\FamilyPlacer\
```
> `C:\ProgramData` is a hidden folder. Type the path directly in Windows Explorer address bar.

**Step 2 — Copy files**
Copy these three items into the folder above:
```
FamilyPlacer.dll          ← from bin\Debug\ or bin\Release\
FamilyPlacer.addin        ← from installer\ folder
FamilyPlacer32.png        ← optional 32×32 ribbon icon (PNG)
```

**Step 3 — Edit the .addin file**
Open `FamilyPlacer.addin` in Notepad and update:
```xml
<Assembly>C:\ProgramData\Autodesk\Revit\Addins\2024\FamilyPlacer\FamilyPlacer.dll</Assembly>
```
> Use the **full absolute path** to the DLL.

**Step 4 — Start Revit 2024**
- Revit will automatically scan the Addins folder on startup
- You should see a new tab: **MEP Tools** in the ribbon
- Inside it: panel **Family Placement** with button **Place Families**

---

## PART C — USER WORKFLOW (Inside Revit 2024)

### Prerequisites before running the add-in

| Requirement | Where in Revit |
|---|---|
| Open the MEP project (.rvt) | Normal file open |
| Link the Architectural model | Insert tab → Link Revit → use Shared Coordinates |
| MEP Spaces created | Analyze tab → Spaces → Place Spaces Automatically |
| Space names match ARK Room names | Analyze tab → Space Naming Utility |
| MEP families loaded | Insert tab → Load Family |
| Active view is a floor/ceiling plan | View tab → floor plan or reflected ceiling plan |

---

### STEP 1 — Launch the add-in and Room/Space check

1. Click **MEP Tools** tab → **Family Placement** panel → **Place Families** button
2. The add-in **automatically** checks:
   - Whether any Spaces exist in the MEP model
   - Whether Space names match Room names in the linked ARK model
3. **If no Spaces exist:**
   - A detailed dialog appears with instructions to create Spaces
   - Add-in stops here — create Spaces and re-run
4. **If names don't match:**
   - A warning dialog lists mismatched Space names
   - You can still proceed; mismatched Spaces appear in the wizard
   - To fix: Analyze → Space Naming Utility → assign names from linked rooms

> **Space Naming Utility path:** Ribbon → Analyze tab → Spaces & Zones panel → Space Naming Utility

---

### STEP 2 — Select Level and Space

The wizard opens showing **Tab 2: Level / Space**

**Dropdown 1 — Level**
- Lists all levels in the MEP model ordered by elevation
- Select the level where families will be hosted
- Example: select `L02 - First Floor`

**Dropdown 2 — Space**
- Automatically filtered to show only Spaces on the selected level
- Displays as: `101 – Reception` (number – name)
- Select the target Space
- The Space area is shown below the dropdown for confirmation

Click **Next ▶**

---

### STEP 3 — Pick the Grid Origin Point

The wizard shows **Tab 3: Origin**

1. Click **📍 Pick Origin in Revit View**
   - The wizard minimises temporarily
   - Revit enters Pick Point mode with snap enabled
2. Snap to your preferred origin — snap types available:
   - **Grid intersection** (best: use Intersections snap)
   - **Column corner** (Endpoints snap)
   - **Inner corner of walls** (Endpoints snap)
   - **Centre of a detail circle** (Centers snap)
   - **South-West corner of the Space** (Nearest snap)
   - **Any point on the floor/ceiling plane**
3. Click to confirm the point
4. The wizard reopens showing the picked coordinates
5. If you need to re-pick, click the button again

Click **Next ▶**

---

### STEP 4 — Set Inward Offset Distance

The wizard shows **Tab 4: Offset**

- The unit label (mm / m / ft / in) matches your **active Revit project units** automatically
- Type the clearance distance from the Space boundary
  - Example for mm units: type `300` for 300 mm clearance
  - Example for feet units: type `1` for 1 foot clearance
- This shrinks the placement boundary **inward on all four sides**
- Families outside this shrunk boundary will be excluded

> **Typical values:** 300–600 mm (MEP clearance from walls)

Click **Next ▶**

---

### STEP 5 — Review Effective Spans

The wizard shows **Tab 5: Spans**

- The add-in calculates and displays:
  ```
  Longest X : 8400 mm
  Longest Y : 5200 mm
  ```
  *(after subtracting the offset from both sides)*
- Also shows raw Space extents and offset applied
- These values guide your grid interval choice in Step 6

Click **Next ▶**

---

### STEPS 6 & 7 — Set Grid Intervals and Review Divisions

The wizard shows **Tab 6–7: Grid**

**Enter tentative spacing:**

| Field | Example | Meaning |
|---|---|---|
| Tentative X Interval | `1500` mm | Approximate spacing between fixture columns |
| Tentative Y Interval | `1500` mm | Approximate spacing between fixture rows |

**The add-in automatically calculates:**
- Integer number of equal divisions
- Adjusted (exact) interval so the span divides perfectly
- Total intersection count

**Example result for X = 8400 mm, interval 1500 mm:**
```
Divisions    : 6  (7 points)
Adjusted interval : 1400 mm
```

**Example result for Y = 5200 mm, interval 1500 mm:**
```
Divisions    : 3  (4 points)
Adjusted interval : 1733 mm
```
```
Total intersections : 7 × 4 = 28
```

> Adjust the tentative interval until you get the desired count.
> Only intersections inside the Space boundary will receive a family instance.

Click **Next ▶**

---

### STEP 8 — Select Family and Type

The wizard shows **Tab 8: Family**

**Dropdown 1 — Family**
- Lists all families loaded in the current Revit project
- Ordered alphabetically
- Example: select `Downlight - Recessed`

**Dropdown 2 — Family Type (Symbol)**
- Populates automatically from the selected family
- Example: select `150 mm Round`

The info panel shows the family category and whether it is face-based.

> **Family not in the list?**
> Close the wizard → Ribbon → Insert tab → Load Family → select the .rfa file → reopen add-in.

Click **Next ▶**

---

### STEP 9 — Confirm Host Type

The wizard shows **Tab 9: Host Type**

- The add-in **automatically detects** whether the family is Face-Based or Level-Based
- The detection result appears in a highlighted box:
  - `🖼 Face-Based` — will be placed on a selected face
  - `📐 Level-Based` — will be placed at level + offset height
- If the automatic detection is wrong, use the radio buttons to override

Click **Next ▶**

---

### STEP 10 — Pick Host Face *(Face-Based families only)*

The wizard shows **Tab 10–11: Placement** → Face-Based panel

1. Click **🖱 Pick Face in Revit View**
   - The wizard minimises
   - Revit enters Face Pick mode
2. Hover over and click the desired face:
   - **Ceiling underside** — for ceiling-mounted lights, diffusers, sprinklers
   - **Floor top face** — for floor-mounted fixtures
   - **Wall face** — for wall-mounted fixtures
   - Any structural or architectural face visible in the view
3. The wizard reopens confirming the element name of the picked face

> **Tip:** Switch to a 3D view before picking if the face is not visible in plan.
> The wizard reopens after picking in any view.

---

### STEP 11 — Level Offset *(Level-Based families only)*

The wizard shows **Tab 10–11: Placement** → Level-Based panel

- The selected level from Step 2 is shown in the hint
- Enter the **vertical offset** above that level
  - `0` = at level elevation (floor-mounted)
  - `2700` mm = 2.7 m above level (ceiling-mounted typical)
  - Negative values = below level
- The add-in sets the offset parameter on each placed instance

---

### Final — Place Families button

Click **✅ Place Families**

- The wizard validates all inputs
- If the origin was not picked, it defaults to the SW corner of the Space
- All grid intersection points inside the Space boundary are generated
- Families are placed at each point

---

### STEP 12 — Placement Summary

A summary dialog appears:
```
✅  24 fixture(s) placed

Space  : 101 – Reception
Level  : L02 - First Floor
Family : Downlight - Recessed  –  150 mm Round
Grid   : 7 × 4  =  28 intersections
Placed : 24  (intersections inside boundary)
```

> The count difference (28 vs 24) means 4 grid points fell outside the
> Space boundary polygon and were skipped — correct behaviour.

---

## PART D — TROUBLESHOOTING

| Issue | Cause | Fix |
|---|---|---|
| Ribbon tab "MEP Tools" missing | .addin not found or wrong path | Check full path in .addin Assembly element |
| "No MEP Spaces found" | Spaces not created | Analyze → Spaces → Place Spaces Automatically |
| Space list is empty for a level | No spaces on that level | Switch level or create spaces on that level |
| Family not in dropdown | Not loaded in project | Insert → Load Family → select .rfa |
| 0 fixtures placed | Grid origin outside space | Re-pick origin inside the space |
| Wrong unit in offset field | Project units mismatch | File → Project Information → Units to verify |
| Face pick doesn't work in plan | Face not selectable in 2D | Switch to 3D view for face pick |

---

## PART E — REBUILDING AFTER CHANGES

1. Close Revit completely
2. Make edits in Visual Studio
3. Build → `Ctrl+Shift+B`
4. Copy new `FamilyPlacer.dll` to the Addins folder (overwrite existing)
5. Restart Revit

> **Never rebuild while Revit is open** — Revit locks the DLL.

---

## PART F — DEPLOYMENT PATHS SUMMARY

| File | Destination |
|---|---|
| `FamilyPlacer.dll` | `C:\ProgramData\Autodesk\Revit\Addins\2024\FamilyPlacer\` |
| `FamilyPlacer.addin` | `C:\ProgramData\Autodesk\Revit\Addins\2024\` (one level up) |
| `FamilyPlacer32.png` | Same folder as DLL (optional ribbon icon) |
