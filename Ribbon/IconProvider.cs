using System;
using System.Windows.Media.Imaging;
using DanBIMTools.Core;

namespace DanBIMTools.Ribbon
{
    /// <summary>
    /// Provides icons for ribbon buttons. Falls back gracefully if icons are missing.
    /// </summary>
    public static class IconProvider
    {
        // Icon names for each button
        public const string Chatbot = "chatbot";
        
        // BIM7AA Panel
        public const string AutoClassify = "bim7aa_autoclassify";
        public const string ValidateCodes = "bim7aa_validate";
        public const string ExportCodes = "bim7aa_export";
        
        // HVAC Panel
        public const string DuctSizing = "hvac_ductsizing";
        public const string ClashPreview = "hvac_clash";
        public const string InsulationCheck = "hvac_insulation";
        
        // Tools Panel
        public const string IKTChecker = "tools_ikt";
        public const string BR18Validator = "tools_br18";
        public const string MissingData = "tools_missing";
        public const string SpecGenerator = "tools_spec";
        public const string QuickCheck = "tools_quick";
        public const string ExportReport = "tools_export";

        /// <summary>
        /// Gets an icon by name. Returns null if not found.
        /// </summary>
        public static BitmapImage? GetIcon(string name, int size = 32)
        {
            return IconHelper.LoadIcon(name, size);
        }

        /// <summary>
        /// Gets the large icon (32x32) for a button.
        /// </summary>
        public static BitmapImage? GetLargeIcon(string name)
        {
            return GetIcon(name, 32);
        }

        /// <summary>
        /// Gets the small icon (16x16) for a button.
        /// </summary>
        public static BitmapImage? GetSmallIcon(string name)
        {
            return GetIcon(name, 16);
        }
    }
}
