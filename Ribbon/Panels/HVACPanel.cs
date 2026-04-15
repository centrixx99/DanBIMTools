using System;
using Autodesk.Revit.UI;
using DanBIMTools.Commands.HVAC;
using DanBIMTools.Ribbon;

namespace DanBIMTools.Ribbon.Panels;

/// <summary>
/// HVAC panel with duct sizing and validation tools.
/// </summary>
public static class HVACPanel
{
    public const string PanelName = "HVAC";
    
    public static void Create(UIControlledApplication application)
    {
        RibbonPanel? panel = application.CreateRibbonPanel(DanBIMRibbon.RibbonTabName, PanelName);
        
        if (panel == null) return;
        
        // Duct Sizing button
        PushButtonData ductSizingBtn = new PushButtonData(
            "DuctSizing",
            "Duct\nSizing",
            typeof(App).Assembly.Location,
            typeof(DuctSizingCommand).FullName);
        
        ductSizingBtn.ToolTip = "Size ducts based on airflow requirements";
        ductSizingBtn.LongDescription = "Calculate optimal duct sizes based on airflow (L/s), velocity limits, and pressure drop calculations following Danish standards.";
        
        PushButton? btn1 = panel.AddItem(ductSizingBtn) as PushButton;
        if (btn1 != null)
        {
            btn1.LargeImage = IconProvider.GetLargeIcon(IconProvider.DuctSizing);
            btn1.Image = IconProvider.GetSmallIcon(IconProvider.DuctSizing);
        }
        
        // Clash Preview button
        PushButtonData clashBtn = new PushButtonData(
            "ClashPreview",
            "Clash\nPreview",
            typeof(App).Assembly.Location,
            typeof(ClashPreviewCommand).FullName);
        
        clashBtn.ToolTip = "Preview potential duct clashes";
        clashBtn.LongDescription = "Visualizes potential clashes between ducts and other building elements before they occur.";
        
        PushButton? btn2 = panel.AddItem(clashBtn) as PushButton;
        if (btn2 != null)
        {
            btn2.LargeImage = IconProvider.GetLargeIcon(IconProvider.ClashPreview);
            btn2.Image = IconProvider.GetSmallIcon(IconProvider.ClashPreview);
        }
        
        // Insulation Validator button
        PushButtonData insulationBtn = new PushButtonData(
            "InsulationValidator",
            "Insulation\nCheck",
            typeof(App).Assembly.Location,
            typeof(InsulationValidatorCommand).FullName);
        
        insulationBtn.ToolTip = "Validate duct insulation specifications";
        insulationBtn.LongDescription = "Checks that duct insulation meets project requirements and Danish building regulations (BR18).";
        
        PushButton? btn3 = panel.AddItem(insulationBtn) as PushButton;
        if (btn3 != null)
        {
            btn3.LargeImage = IconProvider.GetLargeIcon(IconProvider.InsulationCheck);
            btn3.Image = IconProvider.GetSmallIcon(IconProvider.InsulationCheck);
        }
    }
}
