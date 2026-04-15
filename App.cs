using System;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using DanBIMTools.Ribbon;

namespace DanBIMTools;

/// <summary>
/// Main Revit application entry point for DanBIM Tools add-in.
/// </summary>
public class App : IExternalApplication
{
    public static App? Instance { get; private set; }
    public UIControlledApplication? ControlledApplication { get; private set; }
    
    public Result OnStartup(UIControlledApplication application)
    {
        try
        {
            Instance = this;
            ControlledApplication = application;
            
            // Create ribbon
            DanBIMRibbon.CreateRibbon(application);
            
            // No startup dialog — users don't need a modal popup every launch
            
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DanBIM Tools init failed: {ex}");
            TaskDialog.Show("DanBIM Tools Error", 
                $"Failed to initialize DanBIM Tools:\n{ex.Message}");
            return Result.Failed;
        }
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        Instance = null;
        ControlledApplication = null;
        return Result.Succeeded;
    }
}
