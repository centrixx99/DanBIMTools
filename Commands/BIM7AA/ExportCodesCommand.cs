using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace DanBIMTools.Commands.BIM7AA
{
    [Transaction(TransactionMode.Manual)]
    public class ExportCodesCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Get elements with BIM7AA codes
                var elementsWithCodes = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .Where(e => e.Category != null && HasBIM7AACode(e))
                    .ToList();

                if (elementsWithCodes.Count == 0)
                {
                    TaskDialog.Show("Eksport BIM7AA", "Ingen elementer med BIM7AA koder fundet.");
                    return Result.Cancelled;
                }

                // Ask for export format
                TaskDialogResult formatResult = TaskDialog.Show("Eksport BIM7AA",
                    $"Fundet {elementsWithCodes.Count} elementer med BIM7AA koder.\n\nVælg format:",
                    TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No | TaskDialogCommonButtons.Cancel,
                    TaskDialogResult.Yes);

                string format;
                if (formatResult == TaskDialogResult.Yes)
                    format = "CSV";
                else if (formatResult == TaskDialogResult.No)
                    format = "TSV";
                else
                    return Result.Cancelled;

                // Generate export
                string exportContent = GenerateExport(elementsWithCodes, doc, format);

                // Save to file
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = $"BIM7AA_Codes_{doc.Title}_{timestamp}.{format.ToLower()}";
                string filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
                    filename);

                File.WriteAllText(filePath, exportContent, Encoding.UTF8);

                TaskDialog.Show("Eksport BIM7AA", 
                    $"Eksporteret {elementsWithCodes.Count} elementer til:\n{filePath}");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Fejl", $"Eksport fejlede:\n{ex.Message}");
                return Result.Failed;
            }
        }

        private bool HasBIM7AACode(Element elem)
        {
            string[] paramNames = { "BIM7AA_TypeCode", "BIM7AA", "Type Code", "Classification", "bS_Forwarding" };

            foreach (string paramName in paramNames)
            {
                Parameter param = elem.LookupParameter(paramName);
                if (param == null && elem is FamilyInstance fi)
                {
                    param = fi.Symbol?.LookupParameter(paramName);
                }

                if (param != null && param.HasValue)
                {
                    string value = param.AsString();
                    if (!string.IsNullOrEmpty(value))
                        return true;
                }
            }

            return false;
        }

        private string GetBIM7AACode(Element elem)
        {
            string[] paramNames = { "BIM7AA_TypeCode", "BIM7AA", "Type Code", "Classification", "bS_Forwarding" };

            foreach (string paramName in paramNames)
            {
                Parameter param = elem.LookupParameter(paramName);
                if (param == null && elem is FamilyInstance fi)
                {
                    param = fi.Symbol?.LookupParameter(paramName);
                }

                if (param != null && param.HasValue)
                {
                    return param.AsString()?.Trim() ?? "";
                }
            }

            return "";
        }

        private string GenerateExport(List<Element> elements, Document doc, string format)
        {
            StringBuilder sb = new StringBuilder();
            string delimiter = format == "CSV" ? "," : "\t";

            // Header
            sb.AppendLine($"Element ID{delimiter}Kategori{delimiter}Familie{delimiter}Type{delimiter}BIM7AA Kode{delimiter}Niveau{delimiter}Mængde{delimiter}Enhed");

            foreach (Element elem in elements.OrderBy(e => e.Category?.Name))
            {
                string code = GetBIM7AACode(elem);
                if (string.IsNullOrEmpty(code)) continue;

                string category = elem.Category?.Name ?? "Ukendt";
                string family = elem is FamilyInstance fi ? fi.Symbol.FamilyName : "-";
                string type = elem is FamilyInstance fi2 ? fi2.Symbol.Name : elem.Name;
                string level = GetLevelName(elem, doc);
                string quantity = GetQuantity(elem);
                string unit = GetUnit(elem);

                // Escape values for CSV
                sb.AppendLine($"{Escape(elem.Id.Value.ToString())}{delimiter}" +
                    $"{Escape(category)}{delimiter}" +
                    $"{Escape(family)}{delimiter}" +
                    $"{Escape(type)}{delimiter}" +
                    $"{Escape(code)}{delimiter}" +
                    $"{Escape(level)}{delimiter}" +
                    $"{quantity}{delimiter}" +
                    $"{Escape(unit)}");
            }

            return sb.ToString();
        }

        private string GetLevelName(Element elem, Document doc)
        {
            Parameter levelParam = elem.LookupParameter("Level");
            if (levelParam != null && levelParam.HasValue)
            {
                ElementId levelId = levelParam.AsElementId();
                if (levelId != null && levelId.Value > 0)
                {
                    Level level = doc.GetElement(levelId) as Level;
                    return level?.Name ?? "-";
                }
            }

            // Try base constraint for walls
            Parameter baseConstraint = elem.LookupParameter("Base Constraint");
            if (baseConstraint != null && baseConstraint.HasValue)
            {
                ElementId levelId = baseConstraint.AsElementId();
                if (levelId != null && levelId.Value > 0)
                {
                    Level level = doc.GetElement(levelId) as Level;
                    return level?.Name ?? "-";
                }
            }

            return "-";
        }

        private string GetQuantity(Element elem)
        {
            // Try to get area, volume, or length
            double[] quantities = {
                GetParamValue(elem, "Area"),
                GetParamValue(elem, "Volume"),
                GetParamValue(elem, "Length")
            };

            foreach (double qty in quantities)
            {
                if (qty > 0)
                    return qty.ToString("F2");
            }

            return "1";
        }

        private double GetParamValue(Element elem, string paramName)
        {
            Parameter param = elem.LookupParameter(paramName);
            if (param != null && param.HasValue)
            {
                return param.AsDouble();
            }
            return 0;
        }

        private string GetUnit(Element elem)
        {
            if (GetParamValue(elem, "Area") > 0) return "m²";
            if (GetParamValue(elem, "Volume") > 0) return "m³";
            if (GetParamValue(elem, "Length") > 0) return "m";
            return "stk";
        }

        private string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            // Quote if contains comma or newline
            if (value.Contains(",") || value.Contains("\n") || value.Contains("\"") || value.Contains("\t"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
    }
}
