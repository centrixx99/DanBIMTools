using System;
using Autodesk.Revit.DB;

namespace DanBIMTools.Core
{
    /// <summary>
    /// Safe unit conversion utilities for Revit internal units (feet).
    /// Revit stores all length/area/volume in internal feet regardless of display settings.
    /// </summary>
    public static class UnitHelper
    {
        // Revit internal unit constants
        public const double FeetToMeters = 0.3048;
        public const double SqFeetToSqMeters = 0.09290304;
        public const double CubicFeetToCubicMeters = 0.028316846592;
        public const double FeetToMm = 304.8;
        public const double LpsToCfs = 0.035314667; // L/s to ft³/s

        /// <summary>
        /// Safely gets a double parameter value. Returns null if param missing, empty, or wrong storage type.
        /// </summary>
        public static double? GetDoubleValue(Element elem, string paramName)
        {
            Parameter param = elem.LookupParameter(paramName);
            if (param == null && elem is FamilyInstance fi)
                param = fi.Symbol?.LookupParameter(paramName);

            if (param == null || !param.HasValue)
                return null;

            if (param.StorageType == StorageType.Double)
                return param.AsDouble();

            // Try parsing string values
            if (param.StorageType == StorageType.String)
            {
                string strVal = param.AsString();
                if (double.TryParse(strVal, out double parsed))
                    return parsed;
            }

            return null;
        }

        /// <summary>
        /// Gets a parameter value as string (uses AsValueString for display).
        /// </summary>
        public static string? GetStringValue(Element elem, string paramName)
        {
            Parameter param = elem.LookupParameter(paramName);
            if (param == null && elem is FamilyInstance fi)
                param = fi.Symbol?.LookupParameter(paramName);

            if (param == null || !param.HasValue)
                return null;

            try
            {
                return param.AsValueString();
            }
            catch
            {
                if (param.StorageType == StorageType.String)
                    return param.AsString();
                if (param.StorageType == StorageType.Double)
                    return param.AsDouble().ToString("F2");
                if (param.StorageType == StorageType.Integer)
                    return param.AsInteger().ToString();
                return null;
            }
        }

        /// <summary>
        /// Converts internal feet² to m².
        /// </summary>
        public static double AreaToSqMeters(double internalAreaFt2) => internalAreaFt2 * SqFeetToSqMeters;

        /// <summary>
        /// Converts internal feet to m.
        /// </summary>
        public static double LengthToMeters(double internalLengthFt) => internalLengthFt * FeetToMeters;

        /// <summary>
        /// Converts internal feet to mm.
        /// </summary>
        public static double LengthToMm(double internalLengthFt) => internalLengthFt * FeetToMm;

        /// <summary>
        /// Converts mm to internal feet.
        /// </summary>
        public static double MmToFeet(double mm) => mm / FeetToMm;

        /// <summary>
        /// Safely gets an area parameter value converted to m².
        /// Returns 0 if parameter missing or wrong type.
        /// </summary>
        public static double GetAreaSqMeters(Element elem, string paramName = "Area")
        {
            double? val = GetDoubleValue(elem, paramName);
            return val.HasValue ? AreaToSqMeters(val.Value) : 0;
        }

        /// <summary>
        /// Safely gets a length parameter value converted to meters.
        /// Returns 0 if parameter missing or wrong type.
        /// </summary>
        public static double GetLengthMeters(Element elem, string paramName = "Length")
        {
            double? val = GetDoubleValue(elem, paramName);
            return val.HasValue ? LengthToMeters(val.Value) : 0;
        }

        /// <summary>
        /// Safely gets a thickness parameter value converted to mm.
        /// </summary>
        public static double GetThicknessMm(Element elem, string paramName)
        {
            double? val = GetDoubleValue(elem, paramName);
            return val.HasValue ? LengthToMm(val.Value) : 0;
        }
    }
}