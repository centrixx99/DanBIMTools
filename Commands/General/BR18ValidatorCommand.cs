using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DanBIMTools.Core;

namespace DanBIMTools.Commands.General
{
    /// <summary>
    /// Validates elements against Danish Building Regulations (BR18) requirements.
    /// Checks fire ratings, acoustic properties, and energy performance.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class BR18ValidatorCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                var br18 = DanishBuildingRegulations.Load();
                var selectedIds = uidoc.Selection.GetElementIds();
                
                if (selectedIds.Count == 0)
                {
                    TaskDialog.Show("BR18 Validator", "Vælg elementer først.");
                    return Result.Cancelled;
                }

                var results = new List<BR18ValidationResult>();

                foreach (ElementId id in selectedIds)
                {
                    Element elem = doc.GetElement(id);
                    if (elem == null || elem.Category == null) continue;

                    var categoryName = elem.Category.Name;
                    var elementName = elem.Name;

                    // Fire check
                    var fireReqs = br18.GetFireRequirements(categoryName);
                    string fireRating = GetParameterValue(elem, "FireRating");
                    foreach (var fireReq in fireReqs)
                    {
                        results.Add(new BR18ValidationResult
                        {
                            ElementId = id,
                            ElementName = elementName,
                            Category = categoryName,
                                    CheckType = "Brand",
                            Requirement = fireReq.RequiredRating ?? "N/A",
                            ActualValue = fireRating ?? "Ikke angivet",
                            Status = !string.IsNullOrEmpty(fireRating) ? "PASS" : "WARNING",
                            Notes = fireReq.Notes
                        });
                    }

                    // Energy check (U-value)
                    var energyReq = br18.GetEnergyRequirement(categoryName);
                    string uValue = GetParameterValue(elem, "U-Value", "ThermalTransmittance");
                    if (energyReq != null)
                    {
                        results.Add(new BR18ValidationResult
                        {
                            ElementId = id,
                            ElementName = elementName,
                            Category = categoryName,
                            CheckType = "Energi",
                            Requirement = $"U ≤ {energyReq.MaxUValue}",
                            ActualValue = uValue ?? "Ikke angivet",
                            Status = ValidateUValue(uValue, energyReq.MaxUValue),
                            Notes = energyReq.Notes
                        });
                    }

                    // Acoustic check
                    var acousticReq = br18.GetAcousticRequirement(categoryName, "Standard");
                    if (acousticReq != null)
                    {
                        results.Add(new BR18ValidationResult
                        {
                            ElementId = id,
                            ElementName = elementName,
                            Category = categoryName,
                            CheckType = "Lyd",
                            Requirement = $"Rw {acousticReq.Rw}",
                            ActualValue = "N/A",
                            Status = "INFO",
                            Notes = acousticReq.Notes
                        });
                    }
                }

                // Generate report
                ShowReport(results);

                // Select failed elements
                var failedIds = results
                    .Where(r => r.Status == "FAIL")
                    .Select(r => r.ElementId)
                    .Distinct()
                    .ToList();

                if (failedIds.Any())
                {
                    uidoc.Selection.SetElementIds(failedIds);
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Fejl", $"BR18 validering fejlede:\n{ex.Message}");
                return Result.Failed;
            }
        }

        private string? GetParameterValue(Element elem, params string[] paramNames)
        {
            foreach (var name in paramNames)
            {
                var param = elem.LookupParameter(name);
                if (param != null && param.HasValue)
                {
                    return param.AsValueString() ?? param.AsString();
                }

                // Check type parameters
                if (elem is FamilyInstance fi)
                {
                    param = fi.Symbol?.LookupParameter(name);
                    if (param != null && param.HasValue)
                    {
                        return param.AsValueString() ?? param.AsString();
                    }
                }
            }
            return null;
        }

        private string ValidateUValue(string? actual, string? requirement)
        {
            if (string.IsNullOrEmpty(actual)) return "WARNING";
            if (string.IsNullOrEmpty(requirement)) return "INFO";

            // Parse actual value
            var actualParts = actual.Split(' ');
            if (!double.TryParse(actualParts[0], out double actualVal))
                return "WARNING";

            // Parse requirement (e.g., "U ≤ 0.18 W/m²K")
            var reqMatch = System.Text.RegularExpressions.Regex.Match(requirement, @"[0-9]+\.?[0-9]*");
            if (!reqMatch.Success || !double.TryParse(reqMatch.Value, out double reqVal))
                return "INFO";

            return actualVal <= reqVal ? "PASS" : "FAIL";
        }

        private void ShowReport(List<BR18ValidationResult> results)
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("BR18 Bygningsreglement Validator");
            summary.AppendLine($"{results.Count} kontroller udført");
            summary.AppendLine();

            var byStatus = results.GroupBy(r => r.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            summary.AppendLine($"  ✅ OK: {byStatus.GetValueOrDefault("PASS")}");
            summary.AppendLine($"  ❌ Fejl: {byStatus.GetValueOrDefault("FAIL")}");
            summary.AppendLine($"  ⚠️  Advarsel: {byStatus.GetValueOrDefault("WARNING")}");
            summary.AppendLine($"  ℹ️  Info: {byStatus.GetValueOrDefault("INFO")}");
            summary.AppendLine();

            // Show failures/warnings
            var issues = results.Where(r => r.Status != "PASS").Take(20);
            if (issues.Any())
            {
                summary.AppendLine("Problemer fundet:");
                foreach (var issue in issues)
                {
                    summary.AppendLine($"  [{issue.CheckType}] {issue.ElementName}: {issue.ActualValue} (forventet: {issue.Requirement})");
                }
            }
            else
            {
                summary.AppendLine("Alle kontroller bestået!");
            }

            TaskDialog.Show("BR18 Validering", summary.ToString());
        }
    }

    public class BR18ValidationResult
    {
        public ElementId ElementId { get; set; }
        public string ElementName { get; set; } = "";
        public string Category { get; set; } = "";
        public string CheckType { get; set; } = "";
        public string Requirement { get; set; } = "";
        public string ActualValue { get; set; } = "";
        public string Status { get; set; } = "";
        public string? Notes { get; set; }
    }
}
