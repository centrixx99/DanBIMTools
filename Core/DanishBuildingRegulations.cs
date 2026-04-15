using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace DanBIMTools.Core
{
    /// <summary>
    /// Unified database for Danish Building Regulations (BR18) data.
    /// Loads fire, acoustic, and energy requirements.
    /// </summary>
    public class DanishBuildingRegulations
    {
        private static DanishBuildingRegulations? _instance;
        private FireRegulationsData? _fireData;
        private AcousticRegulationsData? _acousticData;
        private EnergyRegulationsData? _energyData;

        public static DanishBuildingRegulations Load()
        {
            if (_instance != null) return _instance;

            _instance = new DanishBuildingRegulations();
            
            string dataPath = Path.Combine(
                Path.GetDirectoryName(typeof(DanishBuildingRegulations).Assembly.Location) ?? "",
                "Data");

            // Load fire regulations
            string firePath = Path.Combine(dataPath, "br18-fire-ratings.json");
            if (File.Exists(firePath))
            {
                string json = File.ReadAllText(firePath);
                _instance._fireData = JsonConvert.DeserializeObject<FireRegulationsData>(json);
            }

            // Load acoustic regulations
            string acousticPath = Path.Combine(dataPath, "br18-acoustic.json");
            if (File.Exists(acousticPath))
            {
                string json = File.ReadAllText(acousticPath);
                _instance._acousticData = JsonConvert.DeserializeObject<AcousticRegulationsData>(json);
            }

            // Load energy regulations
            string energyPath = Path.Combine(dataPath, "br18-energy.json");
            if (File.Exists(energyPath))
            {
                string json = File.ReadAllText(energyPath);
                _instance._energyData = JsonConvert.DeserializeObject<EnergyRegulationsData>(json);
            }

            return _instance;
        }

        // Fire Regulations Queries
        public List<FireResistanceRequirement> GetFireRequirements(string elementType)
        {
            if (_fireData?.ElementFireRequirements == null)
                return new List<FireResistanceRequirement>();

            var requirements = new List<FireResistanceRequirement>();
            
            // Check element type mappings
            foreach (var kvp in _fireData.ElementFireRequirements)
            {
                if (elementType.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    requirements.Add(new FireResistanceRequirement
                    {
                        ElementType = kvp.Key,
                        RequiredRating = kvp.Value.RequiredRating,
                        Notes = kvp.Value.Notes
                    });
                }
            }

            return requirements;
        }

        public bool IsValidFireRating(string rating)
        {
            if (_fireData?.Regulations == null) return false;

            foreach (var reg in _fireData.Regulations)
            {
                if (reg.FireResistanceRatings != null)
                {
                    if (reg.FireResistanceRatings.Any(r => r.Code.Equals(rating, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
            }

            return false;
        }

        // Acoustic Regulations Queries
        public AcousticRequirement? GetAcousticRequirement(string elementType, string scenario)
        {
            if (_acousticData?.Regulations == null) return null;

            foreach (var reg in _acousticData.Regulations)
            {
                if (reg.Requirements != null)
                {
                    // Check for specific element type requirements
                    if (_acousticData.ElementAcousticRequirements?.TryGetValue(elementType, out var elemReq) == true)
                    {
                        return new AcousticRequirement
                        {
                            Rw = elemReq.Rw,
                            LnW = elemReq.LnW,
                            Notes = elemReq.Notes
                        };
                    }
                }
            }

            return null;
        }

        public Dictionary<string, ElementAcousticRequirement> GetAllAcousticRequirements()
        {
            return _acousticData?.ElementAcousticRequirements ?? new Dictionary<string, ElementAcousticRequirement>();
        }

        // Energy Regulations Queries
        public EnergyRequirement? GetEnergyRequirement(string elementType)
        {
            if (_energyData?.ElementEnergyRequirements == null) return null;

            // Normalize element type name
            string normalized = elementType.ToLowerInvariant()
                .Replace("walls", "ydervæg")
                .Replace("wall", "ydervæg")
                .Replace("roofs", "tag")
                .Replace("roof", "tag")
                .Replace("floors", "gulve")
                .Replace("floor", "gulv")
                .Replace("windows", "vinduer")
                .Replace("window", "vindue")
                .Replace("doors", "døre")
                .Replace("door", "dør");

            foreach (var kvp in _energyData.ElementEnergyRequirements)
            {
                if (normalized.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    kvp.Key.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return new EnergyRequirement
                    {
                        ElementType = kvp.Key,
                        MaxUValue = kvp.Value.U_max,
                        Notes = kvp.Value.Notes
                    };
                }
            }

            // Check U-value requirements
            if (_energyData.Regulations != null)
            {
                foreach (var reg in _energyData.Regulations)
                {
                    if (reg.Requirements != null)
                    {
                        var matchingReq = reg.Requirements.FirstOrDefault(r => 
                            elementType.IndexOf(r.Element ?? "", StringComparison.OrdinalIgnoreCase) >= 0);
                        
                        if (matchingReq != null)
                        {
                            return new EnergyRequirement
                            {
                                ElementType = matchingReq.Element ?? elementType,
                                MaxUValue = $"{matchingReq.MaxUValue} {matchingReq.Unit}",
                                Notes = matchingReq.Notes
                            };
                        }
                    }
                }
            }

            return null;
        }

        public List<EnergyRequirement> GetAllEnergyRequirements()
        {
            var results = new List<EnergyRequirement>();
            
            if (_energyData?.ElementEnergyRequirements != null)
            {
                foreach (var kvp in _energyData.ElementEnergyRequirements)
                {
                    results.Add(new EnergyRequirement
                    {
                        ElementType = kvp.Key,
                        MaxUValue = kvp.Value.U_max,
                        Notes = kvp.Value.Notes
                    });
                }
            }

            return results;
        }

        public double? GetMaxUValueForElement(string elementType)
        {
            var req = GetEnergyRequirement(elementType);
            if (req?.MaxUValue == null) return null;

            // Parse "0.18 W/m²K" format
            var parts = req.MaxUValue.Split(' ');
            if (parts.Length >= 1 && double.TryParse(parts[0], out double value))
            {
                return value;
            }

            return null;
        }
    }

    // Data Models
    public class FireRegulationsData
    {
        public string? Version { get; set; }
        public string? Description { get; set; }
        public List<FireRegulation>? Regulations { get; set; }
        public Dictionary<string, ElementFireRequirement>? ElementFireRequirements { get; set; }
    }

    public class FireRegulation
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<string>? AppliesTo { get; set; }
        public List<FireBuildingClass>? Requirements { get; set; }
        public List<FireResistanceRating>? FireResistanceRatings { get; set; }
    }

    public class FireBuildingClass
    {
        public string? BuildingClass { get; set; }
        public int MaxHeightMeters { get; set; }
        public int MinFireResistanceMinutes { get; set; }
        public string? Notes { get; set; }
    }

    public class FireResistanceRating
    {
        public string? Code { get; set; }
        public int IntegrityMinutes { get; set; }
        public int InsulationMinutes { get; set; }
        public List<string>? UseCases { get; set; }
    }

    public class ElementFireRequirement
    {
        public string? RequiredRating { get; set; }
        public string? Notes { get; set; }
    }

    public class FireResistanceRequirement
    {
        public string? ElementType { get; set; }
        public string? RequiredRating { get; set; }
        public string? Notes { get; set; }
    }

    public class AcousticRegulationsData
    {
        public string? Version { get; set; }
        public string? Description { get; set; }
        public List<AcousticRegulation>? Regulations { get; set; }
        public AcousticRatings? AcousticRatings { get; set; }
        public Dictionary<string, ElementAcousticRequirement>? ElementAcousticRequirements { get; set; }
    }

    public class AcousticRegulation
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<string>? AppliesTo { get; set; }
        public List<AcousticScenarioRequirement>? Requirements { get; set; }
    }

    public class AcousticScenarioRequirement
    {
        public string? Scenario { get; set; }
        public int? AirborneSoundInsulationRw { get; set; }
        public int? ImpactSoundLevelLn { get; set; }
        public int? MinValue { get; set; }
        public string? FieldMeasurementRequirement { get; set; }
    }

    public class AcousticRatings
    {
        public RwRating? Rw { get; set; }
        public LnWRating? LnW { get; set; }
    }

    public class RwRating
    {
        public string? Description { get; set; }
        public string? Unit { get; set; }
        public bool HigherIsBetter { get; set; }
        public Dictionary<string, int>? TypicalValues { get; set; }
    }

    public class LnWRating
    {
        public string? Description { get; set; }
        public string? Unit { get; set; }
        public bool HigherIsBetter { get; set; }
        public Dictionary<string, int>? TypicalValues { get; set; }
    }

    public class ElementAcousticRequirement
    {
        public string? Rw { get; set; }
        public string? LnW { get; set; }
        public string? Notes { get; set; }
    }

    public class AcousticRequirement
    {
        public string? Rw { get; set; }
        public string? LnW { get; set; }
        public string? Notes { get; set; }
    }

    public class EnergyRegulationsData
    {
        public string? Version { get; set; }
        public string? Description { get; set; }
        public List<EnergyRegulation>? Regulations { get; set; }
        public Dictionary<string, ElementEnergyRequirement>? ElementEnergyRequirements { get; set; }
        public Dictionary<string, CertificationLevel>? CertificationLevels { get; set; }
    }

    public class EnergyRegulation
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<BuildingTypeEnergy>? BuildingTypes { get; set; }
        public List<EnergyRequirementDetail>? Requirements { get; set; }
    }

    public class BuildingTypeEnergy
    {
        public string? Type { get; set; }
        public double MaxPrimaryEnergy { get; set; }
        public string? Unit { get; set; }
        public string? Notes { get; set; }
    }

    public class EnergyRequirementDetail
    {
        public string? Element { get; set; }
        public double? MaxUValue { get; set; }
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
        public string? Unit { get; set; }
        public string? Notes { get; set; }
    }

    public class ElementEnergyRequirement
    {
        public string? U_max { get; set; }
        public string? Notes { get; set; }
        public Dictionary<string, string>? TypicalValues { get; set; }
    }

    public class CertificationLevel
    {
        public string? Description { get; set; }
        public double PrimaryEnergy { get; set; }
        public string? SuitableFor { get; set; }
    }

    public class EnergyRequirement
    {
        public string? ElementType { get; set; }
        public string? MaxUValue { get; set; }
        public string? Notes { get; set; }
    }
}
