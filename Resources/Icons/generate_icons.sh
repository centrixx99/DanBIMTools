#!/bin/bash
# Generate placeholder icons for DanBIMTools Revit add-in

ICON_DIR="$(dirname "$0")"
cd "$ICON_DIR"

create_icon() {
    local name=$1
    local color=$2
    local size=$3
    local text=$4
    
    convert -size "${size}x${size}" "xc:${color}" \
        -pointsize $((size/2)) \
        -fill white \
        -gravity center \
        -font "Arial-Bold" \
        -annotate +0+0 "$text" \
        "${name}_${size}.png" 2>/dev/null || \
    convert -size "${size}x${size}" "xc:${color}" \
        -pointsize $((size/2)) \
        -fill white \
        -gravity center \
        -annotate +0+0 "$text" \
        "${name}_${size}.png"
}

echo "Generating DanBIMTools icons..."

# BIM7AA Panel Icons (Blue)
create_icon "bim7aa_autoclassify" "#007ACC" 16 "A"
create_icon "bim7aa_autoclassify" "#007ACC" 32 "A"
create_icon "bim7aa_validate" "#007ACC" 16 "V"
create_icon "bim7aa_validate" "#007ACC" 32 "V"
create_icon "bim7aa_export" "#007ACC" 16 "E"
create_icon "bim7aa_export" "#007ACC" 32 "E"

# HVAC Panel Icons (Green)
create_icon "hvac_ductsizing" "#28A745" 16 "D"
create_icon "hvac_ductsizing" "#28A745" 32 "D"
create_icon "hvac_clash" "#28A745" 16 "C"
create_icon "hvac_clash" "#28A745" 32 "C"
create_icon "hvac_insulation" "#28A745" 16 "I"
create_icon "hvac_insulation" "#28A745" 32 "I"

# Tools Panel Icons (Purple)
create_icon "tools_ikt" "#6F42C1" 16 "K"
create_icon "tools_ikt" "#6F42C1" 32 "K"
create_icon "tools_br18" "#6F42C1" 16 "B"
create_icon "tools_br18" "#6F42C1" 32 "B"
create_icon "tools_missing" "#6F42C1" 16 "M"
create_icon "tools_missing" "#6F42C1" 32 "M"
create_icon "tools_spec" "#6F42C1" 16 "S"
create_icon "tools_spec" "#6F42C1" 32 "S"
create_icon "tools_quick" "#6F42C1" 16 "Q"
create_icon "tools_quick" "#6F42C1" 32 "Q"
create_icon "tools_export" "#6F42C1" 16 "R"
create_icon "tools_export" "#6F42C1" 32 "R"

# Chatbot Icon (Orange)
create_icon "chatbot" "#FD7E14" 16 "?"
create_icon "chatbot" "#FD7E14" 32 "?"

echo "Created $(ls -1 *.png 2>/dev/null | wc -l) icons"
echo "Icons:"
ls -la *.png 2>/dev/null | awk '{print $9, $5}'
