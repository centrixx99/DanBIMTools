using System;
using Autodesk.Revit.UI;
using DanBIMTools.Commands.BIM7AA;
using DanBIMTools.Ribbon;

namespace DanBIMTools.Ribbon.Panels;

/// <summary>
/// BIM7AA classification panel with Danish building code tools.
/// </summary>
public static class BIM7AAPanel
{
    public const string PanelName = "BIM7AA";
    
    public static void Create(UIControlledApplication application)
    {
        RibbonPanel? panel = application.CreateRibbonPanel(DanBIMRibbon.RibbonTabName, PanelName);
        
        if (panel == null) return;
        
        // Auto Classify button
        PushButtonData autoClassifyBtn = new PushButtonData(
            "AutoClassify",
            "Auto\nClassify",
            typeof(App).Assembly.Location,
            typeof(AutoClassifyCommand).FullName);
        
        autoClassifyBtn.ToolTip = "Automatically classify selected elements using BIM7AA codes";
        autoClassifyBtn.LongDescription = "Analyzes selected Revit elements and assigns appropriate BIM7AA classification codes based on element type and properties.";
        
        PushButton? btn1 = panel.AddItem(autoClassifyBtn) as PushButton;
        if (btn1 != null)
        {
            btn1.LargeImage = IconProvider.GetLargeIcon(IconProvider.AutoClassify);
            btn1.Image = IconProvider.GetSmallIcon(IconProvider.AutoClassify);
        }
        
        // Validate Codes button
        PushButtonData validateBtn = new PushButtonData(
            "ValidateCodes",
            "Validate\nCodes",
            typeof(App).Assembly.Location,
            typeof(ValidateCodesCommand).FullName);
        
        validateBtn.ToolTip = "Validate BIM7AA codes on selected elements";
        validateBtn.LongDescription = "Checks that all selected elements have valid BIM7AA classification codes and reports any missing or invalid codes.";
        
        PushButton? btn2 = panel.AddItem(validateBtn) as PushButton;
        if (btn2 != null)
        {
            btn2.LargeImage = IconProvider.GetLargeIcon(IconProvider.ValidateCodes);
            btn2.Image = IconProvider.GetSmallIcon(IconProvider.ValidateCodes);
        }
        
        // Add separator
        panel.AddSeparator();
        
        // Add pull-down button for additional BIM7AA tools
        PulldownButtonData pullDownData = new PulldownButtonData(
            "BIM7AATools",
            "More Tools");
        
        PulldownButton? pullDown = panel.AddItem(pullDownData) as PulldownButton;
        
        if (pullDown != null)
        {
            pullDown.ToolTip = "Additional BIM7AA classification tools";
            pullDown.LargeImage = IconProvider.GetLargeIcon(IconProvider.ExportCodes);
            pullDown.Image = IconProvider.GetSmallIcon(IconProvider.ExportCodes);
            
            // Add sub-buttons
            PushButtonData exportCodes = new PushButtonData(
                "ExportCodes",
                "Export Codes",
                typeof(App).Assembly.Location,
                "DanBIMTools.Commands.BIM7AA.ExportCodesCommand");
            
            var exportBtn = pullDown.AddPushButton(exportCodes);
            if (exportBtn != null)
            {
                exportBtn.Image = IconProvider.GetSmallIcon(IconProvider.ExportCodes);
            }
        }
    }
}
