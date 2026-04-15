using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace DanBIMTools.Commands.General
{
    [Transaction(TransactionMode.Manual)]
    public class QuickCheckCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Quick health check of the model
                QuickCheckReport report = RunQuickCheck(doc);
                
                // Display results
                string summary = $"DanBIM Quick Check\n\n" +
                    $"📊 Modeloverblik:\n" +
                    $"  Elementer: {report.TotalElements}\n" +
                    $"  Vægge: {report.WallCount}\n" +
                    $"  Etagedæk: {report.FloorCount}\n" +
                    $"  Døre: {report.DoorCount}\n" +
                    $"  Vinduer: {report.WindowCount}\n" +
                    $"  Kanaler: {report.DuctCount}\n\n";

                // Issues
                summary += $"⚠️  Problemer:\n" +
                    $"  Elementer uden BIM7AA: {report.WithoutBIM7AA}\n" +
                    $"  Elementer uden materiale: {report.WithoutMaterial}\n" +
                    $"  Duplikerede ID'er: {report.DuplicateIds}\n\n";

                // Recommendations
                summary += $"💡 Anbefalinger:\n";
                if (report.WithoutBIM7AA > report.TotalElements * 0.1)
                    summary += "  • Mange elementer mangler BIM7AA - brug Auto Classify\n";
                if (report.WithoutMaterial > 0)
                    summary += "  • Manglende materialer - tjek familier\n";
                if (report.DuplicateIds > 0)
                    summary += "  • Duplikerede ID'er - kontakt BIM koordinator\n";

                if (report.WithoutBIM7AA == 0 && report.WithoutMaterial == 0 && report.DuplicateIds == 0)
                    summary += "  ✅ Model ser god ud!\n";

                TaskDialog.Show("Quick Check", summary);

                // Select problematic elements
                if (report.ProblematicElementIds.Any())
                {
                    uidoc.Selection.SetElementIds(report.ProblematicElementIds);
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Fejl", $"Quick check fejlede:\n{ex.Message}");
                return Result.Failed;
            }
        }

        private QuickCheckReport RunQuickCheck(Document doc)
        {
            var report = new QuickCheckReport();

            // Get counts
            var allElements = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .Where(e => e.Category != null)
                .ToList();

            report.TotalElements = allElements.Count;
            report.WallCount = allElements.Count(e => e.Category.Name == "Walls");
            report.FloorCount = allElements.Count(e => e.Category.Name == "Floors");
            report.DoorCount = allElements.Count(e => e.Category.Name == "Doors");
            report.WindowCount = allElements.Count(e => e.Category.Name == "Windows");
            report.DuctCount = allElements.Count(e => e.Category.Name == "Ducts");

            // Check for missing data
            foreach (Element elem in allElements)
            {
                if (!ShouldCheckElement(elem)) continue;

                // Check BIM7AA
                if (!HasParameterValue(elem, "BIM7AA_TypeCode") && 
                    !HasParameterValue(elem, "BIM7AA") &&
                    !HasParameterValue(elem, "Type Code"))
                {
                    report.WithoutBIM7AA++;
                    report.ProblematicElementIds.Add(elem.Id);
                }

                // Check material
                if (!HasParameterValue(elem, "Material"))
                {
                    report.WithoutMaterial++;
                }
            }

            // Check for duplicate ElementIds (shouldn't happen but good to check)
            var idCounts = allElements.GroupBy(e => e.Id.Value)
                .Where(g => g.Count() > 1)
                .Count();
            report.DuplicateIds = idCounts;

            return report;
        }

        private bool ShouldCheckElement(Element elem)
        {
            var checkCategories = new[] { "Walls", "Floors", "Doors", "Windows", "Roofs", 
                "Structural Columns", "Structural Framing", "Ducts", "Pipes" };
            return checkCategories.Contains(elem.Category?.Name);
        }

        private bool HasParameterValue(Element elem, string paramName)
        {
            Parameter param = elem.LookupParameter(paramName);
            if (param == null && elem is FamilyInstance fi)
            {
                param = fi.Symbol?.LookupParameter(paramName);
            }

            if (param == null) return false;
            if (!param.HasValue) return false;

            if (param.StorageType == StorageType.String)
            {
                return !string.IsNullOrWhiteSpace(param.AsString());
            }

            return true;
        }
    }

    public class QuickCheckReport
    {
        public int TotalElements { get; set; }
        public int WallCount { get; set; }
        public int FloorCount { get; set; }
        public int DoorCount { get; set; }
        public int WindowCount { get; set; }
        public int DuctCount { get; set; }
        public int WithoutBIM7AA { get; set; }
        public int WithoutMaterial { get; set; }
        public int DuplicateIds { get; set; }
        public List<ElementId> ProblematicElementIds { get; set; } = new List<ElementId>();
    }
}
