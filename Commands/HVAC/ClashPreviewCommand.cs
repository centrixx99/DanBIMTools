using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace DanBIMTools.Commands.HVAC
{
    [Transaction(TransactionMode.Manual)]
    public class ClashPreviewCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Select duct system or multiple ducts
                TaskDialog.Show("Kollisionskontrol", 
                    "Vælg kanaler eller et ventilations system for at forhåndsvisning potentielle kollisioner.\n" +
                    "Værktøjet tjekker mod bærende konstruktioner, ande installationer og arkitektur.");

                IList<Reference>? pickedRefs = uidoc.Selection.PickObjects(
                    ObjectType.Element, 
                    new DuctSystemFilter(),
                    "Vælg kanaler/systemer til kollisionskontrol");

                if (pickedRefs == null || pickedRefs.Count == 0)
                {
                    return Result.Cancelled;
                }

                // Collect ducts
                List<Duct> ducts = new List<Duct>();
                foreach (Reference refObj in pickedRefs)
                {
                    Element elem = doc.GetElement(refObj);
                    if (elem is Duct duct)
                    {
                        ducts.Add(duct);
                    }
                    else if (elem is MechanicalSystem system)
                    {
                        // Get all ducts from system
                        var systemDucts = new FilteredElementCollector(doc)
                            .OfClass(typeof(Duct))
                            .Cast<Duct>()
                            .Where(d => d.MEPSystem?.Id == system.Id);
                        ducts.AddRange(systemDucts);
                    }
                }

                if (ducts.Count == 0)
                {
                    TaskDialog.Show("Kollisionskontrol", "Ingen kanaler fundet i valget.");
                    return Result.Cancelled;
                }

                // Find potential clashes
                List<ClashResult> clashes = FindPotentialClashes(doc, ducts);

                // Show results
                if (clashes.Count == 0)
                {
                    TaskDialog.Show("Kollisionskontrol", 
                        $"Kontrolleret {ducts.Count} kanaler.\nIngen potentielle kollisioner fundet.");
                }
                else
                {
                    // Create highlight
                    HighlightClashes(uidoc, clashes);

                    string clashSummary = string.Join("\n", clashes
                        .GroupBy(c => c.ClashType)
                        .Select(g => $"  {g.Key}: {g.Count()}"));

                    TaskDialogResult result = TaskDialog.Show("Kollisionskontrol",
                        $"Kontrolleret {ducts.Count} kanaler.\n" +
                        $"Fundet {clashes.Count} potentielle kollisioner:\n\n{clashSummary}\n\n" +
                        $"Kollisionsområder markeret midlertidigt.",
                        TaskDialogCommonButtons.Ok);
                }

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Fejl", $"Kollisionskontrol fejlede:\n{ex.Message}");
                return Result.Failed;
            }
        }

        private List<ClashResult> FindPotentialClashes(Document doc, List<Duct> ducts)
        {
            List<ClashResult> clashes = new List<ClashResult>();
            double clashTolerance = 0.05; // 50mm tolerance

            // Get all potential clash elements
            var structuralElements = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .Union(new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralColumns))
                .Union(new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Floors))
                .Where(e => e is Element)
                .ToList();

            var otherMEP = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_PipeCurves)
                .Union(new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_CableTray))
                .Where(e => e is Element)
                .ToList();

            foreach (Duct duct in ducts)
            {
                BoundingBoxXYZ? ductBBox = duct.get_BoundingBox(null);
                if (ductBBox == null) continue;

                // Expand bbox for tolerance
                Outline outline = new Outline(
                    new XYZ(ductBBox.Min.X - clashTolerance, ductBBox.Min.Y - clashTolerance, ductBBox.Min.Z - clashTolerance),
                    new XYZ(ductBBox.Max.X + clashTolerance, ductBBox.Max.Y + clashTolerance, ductBBox.Max.Z + clashTolerance));

                // Check against structural elements
                foreach (Element structElem in structuralElements)
                {
                    BoundingBoxXYZ? structBBox = structElem.get_BoundingBox(null);
                    if (structBBox == null) continue;

                    if (Intersects(outline, structBBox))
                    {
                        clashes.Add(new ClashResult
                        {
                            DuctId = duct.Id,
                            ClashingElementId = structElem.Id,
                            DuctName = duct.Name,
                            ClashingElementName = structElem.Name,
                            ClashType = "Bærende konstruktion",
                            Severity = "Høj"
                        });
                    }
                }

                // Check against other MEP
                foreach (Element mepElem in otherMEP)
                {
                    if (mepElem.Id == duct.Id) continue;

                    BoundingBoxXYZ? mepBBox = mepElem.get_BoundingBox(null);
                    if (mepBBox == null) continue;

                    if (Intersects(outline, mepBBox))
                    {
                        clashes.Add(new ClashResult
                        {
                            DuctId = duct.Id,
                            ClashingElementId = mepElem.Id,
                            DuctName = duct.Name,
                            ClashingElementName = mepElem.Name,
                            ClashType = "Anden installation",
                            Severity = "Medium"
                        });
                    }
                }
            }

            return clashes;
        }

        private bool Intersects(Outline outline, BoundingBoxXYZ bbox)
        {
            return outline.Intersects(new Outline(bbox.Min, bbox.Max), 0.0);
        }

        private void HighlightClashes(UIDocument uidoc, List<ClashResult> clashes)
        {
            // Select clashing elements for visual feedback
            List<ElementId> elementIds = new List<ElementId>();
            elementIds.AddRange(clashes.Select(c => c.DuctId));
            elementIds.AddRange(clashes.Select(c => c.ClashingElementId));

            uidoc.Selection.SetElementIds(elementIds.Distinct().ToList());
        }
    }

    public class ClashResult
    {
        public ElementId DuctId { get; set; }
        public ElementId ClashingElementId { get; set; }
        public string DuctName { get; set; } = "";
        public string ClashingElementName { get; set; } = "";
        public string ClashType { get; set; } = "";
        public string Severity { get; set; } = "";
    }

    public class DuctSystemFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Duct || elem is MechanicalSystem;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}
