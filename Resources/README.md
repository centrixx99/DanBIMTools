# DanBIM Tools Resources

This directory contains icons and resources for the Revit add-in.

## Icons

Icons should be placed in `Icons/` folder:
- 32x32 PNG for large ribbon buttons
- 16x16 PNG for small ribbon buttons
- Naming convention: `{Category}_{Action}_{Size}.png`

Example:
- `BIM7AA_AutoClassify_32.png`
- `BIM7AA_Validate_32.png`
- `HVAC_DuctSizing_32.png`

## Generating Icons

You can generate simple icons using any image editor or use the provided placeholders.
For production, create custom icons matching your company/brand colors.

## Current Status

Icons are currently loaded from embedded resources in the assembly.
