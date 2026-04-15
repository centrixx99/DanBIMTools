using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace DanBIMTools.Commands.General
{
    [Transaction(TransactionMode.Manual)]
    public class ExportReportCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Generate comprehensive validation report
                ValidationReport report = GenerateReport(doc);

                // Create HTML report
                string htmlContent = CreateHtmlReport(doc, report);

                // Save to file
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = $"DanBIM_ValidationReport_{doc.Title}_{timestamp}.html";
                string filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    filename);

                File.WriteAllText(filePath, htmlContent, Encoding.UTF8);

                TaskDialog.Show("Rapport Eksport",
                    $"Valideringsrapport genereret:\n{filePath}\n\n" +
                    $"Åbn filen i din browser for at se den interaktive rapport.");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Fejl", $"Rapport generering fejlede:\n{ex.Message}");
                return Result.Failed;
            }
        }

        private ValidationReport GenerateReport(Document doc)
        {
            var report = new ValidationReport
            {
                ProjectName = doc.Title,
                GeneratedDate = DateTime.Now,
                Elements = new List<ElementValidation>()
            };

            var allElements = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .Where(e => e.Category != null)
                .ToList();

            foreach (Element elem in allElements)
            {
                if (!ShouldValidate(elem)) continue;

                var validation = new ElementValidation
                {
                    ElementId = elem.Id.Value,
                    ElementName = elem.Name,
                    Category = elem.Category.Name,
                    Issues = new List<string>()
                };

                // Check BIM7AA
                if (!HasBIM7AACode(elem))
                    validation.Issues.Add("Mangler BIM7AA kode");

                // Check material
                if (!HasMaterial(elem))
                    validation.Issues.Add("Mangler materiale");

                // Check structural marking
                if (IsStructuralCategory(elem.Category.Name) && !IsMarkedStructural(elem))
                    validation.Issues.Add("Ikke markeret som bærende");

                // Check fire rating for certain elements
                if (NeedsFireRating(elem.Category.Name) && !HasFireRating(elem))
                    validation.Issues.Add("Mangler brandklassifikation");

                if (validation.Issues.Any())
                {
                    report.Elements.Add(validation);
                }
            }

            return report;
        }

        private bool ShouldValidate(Element elem)
        {
            var validateCategories = new[] {
                "Walls", "Floors", "Doors", "Windows", "Roofs",
                "Structural Columns", "Structural Framing", "Ducts", "Pipes",
                "Mechanical Equipment", "Electrical Equipment"
            };
            return validateCategories.Contains(elem.Category?.Name);
        }

        private bool HasBIM7AACode(Element elem)
        {
            string[] paramNames = { "BIM7AA_TypeCode", "BIM7AA", "Type Code", "Classification" };
            return paramNames.Any(name => HasParameterValue(elem, name));
        }

        private bool HasMaterial(Element elem)
        {
            return HasParameterValue(elem, "Material") ||
                   HasParameterValue(elem, "Structural Material");
        }

        private bool IsStructuralCategory(string category)
        {
            return category == "Walls" || category == "Floors" ||
                   category == "Structural Columns" || category == "Structural Framing";
        }

        private bool IsMarkedStructural(Element elem)
        {
            return HasParameterValue(elem, "Structural");
        }

        private bool NeedsFireRating(string category)
        {
            return category == "Walls" || category == "Doors" || category == "Floors";
        }

        private bool HasFireRating(Element elem)
        {
            return HasParameterValue(elem, "Fire Rating") ||
                   HasParameterValue(elem, "FireRating");
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
                return !string.IsNullOrWhiteSpace(param.AsString());

            return true;
        }

        private string CreateHtmlReport(Document doc, ValidationReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"da\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"UTF-8\">");
            sb.AppendLine("<title>DanBIM Valideringsrapport</title>");
            sb.AppendLine("<style>");
            sb.AppendLine(@"
                body { font-family: Arial, sans-serif; margin: 40px; background: #f5f5f5; }
                .container { max-width: 1200px; margin: 0 auto; background: white; padding: 30px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
                h1 { color: #333; border-bottom: 3px solid #0078d4; padding-bottom: 10px; }
                h2 { color: #555; margin-top: 30px; }
                .summary { background: #f0f0f0; padding: 20px; border-radius: 5px; margin: 20px 0; }
                table { width: 100%; border-collapse: collapse; margin-top: 20px; }
                th { background: #0078d4; color: white; padding: 12px; text-align: left; }
                td { padding: 10px; border-bottom: 1px solid #ddd; }
                tr:hover { background: #f5f5f5; }
                .issue { color: #d32f2f; }
                .pass { color: #388e3c; }
                .stats { display: flex; gap: 20px; margin: 20px 0; }
                .stat-box { background: #e3f2fd; padding: 15px; border-radius: 5px; text-align: center; flex: 1; }
                .stat-value { font-size: 2em; font-weight: bold; color: #0078d4; }
                .stat-label { color: #666; }
            ");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='container'>");

            // Header
            sb.AppendLine($"<h1>📊 DanBIM Valideringsrapport</h1>");
            sb.AppendLine($"<p><strong>Projekt:</strong> {report.ProjectName}</p>");
            sb.AppendLine($"<p><strong>Genereret:</strong> {report.GeneratedDate:dd-MM-yyyy HH:mm}</p>");

            // Statistics
            var byCategory = report.Elements.GroupBy(e => e.Category)
                .OrderByDescending(g => g.Count())
                .Take(10);

            int totalIssues = report.Elements.Sum(e => e.Issues.Count);

            sb.AppendLine("<div class='stats'>");
            sb.AppendLine($"<div class='stat-box'><div class='stat-value'>{report.Elements.Count}</div><div class='stat-label'>Elementer med problemer</div></div>");
            sb.AppendLine($"<div class='stat-box'><div class='stat-value'>{totalIssues}</div><div class='stat-label'>Antal problemer</div></div>");
            sb.AppendLine($"<div class='stat-box'><div class='stat-value'>{byCategory.Count()}</div><div class='stat-label'>Kategorier påvirket</div></div>");
            sb.AppendLine("</div>");

            // Issues by category
            sb.AppendLine("<h2>Problemer pr. kategori</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Kategori</th><th>Antal elementer</th></tr>");
            foreach (var cat in byCategory)
            {
                sb.AppendLine($"<tr><td>{cat.Key}</td><td>{cat.Count()}</td></tr>");
            }
            sb.AppendLine("</table>");

            // Detailed table
            sb.AppendLine("<h2>Detaljeret oversigt</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Element ID</th><th>Navn</th><th>Kategori</th><th>Problemer</th></tr>");

            foreach (var elem in report.Elements.OrderBy(e => e.Category).Take(100))
            {
                string issues = string.Join("; ", elem.Issues);
                sb.AppendLine($"<tr><td>{elem.ElementId}</td><td>{EscapeHtml(elem.ElementName)}</td><td>{elem.Category}</td><td class='issue'>{EscapeHtml(issues)}</td></tr>");
            }

            if (report.Elements.Count > 100)
            {
                sb.AppendLine($"<tr><td colspan='4' style='text-align:center;'>... og {report.Elements.Count - 100} mere</td></tr>");
            }

            sb.AppendLine("</table>");

            // Footer
            sb.AppendLine("<div style='margin-top: 40px; padding-top: 20px; border-top: 1px solid #ddd; color: #666;'>");
            sb.AppendLine("<p>Genereret af DanBIM Tools for Revit</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private string EscapeHtml(string input)
        {
            return input?
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;") ?? "";
        }
    }

    public class ValidationReport
    {
        public string ProjectName { get; set; } = "";
        public DateTime GeneratedDate { get; set; }
        public List<ElementValidation> Elements { get; set; } = new();
    }

    public class ElementValidation
    {
        public long ElementId { get; set; }
        public string ElementName { get; set; } = "";
        public string Category { get; set; } = "";
        public List<string> Issues { get; set; } = new();
    }
}
