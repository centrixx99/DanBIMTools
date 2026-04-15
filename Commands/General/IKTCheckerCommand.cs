using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace DanBIMTools.Commands.General
{
    [Transaction(TransactionMode.Manual)]
    public class IKTCheckerCommand : IExternalCommand
    {
        // IKT requirements per Danish building regulations
        private readonly List<IKTRequirement> _requirements = new()
        {
            new() { Category = "Alle elementer", Parameter = "GlobalId", Required = true, Description = "Unikt IFC ID" },
            new() { Category = "Alle elementer", Parameter = "Name", Required = true, Description = "Navn på element" },
            new() { Category = "Konstruktion", Parameter = "Structural", Required = true, Description = "Bærende markering" },
            new() { Category = "Vægge", Parameter = "Area", Required = true, Description = "Areal" },
            new() { Category = "Døre/Vinduer", Parameter = "Width", Required = true, Description = "Bredde" },
            new() { Category = "Døre/Vinduer", Parameter = "Height", Required = true, Description = "Højde" },
            new() { Category = "MEP", Parameter = "System Type", Required = true, Description = "Systemtype" }
        };

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Check all elements against IKT requirements
                var allElements = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .Where(e => e.Category != null && !IsExclusionCategory(e.Category.Name))
                    .ToList();

                List<IKTCheckResult> results = new List<IKTCheckResult>();

                foreach (Element elem in allElements)
                {
                    var elementResults = CheckIKTRequirements(elem);
                    results.AddRange(elementResults);
                }

                // Generate summary
                int totalChecks = results.Count;
                int passed = results.Count(r => r.Status == "PASS");
                int failed = results.Count(r => r.Status == "FAIL");
                int warnings = results.Count(r => r.Status == "WARNING");

                var failuresByCategory = results
                    .Where(r => r.Status == "FAIL")
                    .GroupBy(r => r.Category)
                    .OrderByDescending(g => g.Count())
                    .Take(5);

                string summary = $"IKT Kontrol - Bygningsreglementet\n\n" +
                    $"Kontrolleret {allElements.Count} elementer\n" +
                    $"{totalChecks} krav tjekket:\n\n" +
                    $"  ✅ OK: {passed}\n" +
                    $"  ❌ Mangler: {failed}\n" +
                    $"  ⚠️  Advarsel: {warnings}\n\n" +
                    $"Overholdelse: {(passed * 100.0 / totalChecks):F1}%";

                if (failuresByCategory.Any())
                {
                    summary += "\n\nTop mangler:\n" +
                        string.Join("\n", failuresByCategory.Select(g => 
                            $"  {g.Key}: {g.Count()} elementer"));
                }

                TaskDialog.Show("IKT Kontrol", summary);

                // Select elements with failures
                var failedElementIds = results
                    .Where(r => r.Status == "FAIL")
                    .Select(r => r.ElementId)
                    .Distinct()
                    .ToList();

                if (failedElementIds.Any())
                {
                    uidoc.Selection.SetElementIds(failedElementIds);
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Fejl", $"IKT kontrol fejlede:\n{ex.Message}");
                return Result.Failed;
            }
        }

        private bool IsExclusionCategory(string categoryName)
        {
            var exclusions = new[] { 
                "Lines", "Dimensions", "Text Notes", "Reference Planes",
                "Cameras", "Scope Boxes", "Section Boxes", "Annotations", 
                "Raster Images", "Imports in Families" 
            };
            return exclusions.Contains(categoryName, StringComparer.OrdinalIgnoreCase);
        }

        private List<IKTCheckResult> CheckIKTRequirements(Element elem)
        {
            List<IKTCheckResult> results = new List<IKTCheckResult>();
            string category = elem.Category?.Name ?? "Ukendt";

            // Check applicable requirements
            var applicable = _requirements.Where(r => 
                r.Category == "Alle elementer" || 
                category.IndexOf(r.Category, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var req in applicable)
            {
                bool hasParam = CheckParameterExists(elem, req.Parameter);
                
                results.Add(new IKTCheckResult
                {
                    ElementId = elem.Id,
                    ElementName = elem.Name,
                    Category = category,
                    Requirement = req.Description,
                    Parameter = req.Parameter,
                    Status = hasParam ? "PASS" : (req.Required ? "FAIL" : "WARNING"),
                    Message = hasParam ? "OK" : $"Mangler: {req.Parameter}"
                });
            }

            return results;
        }

        private bool CheckParameterExists(Element elem, string paramName)
        {
            // Check built-in parameters
            Parameter param = elem.LookupParameter(paramName);
            if (param != null && param.HasValue) return true;

            // Check type parameters for family instances
            if (elem is FamilyInstance fi)
            {
                param = fi.Symbol?.LookupParameter(paramName);
                if (param != null && param.HasValue) return true;
            }

            // Check common alternatives
            var alternatives = new Dictionary<string, string[]>
            {
                ["Width"] = new[] { "Width", "Bredde", "Rough Width" },
                ["Height"] = new[] { "Height", "Højde", "Rough Height" },
                ["Area"] = new[] { "Area", "Areal", "Area (Gross)" },
                ["System Type"] = new[] { "System Type", "SystemType", "System Classification" }
            };

            if (alternatives.TryGetValue(paramName, out string[] alts))
            {
                foreach (string alt in alts)
                {
                    param = elem.LookupParameter(alt);
                    if (param != null && param.HasValue) return true;
                }
            }

            return false;
        }
    }

    public class IKTRequirement
    {
        public string Category { get; set; } = "";
        public string Parameter { get; set; } = "";
        public bool Required { get; set; }
        public string Description { get; set; } = "";
    }

    public class IKTCheckResult
    {
        public ElementId ElementId { get; set; }
        public string ElementName { get; set; } = "";
        public string Category { get; set; } = "";
        public string Requirement { get; set; } = "";
        public string Parameter { get; set; } = "";
        public string Status { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
