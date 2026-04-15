using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DanBIMTools.Core;

namespace DanBIMTools.Commands.BIM7AA
{
    [Transaction(TransactionMode.Manual)]
    public class ValidateCodesCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Load BIM7AA database
                BIM7AADatabase db = BIM7AADatabase.Load();

                // Get selected elements or all elements
                ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
                
                if (selectedIds.Count == 0)
                {
                    TaskDialogResult dialogResult = TaskDialog.Show("BIM7AA Validering",
                        "Ingen elementer valgt.\n\nVil du validere alle elementer i modellen?",
                        TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

                    if (dialogResult == TaskDialogResult.Yes)
                    {
                        // Get all model elements
                        selectedIds = new FilteredElementCollector(doc)
                            .WhereElementIsNotElementType()
                            .Where(e => e.Category != null)
                            .Select(e => e.Id)
                            .ToList();
                    }
                    else
                    {
                        return Result.Cancelled;
                    }
                }

                List<CodeValidationResult> results = new List<CodeValidationResult>();

                foreach (ElementId id in selectedIds)
                {
                    Element elem = doc.GetElement(id);
                    if (elem == null) continue;

                    CodeValidationResult result = ValidateElement(elem, db);
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }

                // Generate report
                int validCount = results.Count(r => r.Status == "VALID");
                int invalidCount = results.Count(r => r.Status == "INVALID");
                int missingCount = results.Count(r => r.Status == "MISSING");

                string summary = $"BIM7AA Valideringsrapport\n" +
                    $"Kontrolleret {results.Count} elementer:\n\n" +
                    $"  ✅ Gyldige koder: {validCount}\n" +
                    $"  ❌ Ugyldige koder: {invalidCount}\n" +
                    $"  ⚪ Mangler kode: {missingCount}\n\n" +
                    $"I alt: {validCount}/{results.Count} OK ({(validCount * 100.0 / results.Count):F1}%)";

                // Show invalid codes
                if (invalidCount > 0)
                {
                    var invalids = results.Where(r => r.Status == "INVALID").Take(10);
                    string invalidDetails = "\n\nUgyldige koder (første 10):\n" +
                        string.Join("\n", invalids.Select(r => 
                            $"  {r.ElementName}: '{r.CurrentCode}' er ikke en gyldig BIM7AA kode"));
                    
                    if (invalidCount > 10)
                    {
                        invalidDetails += $"\n  ... og {invalidCount - 10} mere";
                    }
                    summary += invalidDetails;
                }

                // Show missing
                if (missingCount > 0)
                {
                    var missing = results.Where(r => r.Status == "MISSING").Take(5);
                    string missingDetails = "\n\nMangler BIM7AA kode:\n" +
                        string.Join("\n", missing.Select(r => 
                            $"  {r.ElementName} ({r.Category})"));
                    
                    if (missingCount > 5)
                    {
                        missingDetails += $"\n  ... og {missingCount - 5} mere";
                    }
                    summary += missingDetails;
                }

                TaskDialog.Show("BIM7AA Validering", summary);

                // Select problematic elements
                var problemIds = results
                    .Where(r => r.Status == "INVALID" || r.Status == "MISSING")
                    .Select(r => r.ElementId)
                    .ToList();

                if (problemIds.Any())
                {
                    uidoc.Selection.SetElementIds(problemIds);
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Fejl", $"Validering fejlede:\n{ex.Message}");
                return Result.Failed;
            }
        }

        private CodeValidationResult ValidateElement(Element elem, BIM7AADatabase db)
        {
            string? bim7aaCode = GetBIM7AACode(elem);
            string category = elem.Category?.Name ?? "Unknown";
            
            if (string.IsNullOrEmpty(bim7aaCode))
            {
                // Check if element should have a code
                if (ShouldHaveCode(elem, db))
                {
                    return new CodeValidationResult
                    {
                        ElementId = elem.Id,
                        ElementName = elem.Name,
                        Category = category,
                        CurrentCode = null,
                        SuggestedCode = db.GetCodeForCategory(category),
                        Status = "MISSING",
                        Message = "Mangler BIM7AA kode"
                    };
                }
                return null;
            }

            if (!db.IsValidCode(bim7aaCode))
            {
                return new CodeValidationResult
                {
                    ElementId = elem.Id,
                    ElementName = elem.Name,
                    Category = category,
                    CurrentCode = bim7aaCode,
                    SuggestedCode = db.GetCodeForCategory(category),
                    Status = "INVALID",
                    Message = $"'{bim7aaCode}' er ikke en gyldig BIM7AA kode"
                };
            }

            return new CodeValidationResult
            {
                ElementId = elem.Id,
                ElementName = elem.Name,
                Category = category,
                CurrentCode = bim7aaCode,
                SuggestedCode = null,
                Status = "VALID",
                Message = "OK"
            };
        }

        private string? GetBIM7AACode(Element elem)
        {
            string[] paramNames = { 
                "BIM7AA_TypeCode", "BIM7AA", "Type Code", "Classification", 
                "bS_Forwarding", "Assembly Code" 
            };

            foreach (string paramName in paramNames)
            {
                Parameter param = elem.LookupParameter(paramName);
                if (param == null && elem is FamilyInstance fi)
                {
                    param = fi.Symbol?.LookupParameter(paramName);
                }

                if (param != null && param.HasValue)
                {
                    string value = param.AsString() ?? "";
                    if (!string.IsNullOrEmpty(value))
                        return value.Trim();
                }
            }

            return null;
        }

        private bool ShouldHaveCode(Element elem, BIM7AADatabase db)
        {
            // Filter out categories that shouldn't have BIM7AA codes
            string category = elem.Category?.Name ?? "";
            
            var noCodeCategories = new[] {
                "Lines", "Dimensions", "Text Notes", "Reference Planes",
                "Cameras", "Scope Boxes", "Section Boxes", "Annotations"
            };

            return !noCodeCategories.Contains(category, StringComparer.OrdinalIgnoreCase);
        }
    }

    public class CodeValidationResult
    {
        public ElementId ElementId { get; set; }
        public string ElementName { get; set; } = "";
        public string Category { get; set; } = "";
        public string? CurrentCode { get; set; }
        public string? SuggestedCode { get; set; }
        public string Status { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
