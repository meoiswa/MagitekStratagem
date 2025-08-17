# Magitek Stratagem - Tobii Game Integration Plugin for Dalamud

## Eye and Head tracking integration for FFXIV

- To use this plugin with a Tobii tracker, you *must* have Tobii GameHub installed.
- To use this plugin with Opentrack, you *must* use the "UDP over network" configured to send data to `127.0.0.1:4242`

## Integrating with MagitekStratagem

 - New in 1.0.1.0! MagitekStratagem creates a Shared Data structure for other plugins to access Tracker Data in a simple and performant way.
   - See `MagitekStratagem\Services\sharedData\SharedDataService.cs` for a more detailed explanation.

## TODO
 
 - Refactor `MagitekStratagemPlugin.Update` to not rely on `ImGui.GetIO()` as this may cause issues down the line.

# Special Thanks
 - Wintermute for the Highlight GameObject sig, and plenty of guidance, in particular with the Ray casting.
 - Avafloww for the Tab Targeting sigs, that somehow still work despite all the patches.
 - NotNite for fine-tuning sigs, and helping improve the targetting overrides.
 - Hasselnussbomber for further sigs fine-tuning, and helping correctly differentiate Circle and Tab targetting on Gamepad.
