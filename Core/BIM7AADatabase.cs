using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace DanBIMTools.Core
{
    /// <summary>
    /// BIM7AA classification database for Danish building codes.
    /// Thread-safe singleton using Lazy{T}.
    /// </summary>
    public class BIM7AADatabase
    {
        private static readonly Lazy<BIM7AADatabase> _lazy =
            new Lazy<BIM7AADatabase>(() => LoadInternal());

        public static BIM7AADatabase Instance => _lazy.Value;

        public static BIM7AADatabase Load() => Instance;

        private Dictionary<string, BIM7AACode> _codes = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, List<string>> _categoryMappings = new(StringComparer.OrdinalIgnoreCase);

        private static BIM7AADatabase LoadInternal()
        {
            var db = new BIM7AADatabase();
            
            string dataPath = Path.Combine(
                Path.GetDirectoryName(typeof(BIM7AADatabase).Assembly.Location) ?? "",
                "Data", "bim7aa-codes.json");

            if (File.Exists(dataPath))
            {
                try
                {
                    string json = File.ReadAllText(dataPath);
                    var data = JsonConvert.DeserializeObject<BIM7AAData>(json);
                    if (data?.Codes != null)
                    {
                        foreach (var code in data.Codes)
                        {
                            db._codes[code.Code] = code;
                        }
                    }
                    if (data?.CategoryMappings != null)
                    {
                        db._categoryMappings = new Dictionary<string, List<string>>(data.CategoryMappings, StringComparer.OrdinalIgnoreCase);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"BIM7AA: Failed to load data file: {ex.Message}");
                }
            }

            // Fallback if file missing or failed to load
            if (db._codes.Count == 0)
            {
                db.LoadDefaultCodes();
            }

            return db;
        }

        private void LoadDefaultCodes()
        {
            _categoryMappings = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Walls"] = new() { "23" },
                ["Floors"] = new() { "28" },
                ["Roofs"] = new() { "29" },
                ["Structural Columns"] = new() { "32" },
                ["Structural Framing"] = new() { "32" },
                ["Doors"] = new() { "26" },
                ["Windows"] = new() { "27" },
                ["Generic Models"] = new() { "90" },
                ["Mechanical Equipment"] = new() { "60" },
                ["Ducts"] = new() { "62" },
                ["Pipes"] = new() { "65" },
                ["Electrical Equipment"] = new() { "70" },
                ["Lighting Fixtures"] = new() { "71" },
                ["Cable Trays"] = new() { "74" },
                ["Curtain Panels"] = new() { "24" },
                ["Stairs"] = new() { "25" },
                ["Ramps"] = new() { "25" }
            };

            _codes["2300"] = new BIM7AACode { Code = "2300", Name = "Vægge", Description = "Alle typer vægge", Level = 2 };
            _codes["2310"] = new BIM7AACode { Code = "2310", Name = "Ydervægge", Description = "Udvendige vægge", Level = 3 };
            _codes["2320"] = new BIM7AACode { Code = "2320", Name = "Indervægge", Description = "Indvendige vægge", Level = 3 };
            _codes["2800"] = new BIM7AACode { Code = "2800", Name = "Etagedæk", Description = "Alle etagedæk", Level = 2 };
            _codes["2810"] = new BIM7AACode { Code = "2810", Name = "Betondæk", Description = "Støbte betondæk", Level = 3 };
            _codes["2900"] = new BIM7AACode { Code = "2900", Name = "Tagkonstruktioner", Description = "Alle tagtyper", Level = 2 };
            _codes["3200"] = new BIM7AACode { Code = "3200", Name = "Søjler", Description = "Bærende søjler", Level = 2 };
            _codes["3210"] = new BIM7AACode { Code = "3210", Name = "Bjælker", Description = "Bærende bjælker", Level = 3 };
            _codes["6000"] = new BIM7AACode { Code = "6000", Name = "Ventilation", Description = "Ventilationsanlæg", Level = 2 };
            _codes["6200"] = new BIM7AACode { Code = "6200", Name = "Kanaler", Description = "Ventilationskanaler", Level = 2 };
            _codes["6500"] = new BIM7AACode { Code = "6500", Name = "VVS", Description = "VVS-installationer", Level = 2 };
            _codes["7000"] = new BIM7AACode { Code = "7000", Name = "El", Description = "El-installationer", Level = 2 };
        }

        public string? GetCodeForCategory(string categoryName)
        {
            if (_categoryMappings.TryGetValue(categoryName, out var codes))
            {
                return codes.FirstOrDefault();
            }
            return null;
        }

        public string? GetCodeForFamily(string familyName)
        {
            foreach (var kvp in _codes)
            {
                if (familyName.IndexOf(kvp.Value.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        public BIM7AACode? GetCode(string code)
        {
            return _codes.TryGetValue(code, out var result) ? result : null;
        }

        public IEnumerable<BIM7AACode> GetAllCodes()
        {
            return _codes.Values.OrderBy(c => c.Code);
        }

        public bool IsValidCode(string code)
        {
            return _codes.ContainsKey(code);
        }
    }

    public class BIM7AACode
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int Level { get; set; }
    }

    public class BIM7AAData
    {
        public List<BIM7AACode> Codes { get; set; } = new();
        public Dictionary<string, List<string>> CategoryMappings { get; set; } = new();
    }
}