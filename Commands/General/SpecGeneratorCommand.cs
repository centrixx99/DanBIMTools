using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DanBIMTools.Core;

namespace DanBIMTools.Commands.General
{
    [Transaction(TransactionMode.Manual)]
    public class SpecGeneratorCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                TaskDialogResult result = TaskDialog.Show("Specifikationsgenerator",
                    "Vælg specifikationstype:\n\nYes = Arbejdsbeskrivelse\nNo = Materialeliste",
                    TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No | TaskDialogCommonButtons.Cancel,
                    TaskDialogResult.Yes);

                string specType;
                if (result == TaskDialogResult.Yes)
                    specType = "Arbejdsbeskrivelse";
                else if (result == TaskDialogResult.No)
                    specType = "Materialeliste";
                else
                    return Result.Cancelled;

                string specContent = specType == "Arbejdsbeskrivelse" 
                    ? GenerateArbejdsbeskrivelse(doc) 
                    : GenerateMaterialeliste(doc);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = $"{specType.Replace(" ", "_")}_{doc.Title}_{timestamp}.txt";
                string filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
                    filename);

                File.WriteAllText(filePath, specContent, Encoding.UTF8);

                TaskDialog.Show("Specifikationsgenerator", 
                    $"{specType} genereret og gemt til:\n{filePath}");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Fejl", $"Specifikationsgenerator fejlede:\n{ex.Message}");
                return Result.Failed;
            }
        }

        private string GenerateArbejdsbeskrivelse(Document doc)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("ARBEJDSBESKRIVELSE");
            sb.AppendLine("=".PadRight(60, '='));
            sb.AppendLine();
            sb.AppendLine($"Projekt: {doc.Title}");
            sb.AppendLine($"Dato: {DateTime.Now:dd-MM-yyyy}");
            sb.AppendLine($"Genereret af: DanBIM Tools v1.5.0");
            sb.AppendLine();

            // Walls
            var walls = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType()
                .Cast<Wall>()
                .ToList();

            if (walls.Any())
            {
                sb.AppendLine("1. VÆGARBEJDER");
                sb.AppendLine("-".PadRight(40, '-'));
                
                var wallTypes = walls.GroupBy(w => w.WallType.Name)
                    .OrderByDescending(g => g.Count());

                foreach (var typeGroup in wallTypes)
                {
                    double totalArea = typeGroup.Sum(w => UnitHelper.GetAreaSqMeters(w, "Area"));
                    sb.AppendLine($"  {typeGroup.Key}: {typeGroup.Count()} stk, {totalArea:F1} m²");
                }
                sb.AppendLine();
            }

            // Floors
            var floors = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Floors)
                .WhereElementIsNotElementType()
                .ToList();

            if (floors.Any())
            {
                sb.AppendLine("2. ETAGEDÆK");
                sb.AppendLine("-".PadRight(40, '-'));
                
                var floorTypes = floors.GroupBy(f => f.Name)
                    .OrderByDescending(g => g.Count());

                foreach (var typeGroup in floorTypes)
                {
                    double totalArea = typeGroup.Sum(f => UnitHelper.GetAreaSqMeters(f, "Area"));
                    sb.AppendLine($"  {typeGroup.Key}: {totalArea:F1} m²");
                }
                sb.AppendLine();
            }

            // Doors
            var doors = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Doors)
                .WhereElementIsNotElementType()
                .ToList();

            if (doors.Any())
            {
                sb.AppendLine("3. DØRE");
                sb.AppendLine("-".PadRight(40, '-'));
                
                var doorTypes = doors.GroupBy(d => d.Name)
                    .OrderByDescending(g => g.Count());

                foreach (var typeGroup in doorTypes)
                {
                    sb.AppendLine($"  {typeGroup.Key}: {typeGroup.Count()} stk");
                }
                sb.AppendLine();
            }

            // Windows
            var windows = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Windows)
                .WhereElementIsNotElementType()
                .ToList();

            if (windows.Any())
            {
                sb.AppendLine("4. VINDUER");
                sb.AppendLine("-".PadRight(40, '-'));
                
                var windowTypes = windows.GroupBy(w => w.Name)
                    .OrderByDescending(g => g.Count());

                foreach (var typeGroup in windowTypes)
                {
                    sb.AppendLine($"  {typeGroup.Key}: {typeGroup.Count()} stk");
                }
                sb.AppendLine();
            }

            // Ducts
            var ducts = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_DuctCurves)
                .ToList();

            if (ducts.Any())
            {
                sb.AppendLine("5. VENTILATION");
                sb.AppendLine("-".PadRight(40, '-'));
                
                double totalLength = ducts.Sum(d => UnitHelper.GetLengthMeters(d, "Length"));
                sb.AppendLine($"  Kanaler: {ducts.Count} stk, {totalLength:F1} m samlet længde");
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("Bemærkninger:");
            sb.AppendLine("- Mængder er baseret på modeldata");
            sb.AppendLine("- Afvigelser kan forekomme");
            sb.AppendLine("- Kontroller mod arbejdstegninger");

            return sb.ToString();
        }

        private string GenerateMaterialeliste(Document doc)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("MATERIALELISTE");
            sb.AppendLine("=".PadRight(60, '='));
            sb.AppendLine();
            sb.AppendLine($"Projekt: {doc.Title}");
            sb.AppendLine($"Dato: {DateTime.Now:dd-MM-yyyy}");
            sb.AppendLine();

            sb.AppendLine("Materiale\tMængde\tEnhed");
            sb.AppendLine("-".PadRight(40, '-'));

            // Walls
            var walls = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType()
                .ToList();
            double wallArea = walls.Sum(w => UnitHelper.GetAreaSqMeters(w, "Area"));
            if (wallArea > 0)
                sb.AppendLine($"Vægge\t{wallArea:F1}\tm²");

            // Floors
            var floors = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Floors)
                .WhereElementIsNotElementType()
                .ToList();
            double floorArea = floors.Sum(f => UnitHelper.GetAreaSqMeters(f, "Area"));
            if (floorArea > 0)
                sb.AppendLine($"Etagedæk\t{floorArea:F1}\tm²");

            // Roofs
            var roofs = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Roofs)
                .WhereElementIsNotElementType()
                .ToList();
            double roofArea = roofs.Sum(r => UnitHelper.GetAreaSqMeters(r, "Area"));
            if (roofArea > 0)
                sb.AppendLine($"Tage\t{roofArea:F1}\tm²");

            // Doors
            var doors = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Doors)
                .WhereElementIsNotElementType()
                .ToList();
            if (doors.Count > 0)
                sb.AppendLine($"Døre\t{doors.Count}\tstk");

            // Windows
            var windows = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Windows)
                .WhereElementIsNotElementType()
                .ToList();
            if (windows.Count > 0)
                sb.AppendLine($"Vinduer\t{windows.Count}\tstk");

            // Ducts
            var ducts = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_DuctCurves)
                .WhereElementIsNotElementType()
                .ToList();
            double ductLength = ducts.Sum(d => UnitHelper.GetLengthMeters(d, "Length"));
            if (ductLength > 0)
                sb.AppendLine($"Ventilationskanaler\t{ductLength:F1}\tm");

            // Pipes
            var pipes = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_PipeCurves)
                .WhereElementIsNotElementType()
                .ToList();
            double pipeLength = pipes.Sum(p => UnitHelper.GetLengthMeters(p, "Length"));
            if (pipeLength > 0)
                sb.AppendLine($"Rør\t{pipeLength:F1}\tm");

            // Cable trays
            var cableTrays = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_CableTray)
                .WhereElementIsNotElementType()
                .ToList();
            double trayLength = cableTrays.Sum(t => UnitHelper.GetLengthMeters(t, "Length"));
            if (trayLength > 0)
                sb.AppendLine($"Kabelbaner\t{trayLength:F1}\tm");

            sb.AppendLine();
            sb.AppendLine("Noter:");
            sb.AppendLine("- Mængder er vejledende");
            sb.AppendLine("- Beregn 5-10% spild");

            return sb.ToString();
        }
    }
}