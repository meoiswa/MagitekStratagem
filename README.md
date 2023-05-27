# Tobii Plugin for Dalamud

## Experimental Tobii integration for FFXIV

You will need to supply your own, as licensing won't allow me to distribute:
 - lib/tobii_gameintegration_x64.dll
 - lib/tobii_gameintegration_x86.dll
 - lib/Tobii.GameIntegration.Net.dll

## TODO
 - Rename the plugin to something more generalized.
 - Implement a generalized IPositionalService that can support other types of positional tracking.
   - Refactor TobiiService to implement IPositionalService.
   - Implement loading of IPositionalServices on runtime.
 - Modify Closest Match detection to use rays to detect hitboxes.
 - Refactor `TobiiPlugin.Update` to not rely on `ImGui.GetIO()` as this may cause issues down the line.

# Special Thanks
 - Wintermute for the Highlight GameObject sig, and plenty of guidance.
 - Avafloww for the Tab Targeting sigs, that somehow still work despite all the patches.
