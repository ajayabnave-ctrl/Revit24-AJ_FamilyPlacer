using Autodesk.Revit.DB;

namespace FamilyPlacer
{
    public static class UnitHelper
    {
        public static string GetUnitSuffix(Document doc)
        {
            ForgeTypeId uid = doc.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId();
            if (uid == UnitTypeId.Millimeters) return "mm";
            if (uid == UnitTypeId.Centimeters) return "cm";
            if (uid == UnitTypeId.Meters)      return "m";
            if (uid == UnitTypeId.Feet)        return "ft";
            if (uid == UnitTypeId.Inches)      return "in";
            return "mm";
        }

        public static double ToInternal(double display, Document doc)
        {
            ForgeTypeId uid = doc.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId();
            return UnitUtils.ConvertToInternalUnits(display, uid);
        }

        public static double ToDisplay(double feet, Document doc)
        {
            ForgeTypeId uid = doc.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId();
            return UnitUtils.ConvertFromInternalUnits(feet, uid);
        }

        public static string Format(double feet, Document doc)
        {
            return ToDisplay(feet, doc).ToString("F0") + " " + GetUnitSuffix(doc);
        }
    }
}
