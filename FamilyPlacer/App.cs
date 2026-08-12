using Autodesk.Revit.UI;
using System;
using System.Reflection;

namespace FamilyPlacer
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            try
            {
                string tab = "MEP Tools";
                try { app.CreateRibbonTab(tab); } catch { }
                RibbonPanel panel = app.CreateRibbonPanel(tab, "Family Placement");
                string path = Assembly.GetExecutingAssembly().Location;
                panel.AddItem(new PushButtonData(
                    "FamilyPlacer", "Place\nFamilies", path,
                    "FamilyPlacer.FamilyPlacerCommand")
                {
                    ToolTip = "Grid-based MEP family placement inside Space boundaries"
                });
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("FamilyPlacer", ex.Message);
                return Result.Failed;
            }
        }
        public Result OnShutdown(UIControlledApplication app) => Result.Succeeded;
    }
}
