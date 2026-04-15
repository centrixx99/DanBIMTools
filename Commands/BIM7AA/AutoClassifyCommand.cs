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
    public class AutoClassifyCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            
            // Get selected elements
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            
            if (selectedIds.Count == 0)
            {
                TaskDialog.Show("BIM7AA Auto-klassificering", "Vælg elementer først.");
                return Result.Cancelled;
            }
            
            BIM7AADatabase db = BIM7AADatabase.Load();
            int classifiedCount = 0;
            
            using (Transaction trans = new Transaction(doc, "BIM7AA Auto-klassificering"))
            {
                trans.Start();
                
                foreach (ElementId id in selectedIds)
                {
                    Element elem = doc.GetElement(id);
                    if (elem == null) continue;
                    
                    // Suggest BIM7AA code based on element category and family
                    string suggestedCode = SuggestBIM7AACode(elem, db);
                    
                    if (!string.IsNullOrEmpty(suggestedCode))
                    {
                        // Set BIM7AA parameter if it exists
                        Parameter param = elem.LookupParameter("BIM7AA_TypeCode") ?? 
                                         elem.LookupParameter("Type Code");
                        
                        if (param != null && !param.IsReadOnly)
                        {
                            param.Set(suggestedCode);
                            classifiedCount++;
                        }
                    }
                }
                
                trans.Commit();
            }
            
            TaskDialog.Show("BIM7AA Auto-klassificering", 
                $"Klassificeret {classifiedCount} af {selectedIds.Count} elementer.");
            
            return Result.Succeeded;
        }
        
        private string SuggestBIM7AACode(Element elem, BIM7AADatabase db)
        {
            Category cat = elem.Category;
            string categoryName = cat?.Name ?? "";
            string familyName = elem is FamilyInstance famInst ? 
                famInst.Symbol.FamilyName : elem.GetType().Name;
            
            // Map Revit categories to BIM7AA codes
            var categoryMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Walls", "23" },
                { "Floors", "28" },
                { "Roofs", "29" },
                { "Structural Columns", "32" },
                { "Structural Framing", "32" },
                { "Doors", "26" },
                { "Windows", "27" },
                { "Generic Models", "90" },
                { "Mechanical Equipment", "60" },
                { "Ducts", "62" },
                { "Pipes", "65" },
                { "Electrical Equipment", "70" },
                { "Lighting Fixtures", "71" },
                { "Cable Trays", "74" }
            };
            
            if (categoryMapping.TryGetValue(categoryName, out string code))
            {
                // Get more specific code from database if available
                var specificCode = db.GetCodeForFamily(familyName);
                return specificCode ?? code + "00";
            }
            
            return null;
        }
    }
}
