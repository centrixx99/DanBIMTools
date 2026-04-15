using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;

namespace DanBIMTools.Commands.HVAC
{
    [Transaction(TransactionMode.Manual)]
    public class InsulationValidatorCommand : IExternalCommand
    {
        // BR18 insulation requirements for Denmark
        private readonly Dictionary<string, double> _minInsulationThickness = new()
        {
            ["Outdoor_Duct"] = 50.0,      // mm for outdoor ducts
            ["Cold_Duct_Indoor"] = 30.0,   // mm for cold supply ducts indoor
            ["Warm_Duct_Indoor"] = 25.0,   // mm for return/warm ducts indoor
            ["Exhaust_Duct"] = 20.0       // mm for exhaust ducts
        };

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Get all ducts in model
                var ducts = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_DuctCurves)
                    .OfClass(typeof(Duct))
                    .Cast<Duct>()
                    .ToList();

                if (ducts.Count == 0)
                {
                    TaskDialog.Show("Isoleringskontrol", "Ingen kanaler fundet i modellen.");
                    return Result.Cancelled;
                }

                List<ValidationResult> results = new List<ValidationResult>();

                foreach (Duct duct in ducts)
                {
                    ValidationResult result = ValidateDuctInsulation(duct);
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }

                // Show summary
                int passCount = results.Count(r => r.Status == "OK");
                int failCount = results.Count(r => r.Status == "FAIL");
                int warnCount = results.Count(r => r.Status == "WARNING");
                int noInsulationCount = results.Count(r => r.Status == "NO_INSULATION");

                string summary = $"Kontrolleret {ducts.Count} kanaler:\n\n" +
                    $"  ✅ OK: {passCount}\n" +
                    $"  ❌ For tynd isolering: {failCount}\n" +
                    $"  ⚠️  Advarsel: {warnCount}\n" +
                    $"  ⚪ Mangler isolering: {noInsulationCount}\n\n" +
                    $"Standard: BR18 Bygningsreglementet";

                // Show detailed results for failures
                if (failCount > 0 || noInsulationCount > 0)
                {
                    var issues = results
                        .Where(r => r.Status == "FAIL" || r.Status == "NO_INSULATION")
                        .Take(10);

                    string details = string.Join("\n", issues.Select(r =>
                        $"  {r.ElementName}: {r.Message}"));

                    summary += $"\n\nProblemer (viser første 10):\n{details}";

                    if (failCount + noInsulationCount > 10)
                    {
                        summary += $"\n  ... og {failCount + noInsulationCount - 10} mere";
                    }
                }

                TaskDialog.Show("Isoleringskontrol", summary);

                // Select failing elements
                var failingIds = results
                    .Where(r => r.Status == "FAIL" || r.Status == "NO_INSULATION")
                    .Select(r => r.ElementId)
                    .ToList();

                if (failingIds.Any())
                {
                    uidoc.Selection.SetElementIds(failingIds);
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Fejl", $"Isoleringskontrol fejlede:\n{ex.Message}");
                return Result.Failed;
            }
        }

        private ValidationResult ValidateDuctInsulation(Duct duct)
        {
            // Determine duct type
            string ductType = ClassifyDuctType(duct);
            double requiredThickness = GetRequiredThickness(ductType);

            // Check for insulation
            double? actualThickness = GetInsulationThickness(duct);

            if (actualThickness == null)
            {
                return new ValidationResult
                {
                    ElementId = duct.Id,
                    ElementName = duct.Name,
                    Status = "NO_INSULATION",
                    Message = "Mangler isolering",
                    RequiredThickness = requiredThickness,
                    ActualThickness = 0
                };
            }

            if (actualThickness < requiredThickness)
            {
                return new ValidationResult
                {
                    ElementId = duct.Id,
                    ElementName = duct.Name,
                    Status = "FAIL",
                    Message = $"For tynd: {actualThickness:F0}mm (kræver {requiredThickness:F0}mm)",
                    RequiredThickness = requiredThickness,
                    ActualThickness = actualThickness.Value
                };
            }

            return new ValidationResult
            {
                ElementId = duct.Id,
                ElementName = duct.Name,
                Status = "OK",
                Message = $"OK: {actualThickness:F0}mm isolering",
                RequiredThickness = requiredThickness,
                ActualThickness = actualThickness.Value
            };
        }

        private string ClassifyDuctType(Duct duct)
        {
            // Check duct properties to classify
            Parameter systemTypeParam = duct.LookupParameter("System Type");
            if (systemTypeParam != null)
            {
                string systemType = systemTypeParam.AsString() ?? "";
                if (systemType.IndexOf("Supply", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Cold_Duct_Indoor";
                if (systemType.IndexOf("Return", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Warm_Duct_Indoor";
                if (systemType.IndexOf("Exhaust", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Exhaust_Duct";
            }

            // Check if outdoor
            BoundingBoxXYZ? bbox = duct.get_BoundingBox(null);
            if (bbox != null)
            {
                // Simple check - if at high Z, might be outdoor
                // In real implementation, check against building envelope
            }

            // Default
            return "Warm_Duct_Indoor";
        }

        private double GetRequiredThickness(string ductType)
        {
            if (_minInsulationThickness.TryGetValue(ductType, out double thickness))
                return thickness;
            return 25.0; // Default
        }

        private double? GetInsulationThickness(Duct duct)
        {
            // Try various parameter names
            string[] paramNames = { "Insulation Thickness", "InsulationThickness", "Isolering", "Ins Thk" };

            foreach (string paramName in paramNames)
            {
                Parameter param = duct.LookupParameter(paramName);
                if (param != null && param.HasValue)
                {
                    double value = param.AsDouble();
                    // Convert from feet to mm
                    return value * 304.8;
                }
            }

            // Check insulation element attached to duct
            var insulationElements = GetInsulationElements(duct);
            if (insulationElements.Any())
            {
                // Get thickness from first insulation element
                var first = insulationElements.First();
                Parameter thkParam = first.LookupParameter("Thickness");
                if (thkParam != null && thkParam.HasValue)
                {
                    return thkParam.AsDouble() * 304.8; // feet to mm
                }
            }

            return null;
        }

        private IEnumerable<Element> GetInsulationElements(Duct duct)
        {
            // Get duct insulation (if applied as separate element)
            var doc = duct.Document;
            var collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_DuctInsulations);

            // Filter by host
            return collector.Cast<Element>()
                .Where(e => e.LookupParameter("Host")?.AsElementId() == duct.Id);
        }
    }

    public class ValidationResult
    {
        public ElementId ElementId { get; set; }
        public string ElementName { get; set; } = "";
        public string Status { get; set; } = "";
        public string Message { get; set; } = "";
        public double RequiredThickness { get; set; }
        public double ActualThickness { get; set; }
    }
}
