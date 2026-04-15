using System;
using System.Windows;
using System.Windows.Controls;
using Grid = System.Windows.Controls.Grid;
using TextBox = System.Windows.Controls.TextBox;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace DanBIMTools.Commands.General
{
    [Transaction(TransactionMode.Manual)]
    public class ChatbotCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var chatWindow = new ChatbotWindow(commandData.Application);
                chatWindow.Show();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Fejl", $"Kunne ikke åbne chatbot:\n{ex.Message}");
                return Result.Failed;
            }
        }
    }

    /// <summary>
    /// WPF Chatbot window for DanBIM assistant.
    /// Provides quick navigation to DanBIM tools via natural language commands.
    /// </summary>
    public class ChatbotWindow : Window
    {
        private TextBox _inputBox;
        private TextBox _outputBox;
        private UIApplication _uiApp;

        public ChatbotWindow(UIApplication uiApp)
        {
            _uiApp = uiApp;
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            Title = "DanBIM Assistant";
            Width = 500;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Output area
            _outputBox = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = System.Windows.TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(10),
                Text = "🤖 DanBIM Assistant klar\n\n" +
                       "Kommandoer:\n" +
                       "• 'vælg vægge uden BIM7AA koder'\n" +
                       "• 'tjek isolering på kanaler'\n" +
                       "• 'eksporter materialeliste'\n" +
                       "• 'find døre uden brandklassifikation'\n" +
                       "• 'hjælp'\n\n" +
                       "Skriv din kommando nedenfor:"
            };
            Grid.SetRow(_outputBox, 0);
            grid.Children.Add(_outputBox);

            // Input area
            _inputBox = new TextBox
            {
                Height = 60,
                TextWrapping = System.Windows.TextWrapping.Wrap,
                AcceptsReturn = true,
                Margin = new Thickness(10, 0, 10, 5)
            };
            Grid.SetRow(_inputBox, 1);
            grid.Children.Add(_inputBox);

            // Button panel
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10) };
            
            var sendButton = new Button
            {
                Content = "Send",
                Width = 80,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            sendButton.Click += SendButton_Click;
            buttonPanel.Children.Add(sendButton);

            var clearButton = new Button
            {
                Content = "Ryd",
                Width = 80,
                Height = 30
            };
            clearButton.Click += (s, e) => { _inputBox.Clear(); _outputBox.Clear(); };
            buttonPanel.Children.Add(clearButton);

            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string userInput = _inputBox.Text.Trim();
            if (string.IsNullOrEmpty(userInput)) return;

            _outputBox.AppendText($"\n\n👤 Dig: {userInput}");
            string response = ProcessCommand(userInput);
            _outputBox.AppendText($"\n\n🤖 DanBIM: {response}");
            _outputBox.ScrollToEnd();
            _inputBox.Clear();
        }

        private string ProcessCommand(string input)
        {
            string lowerInput = input.ToLower();

            // Help first
            if (lowerInput.Contains("hjælp") || lowerInput.Contains("help"))
            {
                return "Tilgængelige kommandoer:\n" +
                       "• BIM7AA: klassificer, valider, eksporter\n" +
                       "• HVAC: kanaludregning, isolering, kollision\n" +
                       "• Vælg: elementer efter kriterier\n" +
                       "• Eksport: materialeliste, IFC, rapport\n" +
                       "• IKT: kontrol af modelkrav\n\n" +
                       "Eller brug knapperne på DanBIM ribbon fanen.";
            }

            // BIM7AA commands
            if (lowerInput.Contains("bim7aa") || lowerInput.Contains("bim 7aa"))
            {
                if (lowerInput.Contains("klassificer") || lowerInput.Contains("auto"))
                    return "Kører auto-klassificering...\nVælg elementer og klik 'Auto Classify' på BIM7AA fanen.";
                if (lowerInput.Contains("valider") || lowerInput.Contains("tjek"))
                    return "Validerer BIM7AA koder...\nKlik 'Validate Codes' på BIM7AA fanen for resultatet.";
                if (lowerInput.Contains("eksporter"))
                    return "Eksporterer BIM7AA koder...\nKlik 'Export Codes' på BIM7AA fanen.";
                return "BIM7AA kommandoer: auto-klassificering, validering, eksport.";
            }

            // HVAC commands
            if (lowerInput.Contains("kanal") || lowerInput.Contains("duct") || lowerInput.Contains("ventilation"))
            {
                if (lowerInput.Contains("størrelse") || lowerInput.Contains("udregning"))
                    return "Åbner kanaludregning...\nVælg en kanal og angiv luftmængde i L/s.";
                if (lowerInput.Contains("isolering") || lowerInput.Contains("isolation"))
                    return "Tjekker isolering...\nKlik 'Insulation Check' på HVAC fanen. Kræver ifølge BR18: min 25-50mm afhængig af placering.";
                if (lowerInput.Contains("kollision"))
                    return "Forhåndsviser kollisioner...\nVælg kanaler og klik 'Clash Preview'.";
                return "HVAC kommandoer: kanaludregning, isoleringstjek, kollisionskontrol.";
            }

            // Selection commands
            if (lowerInput.Contains("vælg") || lowerInput.Contains("select"))
            {
                if (lowerInput.Contains("væg") && lowerInput.Contains("uden"))
                    return "Vælger vægge uden BIM7AA koder...\nBrug 'Missing Data' på Tools fanen og filtrer efter BIM7AA_TypeCode.";
                if (lowerInput.Contains("dør") && lowerInput.Contains("brand"))
                    return "Vælger døre uden brandklassifikation...\nBrug 'Missing Data' og søg efter 'Fire Rating'.";
                if (lowerInput.Contains("vindue"))
                    return "Vælger vinduer...\nBrug 'Missing Data' for at se hvilke der mangler parametre.";
                return "Vælg kommando forstået. Specificer elementtype og kriterie.";
            }

            // Export commands
            if (lowerInput.Contains("eksporter") || lowerInput.Contains("export"))
            {
                if (lowerInput.Contains("materiale") || lowerInput.Contains("spec"))
                    return "Genererer materialeliste...\nKlik 'Spec Generator' på Tools fanen.";
                if (lowerInput.Contains("ifc"))
                    return "IFC eksport: Brug Revit's indbyggede IFC eksport (File > Export > IFC).";
                if (lowerInput.Contains("rapport") || lowerInput.Contains("report"))
                    return "Eksporterer rapport...\nKlik 'Export Report' på Tools fanen.";
                return "Eksport kommandoer: materialeliste, IFC, rapport.";
            }

            // IKT commands
            if (lowerInput.Contains("ikt"))
            {
                return "Kører IKT kontrol...\nKlik 'IKT Checker' på Tools fanen for at validere modellen mod bygningsreglementet.";
            }

            return "Beklager, jeg forstod ikke kommandoen.\n" +
                   "Skriv 'hjælp' for at se tilgængelige kommandoer.";
        }
    }
}