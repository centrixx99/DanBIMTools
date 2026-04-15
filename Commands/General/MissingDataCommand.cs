using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace DanBIMTools.Commands.General
{
    [Transaction(TransactionMode.Manual)]
    public class MissingDataCommand : IExternalCommand
    {
        // Critical parameters that should be filled
        private readonly List<ParameterRequirement> _criticalParams = new()
        {
            new() { Name = "BIM7AA_TypeCode", AppliesTo = new[] { "Walls", "Floors", "Doors", "Windows", "Roofs" }, Severity = "High" },
            new() { Name = "Fire Rating", AppliesTo = new[] { "Walls", "Doors", "Floors" }, Severity = "Critical" },
            new() { Name = "Material", AppliesTo = new[] { "Walls", "Floors", "Roofs", "Doors", "Windows" }, Severity = "Medium" },
            new() { Name = "Structural", AppliesTo = new[] { "Walls", "Floors", "Columns", "Framing" }, Severity = "Critical" },
            new() { Name = "Assembly Code", AppliesTo = new[] { "All" }, Severity = "Medium" },
            new() { Name = "Comments", AppliesTo = new[] { "All" }, Severity = "Low" }
        };

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Get all elements with categories
                var allElements = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .Where(e => e.Category != null && !IsExclusionCategory(e.Category.Name))
                    .ToList();

                List<MissingDataResult> results = new List<MissingDataResult>();

                foreach (Element elem in allElements)
                {
                    var missingParams = CheckMissingParameters(elem);
                    if (missingParams.Any())
                    {
                        results.AddRange(missingParams);
                    }
                }

                // Group and summarize
                var bySeverity = results.GroupBy(r => r.Severity)
                    .OrderByDescending(g => g.Key switch { "Critical" => 4, "High" => 3, "Medium" => 2, _ => 1 });

                string summary = $"Manglende Data Rapport\n\n" +
                    $"Kontrolleret {allElements.Count} elementer\n" +
                    $"Fundet {results.Count} manglende værdier:\n\n";

                foreach (var group in bySeverity)
                {
                    string icon = group.Key switch
                    {
                        "Critical" => "🔴",
                        "High" => "🟠",
                        "Medium" => "🟡",
                        _ => "⚪"
                    };
                    summary += $"  {icon} {group.Key}: {group.Count()}\n";
                }

                // Show specific missing data by category
                var byCategory = results
                    .GroupBy(r => r.Category)
                    .OrderByDescending(g => g.Count())
                    .Take(10);

                if (byCategory.Any())
                {
                    summary += "\nKategorier med flest mangler:\n" +
                        string.Join("\n", byCategory.Select(g => 
                            $"  {g.Key}: {g.Count()}"));
                }

                TaskDialog.Show("Manglende Data", summary);

                // Select elements with critical or high severity issues
                var criticalIds = results
                    .Where(r => r.Severity == "Critical" || r.Severity == "High")
                    .Select(r => r.ElementId)
                    .Distinct()
                    .ToList();

                if (criticalIds.Any())
                {
                    uidoc.Selection.SetElementIds(criticalIds);
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Fejl", $"Manglende data check fejlede:\n{ex.Message}");
                return Result.Failed;
            }
        }

        private bool IsExclusionCategory(string categoryName)
        {
            var exclusions = new[] { 
                "Lines", "Dimensions", "Text Notes", "Reference Planes",
                "Cameras", "Scope Boxes", "Section Boxes", "Annotations", 
                "Raster Images", "Imports in Families", "Views", "Sheets" 
            };
            return exclusions.Contains(categoryName, StringComparer.OrdinalIgnoreCase);
        }

        private List<MissingDataResult> CheckMissingParameters(Element elem)
        {
            List<MissingDataResult> results = new List<MissingDataResult>();
            string category = elem.Category?.Name ?? "Ukendt";

            foreach (var req in _criticalParams)
            {
                // Check if this requirement applies to this category
                bool applies = req.AppliesTo.Contains("All") || 
                    req.AppliesTo.Any(a => category.IndexOf(a, StringComparison.OrdinalIgnoreCase) >= 0);

                if (!applies) continue;

                // Check if parameter exists and has value
                bool hasValue = CheckHasValue(elem, req.Name);

                if (!hasValue)
                {
                    results.Add(new MissingDataResult
                    {
                        ElementId = elem.Id,
                        ElementName = elem.Name,
                        Category = category,
                        ParameterName = req.Name,
                        Severity = req.Severity,
                        Message = $"Mangler: {req.Name}"
                    });
                }
            }

            return results;
        }

        private bool CheckHasValue(Element elem, string paramName)
        {
            Parameter param = elem.LookupParameter(paramName);
            if (param == null && elem is FamilyInstance fi)
            {
                param = fi.Symbol?.LookupParameter(paramName);
            }

            if (param == null) return false;
            if (!param.HasValue) return false;

            // Check for empty string
            if (param.StorageType == StorageType.String)
            {
                string value = param.AsString();
                return !string.IsNullOrWhiteSpace(value);
            }

            // Check for zero/null values
            switch (param.StorageType)
            {
                case StorageType.Integer:
                    return param.AsInteger() != 0 || paramName.Contains("Structural");
                case StorageType.Double:
                    return param.AsDouble() > 0;
                case StorageType.ElementId:
                    return param.AsElementId() != null && param.AsElementId().Value != -1;
                default:
                    return true;
            }
        }
    }

    public class ParameterRequirement
    {
        public string Name { get; set; } = "";
        public string[] AppliesTo { get; set; } = Array.Empty<string>();
        public string Severity { get; set; } = "Medium";
    }

    public class MissingDataResult
    {
        public ElementId ElementId { get; set; }
        public string ElementName { get; set; } = "";
        public string Category { get; set; } = "";
        public string ParameterName { get; set; } = "";
        public string Severity { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
