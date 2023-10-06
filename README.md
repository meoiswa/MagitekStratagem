# Magitek Stratagem - Tobii Game Integration Plugin for Dalamud

## Experimental Tobii integration for FFXIV

To use this plugin with a Tobii tracker, you *must* have Tobii GameHub installed.

## TODO
 - Figure out a way to distinguish when `SelectTabTargetXXXX` is called by Enemy vs Soft targetting on keyboard.
 - Rename the plugin to something more generalized.
 - Implement a generalized ITrackerService that can support other types of positional tracking.
   - ~~Refactor TobiiService to implement ITrackerService.~~
   - Implement loading of ITrackerServices on runtime.
 - Refactor `MagitekStratagemPlugin.Update` to not rely on `ImGui.GetIO()` as this may cause issues down the line.

# Special Thanks
 - Wintermute for the Highlight GameObject sig, and plenty of guidance, in particular with the Ray casting.
 - Avafloww for the Tab Targeting sigs, that somehow still work despite all the patches.
 - NotNite for fine-tuning sigs, and helping improve the targetting overrides.
