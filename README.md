# DanBIMTools 🇩🇰

**Danish BIM Tools for Autodesk Revit** — Classification, validation, and compliance tools built for Danish construction professionals.

[![Revit 2026](https://img.shields.io/badge/Revit-2026-blue.svg)]()
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple.svg)]()
[![License](https://img.shields.io/badge/license-MIT-green.svg)]()

## ✨ Features

### 📋 BIM7AA Panel
- **Auto Classify** — Suggests BIM7AA type codes based on element category and family
- **Validate Codes** — Checks elements for valid BIM7AA classification codes
- **Export Codes** — Export classification reports to CSV

### 🌡️ HVAC Panel
- **Duct Sizing** — Calculate optimal duct dimensions from airflow (L/s) with Danish standard sizes
- **Clash Preview** — Preview potential clashes between ducts and structural elements
- **Insulation Check** — Validate duct insulation against BR18 requirements (25-100mm)

### 🔧 Tools Panel
- **IKT Checker** — Validate model elements against Danish IKT delivery requirements
- **BR18 Validator** — Check fire ratings, acoustics, and energy compliance against BR18
- **Missing Data** — Find elements with missing required parameters (severity-ranked)
- **Spec Generator** — Generate arbejdsbeskrivelser and materialelister from model data
- **Quick Check** — One-click model health overview
- **Export Report** — Full validation report as interactive HTML

### 💬 DanBIM Assistant
Natural language command interface for quick access to all tools.

## 🚀 Installation

### Quick Install (Recommended)

1. Download `DanBIMTools-Installer.zip` from [Releases](../../releases)
2. Right-click `Install.ps1` → **Run with PowerShell**
3. Restart Revit — look for the **DanBIM** ribbon tab

### Manual Install

```powershell
# Build from source
.\build.ps1 -Configuration Release

# Copy to Revit addins folder
Copy-Item .\bin\Release\net8.0-windows\* "$env:APPDATA\Autodesk\Revit\Addins\2026\" -Recurse
```

### Uninstall

```powershell
Remove-Item "$env:APPDATA\Autodesk\Revit\Addins\2026\DanBIMTools*"
```

## 📁 Project Structure

```
DanBIMTools/
├── App.cs                        # IExternalApplication entry point
├── DanBIMTools.csproj            # .NET 8.0 project (auto-detects Revit)
├── DanBIMTools.addin             # Revit add-in manifest
├── Ribbon/
│   ├── DanBIMRibbon.cs           # Ribbon tab + panel creation
│   ├── IconProvider.cs            # Icon name constants
│   └── Panels/
│       ├── BIM7AAPanel.cs
│       ├── HVACPanel.cs
│       └── ToolsPanel.cs
├── Commands/
│   ├── BIM7AA/                    # Classification commands
│   ├── HVAC/                      # Duct sizing, clash, insulation
│   └── General/                   # IKT, BR18, reports, chatbot
├── Core/
│   ├── BIM7AADatabase.cs          # BIM7AA classification data
│   ├── DanishBuildingRegulations.cs # BR18 fire/acoustic/energy data
│   ├── IconHelper.cs              # Embedded resource icon loader
│   └── UnitHelper.cs              # Revit internal unit conversions
├── Data/                          # JSON databases (BR18, BIM7AA, IKT)
└── Resources/Icons/              # 16x16 and 32x32 PNG icons
```

## 🇩🇰 Danish Standards Coverage

| Standard | Coverage |
|----------|----------|
| **BIM7AA v3.2** | 64+ classification codes with Danish names |
| **BR18 Fire (K7-K10)** | Fire resistance ratings per building class |
| **BR18 Acoustics (L1-L4)** | Airborne/impact sound requirements |
| **BR18 Energy** | U-value requirements by element type |
| **IKT Requirements** | Model A/B/P delivery specifications |

## 🛠️ Requirements

- **Autodesk Revit 2026** (also compatible with 2024/2025)
- **Windows 10/11** x64
- **.NET 8.0 SDK** (for building from source)

## 🔧 Building from Source

```powershell
# Auto-detect Revit and build
dotnet build -c Release

# Or specify Revit path
$env:REVIT_API_PATH="C:\Program Files\Autodesk\Revit 2026"
dotnet build -c Release
```

The project auto-detects Revit installations in standard paths.

## 📋 Changelog

### v1.5.0 (2026-04-15)
- **Fixed**: Unit conversion errors in SpecGenerator and ExportReport (ft→m², ft→m)
- **Fixed**: Silent default values in Duct Sizing — now prompts user for airflow
- **Fixed**: Comments field overwrite — now appends calculation notes
- **Fixed**: Thread-unsafe singletons in BIM7AADatabase and DanishBuildingRegulations
- **Fixed**: Startup dialog removed — no more blocking popup on Revit launch
- **Added**: UnitHelper class for safe Revit unit conversions
- **Improved**: Chatbot renamed to "DanBIM Assistant" (no misleading AI branding)

### v1.4.0 (2026-04-06)
- Professional installer system with multi-version support
- Enhanced BIM7AA database (64+ codes from official v3.2)
- Enhanced IKT requirements (Molio A102, BEK 118/119)
- 26 ribbon icons (16x16 + 32x32)

### v1.3.0 - v1.1.0
- BR18 building regulations database
- HVAC tools (duct sizing, clash preview, insulation)
- IKT checker, missing data finder, spec generator
- Initial BIM7AA classification and export

## 📄 License

MIT License — See [LICENSE](LICENSE) for details.

---

*Built for Danish construction. 🇩🇰*
