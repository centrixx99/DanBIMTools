using System;
using Autodesk.Revit.UI;
using DanBIMTools.Commands.General;
using DanBIMTools.Ribbon;

namespace DanBIMTools.Ribbon.Panels;

/// <summary>
/// General tools panel with IKT checking and data validation.
/// </summary>
public static class ToolsPanel
{
    public const string PanelName = "Tools";
    
    public static void Create(UIControlledApplication application)
    {
        RibbonPanel? panel = application.CreateRibbonPanel(DanBIMRibbon.RibbonTabName, PanelName);
        
        if (panel == null) return;
        
        // IKT Checker button
        PushButtonData iktBtn = new PushButtonData(
            "IKTChecker",
            "IKT\nChecker",
            typeof(App).Assembly.Location,
            typeof(IKTCheckerCommand).FullName);
        
        iktBtn.ToolTip = "Check IKT requirements compliance";
        iktBtn.LongDescription = "Validates that model elements meet IKT (Information and Communication Technology) requirements for digital delivery.";
        
        PushButton? btn1 = panel.AddItem(iktBtn) as PushButton;
        if (btn1 != null)
        {
            btn1.LargeImage = IconProvider.GetLargeIcon(IconProvider.IKTChecker);
            btn1.Image = IconProvider.GetSmallIcon(IconProvider.IKTChecker);
        }
        
        // BR18 Validator button
        PushButtonData br18Btn = new PushButtonData(
            "BR18Validator",
            "BR18\nValidator",
            typeof(App).Assembly.Location,
            typeof(BR18ValidatorCommand).FullName);
        
        br18Btn.ToolTip = "Check BR18 Building Regulations compliance";
        br18Btn.LongDescription = "Validates elements against Danish Building Regulations (BR18) for fire, acoustic, and energy requirements.";
        
        PushButton? btnBR18 = panel.AddItem(br18Btn) as PushButton;
        if (btnBR18 != null)
        {
            btnBR18.LargeImage = IconProvider.GetLargeIcon(IconProvider.BR18Validator);
            btnBR18.Image = IconProvider.GetSmallIcon(IconProvider.BR18Validator);
        }
        
        // Missing Data button
        PushButtonData missingDataBtn = new PushButtonData(
            "MissingData",
            "Missing\nData",
            typeof(App).Assembly.Location,
            typeof(MissingDataCommand).FullName);
        
        missingDataBtn.ToolTip = "Find elements with missing required data";
        missingDataBtn.LongDescription = "Identifies elements that are missing required parameters or property values.";
        
        PushButton? btn2 = panel.AddItem(missingDataBtn) as PushButton;
        if (btn2 != null)
        {
            btn2.LargeImage = IconProvider.GetLargeIcon(IconProvider.MissingData);
            btn2.Image = IconProvider.GetSmallIcon(IconProvider.MissingData);
        }
        
        // Spec Generator button
        PushButtonData specBtn = new PushButtonData(
            "SpecGenerator",
            "Spec\nGenerator",
            typeof(App).Assembly.Location,
            typeof(SpecGeneratorCommand).FullName);
        
        specBtn.ToolTip = "Generate specification documents";
        specBtn.LongDescription = "Creates specification documents from model data, including material lists and component schedules.";
        
        PushButton? btn3 = panel.AddItem(specBtn) as PushButton;
        if (btn3 != null)
        {
            btn3.LargeImage = IconProvider.GetLargeIcon(IconProvider.SpecGenerator);
            btn3.Image = IconProvider.GetSmallIcon(IconProvider.SpecGenerator);
        }
        
        // Add separator
        panel.AddSeparator();
        
        // Add stacked buttons for quick tools
        PushButtonData quickCheck = new PushButtonData(
            "QuickCheck",
            "Quick\nCheck",
            typeof(App).Assembly.Location,
            "DanBIMTools.Commands.General.QuickCheckCommand");
        
        PushButtonData exportReport = new PushButtonData(
            "ExportReport",
            "Export\nReport",
            typeof(App).Assembly.Location,
            "DanBIMTools.Commands.General.ExportReportCommand");
        
        var stackedItems = panel.AddStackedItems(quickCheck, exportReport);
        
        // Apply icons to stacked buttons if possible
        if (stackedItems != null && stackedItems.Count >= 2)
        {
            if (stackedItems[0] is PushButton quickBtn)
            {
                quickBtn.Image = IconProvider.GetSmallIcon(IconProvider.QuickCheck);
            }
            if (stackedItems[1] is PushButton exportBtn)
            {
                exportBtn.Image = IconProvider.GetSmallIcon(IconProvider.ExportReport);
            }
        }
    }
}
