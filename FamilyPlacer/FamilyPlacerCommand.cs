using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

// Explicit aliases to prevent WinForms vs Revit DB collisions
using WinForm = System.Windows.Forms.Form;
using WinLabel = System.Windows.Forms.Label;
using WinCombo = System.Windows.Forms.ComboBox;
using WinButton = System.Windows.Forms.Button;
using WinTextBox = System.Windows.Forms.TextBox;
using WinControl = System.Windows.Forms.Control;
using ComboStyle = System.Windows.Forms.ComboBoxStyle;
using WinFlatStyle = System.Windows.Forms.FlatStyle;
using WinBorderStyle = System.Windows.Forms.BorderStyle;
using FormPos = System.Windows.Forms.FormStartPosition;
using FormBorder = System.Windows.Forms.FormBorderStyle;
using DialogRes = System.Windows.Forms.DialogResult;
using RadioButton = System.Windows.Forms.RadioButton;
using WinPanel = System.Windows.Forms.Panel;
using DrawingColor = System.Drawing.Color;

namespace FamilyPlacer
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class FamilyPlacerCommand : IExternalCommand
    {
        private Document _doc;
        private UIDocument _uiDoc;
        private string _unit;

        public Result Execute(ExternalCommandData cData, ref string msg, ElementSet els)
        {
            _uiDoc = cData.Application.ActiveUIDocument;
            _doc = _uiDoc.Document;

            _unit = UnitHelper.GetUnitSuffix(_doc);

            try
            {
                List<XYZ> placementPoints = new List<XYZ>();
                bool pointsLoadedFromCsv = false;

                // --- STEP 1: ATTEMPT CSV IMPORT ---
                TaskDialogResult csvChoice = TaskDialog.Show("Data Source Selection",
                    "Would you like to import coordinates from a CSV file?\n\n" +
                    "• YES - Select CSV file\n" +
                    "• NO - Fallback to original Automatic Space Boundary Grid placement",
                    TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

                if (csvChoice == TaskDialogResult.Yes)
                {
                    using (OpenFileDialog openFileDialog = new OpenFileDialog())
                    {
                        // FIXED: Added missing pattern filter pair to eliminate System.ArgumentException
                        openFileDialog.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                        openFileDialog.Title = "Select CSV File containing X, Y, Z Points";

                        if (openFileDialog.ShowDialog() == DialogRes.OK)
                        {
                            placementPoints = ParseCsvPoints(openFileDialog.FileName, out string parseError);
                            if (placementPoints != null && placementPoints.Count > 0)
                            {
                                pointsLoadedFromCsv = true;
                                TaskDialog.Show("CSV Loaded", $"Successfully imported {placementPoints.Count} points from file.");
                            }
                            else
                            {
                                TaskDialog.Show("CSV Parsing Failed", $"Reason: {parseError}\n\nFalling back to original Auto-Grid engine...");
                            }
                        }
                        else
                        {
                            TaskDialog.Show("Import Canceled", "No file chosen. Falling back to original Auto-Grid engine...");
                        }
                    }
                }

                // --- STEP 2: PICK LEVEL & PLANE CONTEXT ---
                var levels = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation)
                    .ToList();

                Level selectedLevel = ShowLevelPicker(levels);
                if (selectedLevel == null) return Result.Cancelled;

                Reference pickedPlaneRef = null;
                Element planeElement = null;
                Plane computedPlane = null;
                double baselineZ = selectedLevel.Elevation;

                Autodesk.Revit.DB.View activeView = _doc.ActiveView;
                if (activeView.ViewType != ViewType.Section && activeView.ViewType != ViewType.Elevation)
                {
                    TaskDialog.Show("View Requirement", "Please open or switch to a Section or Elevation view to pick your reference baseline, then click OK.");
                }

                try
                {
                    pickedPlaneRef = _uiDoc.Selection.PickObject(ObjectType.Element, "Select a Reference Plane or Level Plane for Z-axis height orientation alignment.");
                    planeElement = _doc.GetElement(pickedPlaneRef);
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    TaskDialog.Show("Fallback Notice", "Plane picking canceled. Using default selected Level elevation as baseline Z.");
                }

                if (planeElement is ReferencePlane refPlane)
                {
                    computedPlane = refPlane.GetPlane();
                    baselineZ = computedPlane.Origin.Z;
                }
                else if (planeElement is Level levelPlane)
                {
                    baselineZ = levelPlane.Elevation;
                }

                // --- STEP 3: FALLBACK TO AUTOMATIC GRID IF NO CSV IS LOADED ---
                if (!pointsLoadedFromCsv)
                {
                    var allSpaces = new FilteredElementCollector(_doc)
                        .OfClass(typeof(SpatialElement))
                        .Cast<SpatialElement>()
                        .OfType<Space>()
                        .Where(s => s.Area > 0)
                        .OrderBy(s => s.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString())
                        .ToList();

                    if (allSpaces.Count == 0)
                    {
                        TaskDialog.Show("Execution Ended", "No valid MEP Spaces found for automatic grid layout calculations.");
                        return Result.Failed;
                    }

                    var levelSpaces = allSpaces.Where(s => s.Level != null && s.Level.Id == selectedLevel.Id).ToList();
                    Space selectedSpace = ShowSpacePicker(levelSpaces.Count > 0 ? levelSpaces : allSpaces);
                    if (selectedSpace == null) return Result.Cancelled;

                    XYZ origin = new XYZ(0, 0, selectedLevel.Elevation);
                    TaskDialogResult originMethod = TaskDialog.Show("Origin Setup", "Set grid calculation origin?", TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);
                    if (originMethod == TaskDialogResult.Yes)
                    {
                        try { origin = _uiDoc.Selection.PickPoint("Pick space grid layout origin point"); }
                        catch { origin = GetSpaceCentroid(selectedSpace, selectedLevel); }
                    }
                    else
                    {
                        origin = GetSpaceCentroid(selectedSpace, selectedLevel);
                    }

                    double offsetDisplay = ShowNumberInput("Offset", "Enter inward boundary offset (" + _unit + "):", 300);
                    double offsetInternal = UnitHelper.ToInternal(offsetDisplay, _doc);

                    GetSpaceBBox(selectedSpace, out double lx, out double ly, out double minX, out double minY, out double maxX, out double maxY);
                    double spanX = Math.Max(0, lx - 2 * offsetInternal);
                    double spanY = Math.Max(0, ly - 2 * offsetInternal);

                    double gridXDisplay = ShowNumberInput("X Interval", "Enter X grid intervals:", 1500);
                    double gridYDisplay = ShowNumberInput("Y Interval", "Enter Y grid intervals:", 1500);
                    double gridXInt = UnitHelper.ToInternal(gridXDisplay, _doc);
                    double gridYInt = UnitHelper.ToInternal(gridYDisplay, _doc);

                    int divX = ComputeDivisions(spanX, gridXInt);
                    int divY = ComputeDivisions(spanY, gridYInt);
                    double adjX = divX > 0 ? spanX / divX : gridXInt;
                    double adjY = divY > 0 ? spanY / divY : gridYInt;

                    double startX = minX + offsetInternal;
                    double startY = minY + offsetInternal;
                    double endX = maxX - offsetInternal;
                    double endY = maxY - offsetInternal;

                    for (int ix = 0; ix <= divX; ix++)
                    {
                        double x = origin.X + ix * adjX;
                        if (x < startX || x > endX) continue;
                        for (int iy = 0; iy <= divY; iy++)
                        {
                            double y = origin.Y + iy * adjY;
                            if (y < startY || y > endY) continue;

                            XYZ testPt = new XYZ(x, y, selectedLevel.Elevation + 0.1);
                            if (selectedSpace.IsPointInSpace(testPt))
                            {
                                placementPoints.Add(new XYZ(x, y, baselineZ));
                            }
                        }
                    }
                }

                if (placementPoints.Count == 0)
                {
                    TaskDialog.Show("Execution Error", "No points generated or found. Process terminated.");
                    return Result.Cancelled;
                }

                // --- STEP 4: PICK FAMILY SYMBOL ---
                FamilySymbol selectedSymbol = ShowFamilyPicker();
                if (selectedSymbol == null) return Result.Cancelled;

                bool isFaceBased = IsFaceBasedFamily(selectedSymbol.Family);
                bool isCeilingBased = IsCeilingBasedFamily(selectedSymbol.Family);

                // --- STEP 5: INSTANTIATE ELEMENTS ---
                int placedCount = 0;
                int failedCount = 0;
                List<string> errorLogs = new List<string>();

                using (Transaction tx = new Transaction(_doc, "Multi-Source Family Insertion Script"))
                {
                    tx.Start();
                    if (!selectedSymbol.IsActive) selectedSymbol.Activate();

                    foreach (XYZ pt in placementPoints)
                    {
                        try
                        {
                            FamilyInstance inst = null;
                            XYZ finalTargetPoint = new XYZ(pt.X, pt.Y, baselineZ);

                            if (isCeilingBased)
                            {
                                Face ceilingFace = FindCeilingFaceAtPoint(finalTargetPoint, out Element ceilingElem);
                                if (ceilingFace != null)
                                {
                                    inst = _doc.Create.NewFamilyInstance(ceilingFace, finalTargetPoint, XYZ.BasisX, selectedSymbol);
                                }
                                else
                                {
                                    failedCount++;
                                    errorLogs.Add($"Point ({pt.X:F1}, {pt.Y:F1}): Missing intersecting ceiling element directly above.");
                                    continue;
                                }
                            }
                            else if (isFaceBased && planeElement is ReferencePlane)
                            {
                                SketchPlane skPlane = SketchPlane.Create(_doc, computedPlane);
                                inst = _doc.Create.NewFamilyInstance(finalTargetPoint, selectedSymbol, skPlane, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                            }
                            else
                            {
                                inst = _doc.Create.NewFamilyInstance(finalTargetPoint, selectedSymbol, selectedLevel, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                            }

                            if (inst != null) placedCount++;
                        }
                        catch (Exception innerEx)
                        {
                            failedCount++;
                            errorLogs.Add(innerEx.Message);
                        }
                    }
                    tx.Commit();
                }

                string modeLabel = pointsLoadedFromCsv ? "CSV Coordinate File Data" : "Automated Space Grid Array Engine";
                string finalMsg = $"Process Completed via [{modeLabel}]\n\n• Successfully Placed: {placedCount}\n• Failures Encountered: {failedCount}";
                if (failedCount > 0) finalMsg += "\n\nSample Fault Reasons:\n" + string.Join("\n", errorLogs.Distinct().Take(5));

                TaskDialog.Show("Process Log Summary", finalMsg);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                msg = ex.ToString();
                return Result.Failed;
            }
        }

        // --- EXTRACTED UTILITY HELPERS ---

        private List<XYZ> ParseCsvPoints(string path, out string error)
        {
            error = "None";
            List<XYZ> points = new List<XYZ>();
            try
            {
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.ToLower().Contains("x")) continue;
                    string[] parts = line.Split(',');
                    if (parts.Length >= 3 &&
                        double.TryParse(parts[0].Trim(), out double x) &&
                        double.TryParse(parts[1].Trim(), out double y) &&
                        double.TryParse(parts[2].Trim(), out double z))
                    {
                        points.Add(new XYZ(x, y, z));
                    }
                }
            }
            catch (Exception ex) { error = ex.Message; return null; }
            return points;
        }

        private Face FindCeilingFaceAtPoint(XYZ originPt, out Element ceilingElement)
        {
            ceilingElement = null;
            FilteredElementCollector viewColl = new FilteredElementCollector(_doc).OfClass(typeof(View3D));
            View3D view3D = viewColl.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate);
            if (view3D == null) return null;

            ReferenceIntersector intersector = new ReferenceIntersector(new ElementClassFilter(typeof(Ceiling)), FindReferenceTarget.Element, view3D);
            ReferenceWithContext context = intersector.FindNearest(originPt, XYZ.BasisZ);
            if (context != null)
            {
                Reference r = context.GetReference();
                ceilingElement = _doc.GetElement(r.ElementId);
                return ceilingElement?.GetGeometryObjectFromReference(r) as Face;
            }
            return null;
        }

        private bool IsFaceBasedFamily(Family family)
        {
            Parameter p = family.get_Parameter(BuiltInParameter.FAMILY_HOSTING_BEHAVIOR);
            return p != null && p.AsInteger() == 5;
        }

        private bool IsCeilingBasedFamily(Family family)
        {
            Parameter p = family.get_Parameter(BuiltInParameter.FAMILY_HOSTING_BEHAVIOR);
            return p != null && p.AsInteger() == 3;
        }

        private Level ShowLevelPicker(List<Level> levels)
        {
            using (WinForm form = new WinForm())
            {
                form.Text = "Pick Baseline Level Context";
                form.Size = new Size(350, 200);
                form.StartPosition = FormPos.CenterScreen;

                WinCombo cb = new WinCombo();
                cb.Left = 20;
                cb.Top = 30;
                cb.Width = 280;
                cb.DropDownStyle = ComboStyle.DropDownList;

                foreach (Level l in levels)
                {
                    cb.Items.Add(l.Name);
                }

                if (cb.Items.Count > 0) cb.SelectedIndex = 0;

                WinButton btn = new WinButton();
                btn.Text = "OK";
                btn.Left = 200;
                btn.Top = 90;
                btn.DialogResult = DialogRes.OK;

                form.Controls.Add(cb);
                form.Controls.Add(btn);

                if (form.ShowDialog() == DialogRes.OK)
                {
                    return levels[cb.SelectedIndex];
                }
                return null;
            }
        }

        private Space ShowSpacePicker(List<Space> spaces)
        {
            using (WinForm form = new WinForm())
            {
                form.Text = "Pick Structural Space Target";
                form.Size = new Size(350, 200);
                form.StartPosition = FormPos.CenterScreen;

                WinCombo cb = new WinCombo();
                cb.Left = 20;
                cb.Top = 30;
                cb.Width = 280;
                cb.DropDownStyle = ComboStyle.DropDownList;

                foreach (Space s in spaces)
                {
                    cb.Items.Add(s.Name ?? s.Number);
                }

                if (cb.Items.Count > 0) cb.SelectedIndex = 0;

                WinButton btn = new WinButton();
                btn.Text = "OK";
                btn.Left = 200;
                btn.Top = 90;
                btn.DialogResult = DialogRes.OK;

                form.Controls.Add(cb);
                form.Controls.Add(btn);

                if (form.ShowDialog() == DialogRes.OK)
                {
                    return spaces[cb.SelectedIndex];
                }
                return null;
            }
        }

        private FamilySymbol ShowFamilyPicker()
        {
            var symbols = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .OrderBy(s => s.Family.Name)
                .ToList();

            using (WinForm form = new WinForm())
            {
                form.Text = "Select Family Type Symbol";
                form.Size = new Size(400, 220);
                form.StartPosition = FormPos.CenterScreen;

                WinCombo cb = new WinCombo();
                cb.Left = 20;
                cb.Top = 30;
                cb.Width = 340;
                cb.DropDownStyle = ComboStyle.DropDownList;

                foreach (FamilySymbol s in symbols)
                {
                    cb.Items.Add($"{s.Family.Name} : {s.Name}");
                }

                if (cb.Items.Count > 0) cb.SelectedIndex = 0;

                WinButton btn = new WinButton();
                btn.Text = "OK";
                btn.Left = 260;
                btn.Top = 100;
                btn.DialogResult = DialogRes.OK;

                form.Controls.Add(cb);
                form.Controls.Add(btn);

                if (form.ShowDialog() == DialogRes.OK)
                {
                    return symbols[cb.SelectedIndex];
                }
                return null;
            }
        }

        private double ShowNumberInput(string title, string instruction, double defaultVal)
        {
            using (WinForm form = new WinForm())
            {
                form.Text = title;
                form.Size = new Size(350, 180);
                form.StartPosition = FormPos.CenterScreen;

                WinLabel lbl = new WinLabel();
                lbl.Text = instruction;
                lbl.Left = 20;
                lbl.Top = 20;
                lbl.Width = 280;

                WinTextBox txt = new WinTextBox();
                txt.Text = defaultVal.ToString();
                txt.Left = 20;
                txt.Top = 50;
                txt.Width = 280;

                WinButton btn = new WinButton();
                btn.Text = "Apply";
                btn.Left = 220;
                btn.Top = 90;
                btn.DialogResult = DialogRes.OK;

                form.Controls.Add(lbl);
                form.Controls.Add(txt);
                form.Controls.Add(btn);

                if (form.ShowDialog() == DialogRes.OK)
                {
                    if (double.TryParse(txt.Text, out double output))
                    {
                        return output;
                    }
                }
                return defaultVal;
            }
        }

        private XYZ GetSpaceCentroid(Space space, Level lvl)
        {
            BoundingBoxXYZ bbox = space.get_BoundingBox(null);
            if (bbox == null) return new XYZ(0, 0, lvl.Elevation);
            return (bbox.Min + bbox.Max) * 0.5;
        }

        private void GetSpaceBBox(Space space, out double lx, out double ly, out double minX, out double minY, out double maxX, out double maxY)
        {
            BoundingBoxXYZ bbox = space.get_BoundingBox(null);
            minX = bbox.Min.X;
            minY = bbox.Min.Y;
            maxX = bbox.Max.X;
            maxY = bbox.Max.Y;
            lx = maxX - minX;
            ly = maxY - minY;
        }

        private int ComputeDivisions(double span, double interval)
        {
            if (span <= 0 || interval <= 0) return 0;
            return (int)Math.Floor(span / interval);
        }
    }
}