using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using DanBIMTools.Core;

// Resolve WPF/Revit TextBox ambiguity
using WpfTextBox = System.Windows.Controls.TextBox;

namespace DanBIMTools.Commands.HVAC
{
    [Transaction(TransactionMode.Manual)]
    public class DuctSizingCommand : IExternalCommand
    {
        private const double DEFAULT_VELOCITY_M_S = 4.0; // m/s for main ducts
        private const double DEFAULT_PRESSURE_DROP_PA_M = 1.0; // Pa/m

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                ISelectionFilter ductFilter = new DuctSelectionFilter();
                Reference? pickedRef = uidoc.Selection.PickObject(ObjectType.Element, ductFilter, "Vælg kanal eller ventilationsaggregat");
                
                if (pickedRef == null)
                {
                    TaskDialog.Show("Kanaludregning", "Intet element valgt.");
                    return Result.Cancelled;
                }

                Element elem = doc.GetElement(pickedRef);
                
                // Get airflow from parameter
                double airflowLps = GetAirflow(elem);
                
                // If no airflow found, ask user
                if (airflowLps <= 0)
                {
                    airflowLps = PromptForAirflow(elem.Name);
                    if (airflowLps <= 0)
                    {
                        TaskDialog.Show("Kanaludregning", "Ingen luftmængde angivet. Afbrudt.");
                        return Result.Cancelled;
                    }
                }

                DuctSizeResult sizeResult = CalculateDuctSize(airflowLps);

                string resultMessage = $"Luftmængde: {airflowLps:F0} L/s\n" +
                    $"Anbefalet kanalstørrelse:\n" +
                    $"  Rektangulær: {sizeResult.Rectangular}\n" +
                    $"  Rund: Ø{sizeResult.RoundDiameter:F0} mm\n" +
                    $"  Hastighed: {sizeResult.Velocity:F1} m/s\n" +
                    $"  Tryktab: ~{sizeResult.PressureDrop:F2} Pa/m";

                TaskDialogResult dialogResult = TaskDialog.Show("Kanaludregning", resultMessage, 
                    TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel);

                if (dialogResult == TaskDialogResult.Ok)
                {
                    if (elem is Duct duct)
                    {
                        using (Transaction trans = new Transaction(doc, "Sæt kanalstørrelse"))
                        {
                            trans.Start();
                            // Append to Comments instead of overwriting
                            Parameter commentParam = duct.LookupParameter("Comments");
                            if (commentParam != null && !commentParam.IsReadOnly)
                            {
                                string existing = commentParam.AsString() ?? "";
                                string note = $"[DanBIM] Beregnet: {sizeResult.Rectangular}, Ø{sizeResult.RoundDiameter}mm";
                                string newComment = string.IsNullOrEmpty(existing) ? note : $"{existing}; {note}";
                                commentParam.Set(newComment);
                            }
                            trans.Commit();
                        }
                        TaskDialog.Show("Kanaludregning", "Beregning noteret i kommentar (tilføjet, ikke overskrevet).");
                    }
                }

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Fejl", $"Kanaludregning fejlede:\n{ex.Message}");
                return Result.Failed;
            }
        }

        /// <summary>
        /// Prompts user for airflow value via a simple WPF dialog.
        /// Returns 0 if cancelled.
        /// </summary>
        private double PromptForAirflow(string elementName)
        {
            double result = 0;
            var window = new Window
            {
                Title = "Angiv luftmængde",
                Width = 350,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var stack = new StackPanel { Margin = new Thickness(15) };
            stack.Children.Add(new TextBlock 
            { 
                Text = $"Element: {elementName}", 
                Margin = new Thickness(0, 0, 0, 10),
                FontWeight = FontWeights.Bold 
            });
            stack.Children.Add(new TextBlock { Text = "Luftmængde (L/s):" });
            var input = new WpfTextBox { Text = "500", Margin = new Thickness(0, 5, 0, 10) };
            stack.Children.Add(input);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okBtn = new Button { Content = "OK", Width = 70, Height = 25, Margin = new Thickness(5, 0, 0, 0) };
            var cancelBtn = new Button { Content = "Annuller", Width = 70, Height = 25, Margin = new Thickness(5, 0, 0, 0) };
            btnPanel.Children.Add(okBtn);
            btnPanel.Children.Add(cancelBtn);
            stack.Children.Add(btnPanel);

            window.Content = stack;

            okBtn.Click += (s, e) =>
            {
                if (double.TryParse(input.Text, out double val) && val > 0)
                {
                    result = val;
                    window.DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Indtast et gyldigt tal > 0", "Ugyldig værdi");
                }
            };
            cancelBtn.Click += (s, e) => { window.DialogResult = false; };

            bool? dialogResult = window.ShowDialog();
            return dialogResult == true ? result : 0;
        }

        private double GetAirflow(Element elem)
        {
            // Try common parameter names for airflow
            string[] paramNames = { "Air Flow", "Flow", "L/s", "Lps", "AirFlow", "Flow Rate" };
            
            foreach (string paramName in paramNames)
            {
                double? val = UnitHelper.GetDoubleValue(elem, paramName);
                if (val.HasValue && val.Value > 0)
                {
                    // Revit stores airflow in ft³/s internally
                    if (paramName == "Air Flow" || paramName == "AirFlow")
                        return val.Value / UnitHelper.LpsToCfs; // ft³/s to L/s
                    return val.Value;
                }
            }

            // Try to get from comment parameter
            string? comment = UnitHelper.GetStringValue(elem, "Comments");
            if (!string.IsNullOrEmpty(comment))
            {
                var match = System.Text.RegularExpressions.Regex.Match(comment, @"(\d+(?:\.\d+)?)\s*(?:L/s|Lps|l/s)");
                if (match.Success && double.TryParse(match.Groups[1].Value, out double parsed))
                    return parsed;
            }

            return 0;
        }

        private DuctSizeResult CalculateDuctSize(double airflowLps)
        {
            double airflowM3s = airflowLps / 1000.0; // L/s to m³/s
            
            double area = airflowM3s / DEFAULT_VELOCITY_M_S; // m²
            double diameter = Math.Sqrt(4 * area / Math.PI) * 1000; // mm
            
            // Standard duct sizes (Danish standards - DS/EN 1505, 1506)
            int[] standardDiameters = { 80, 100, 125, 160, 200, 250, 315, 400, 500, 630, 800, 1000 };
            int nearestDiameter = standardDiameters.OrderBy(d => Math.Abs(d - diameter)).First();
            
            // Calculate rectangular equivalent (maintain area)
            double rectHeight = nearestDiameter;
            double rectWidth = area * 1000000 / rectHeight; // mm
            
            // Round to standard sizes
            int[] standardWidths = { 100, 150, 200, 250, 300, 400, 500, 600, 800, 1000, 1200 };
            int nearestWidth = standardWidths.OrderBy(w => Math.Abs(w - rectWidth)).First();
            
            // Calculate actual velocity with selected size
            double actualArea = Math.PI * Math.Pow(nearestDiameter / 2000.0, 2); // m²
            double actualVelocity = airflowM3s / actualArea;
            
            // Estimate pressure drop (simplified Darcy-Weisbach)
            double pressureDrop = Math.Pow(actualVelocity / DEFAULT_VELOCITY_M_S, 2) * DEFAULT_PRESSURE_DROP_PA_M;

            return new DuctSizeResult
            {
                RoundDiameter = nearestDiameter,
                Rectangular = $"{nearestWidth}x{rectHeight:F0}",
                Velocity = actualVelocity,
                PressureDrop = pressureDrop
            };
        }
    }

    public class DuctSizeResult
    {
        public int RoundDiameter { get; set; }
        public string Rectangular { get; set; } = "";
        public double Velocity { get; set; }
        public double PressureDrop { get; set; }
    }

    public class DuctSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Duct || elem is MechanicalEquipment || elem is FamilyInstance;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}