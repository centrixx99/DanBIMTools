using System;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using DanBIMTools.Ribbon.Panels;

namespace DanBIMTools.Ribbon;

/// <summary>
/// Main ribbon creation for DanBIM Tools.
/// </summary>
public static class DanBIMRibbon
{
    public const string RibbonTabName = "DanBIM";
    
    public static void CreateRibbon(UIControlledApplication application)
    {
        try
        {
            // Create ribbon tab
            application.CreateRibbonTab(RibbonTabName);
            
            // Create panels
            BIM7AAPanel.Create(application);
            HVACPanel.Create(application);
            ToolsPanel.Create(application);
            
            // Add chatbot button to main tab
            AddChatbotButton(application);
        }
        catch (Exception ex)
        {
            TaskDialog.Show("DanBIM Ribbon Error", 
                $"Failed to create ribbon:\n{ex.Message}");
        }
    }
    
    private static void AddChatbotButton(UIControlledApplication application)
    {
        // Add chatbot button to a dedicated panel
        RibbonPanel? chatbotPanel = application.CreateRibbonPanel(RibbonTabName, "AI Assistant");
        
        if (chatbotPanel != null)
        {
            PushButtonData chatbotBtn = new PushButtonData(
                "ChatbotButton",
                "DanBIM\nChat",
                typeof(App).Assembly.Location,
                "DanBIMTools.Commands.General.ChatbotCommand");
            
            chatbotBtn.ToolTip = "Open the DanBIM AI chatbot assistant";
            chatbotBtn.LongDescription = "Get help with BIM7AA codes, HVAC calculations, and general Revit tasks.";
            
            PushButton? btn = chatbotPanel.AddItem(chatbotBtn) as PushButton;
            if (btn != null)
            {
                btn.LargeImage = IconProvider.GetLargeIcon(IconProvider.Chatbot);
                btn.Image = IconProvider.GetSmallIcon(IconProvider.Chatbot);
            }
        }
    }
}
