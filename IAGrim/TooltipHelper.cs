using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace EvilsoftCommons {
    /// <summary>
    /// Tooltip helper utility.
    /// </summary>
    public static class TooltipHelper {
        private const int TooltipDuration = 2000;

        /// <summary>
        /// Show a tooltip at a given control.
        /// </summary>
        public static async Task ShowTooltipForControl(string text, Control control, bool focus = true) {
            ToolTip.SetTip(control, text);
            ToolTip.SetIsOpen(control, true);
            if (focus)
                control.Focus();
            await Task.Delay(TooltipDuration);
            ToolTip.SetIsOpen(control, false);
        }

        public enum TooltipLocation {LEFT, TOP, RIGHT, BOTTOM}

        /// <summary>
        /// Show a tooltip at a given location relative to a control.
        /// </summary>
        public static async Task ShowTooltipForControl(string text, Control control, TooltipLocation location) {
            ToolTip.SetTip(control, text);
            ToolTip.SetPlacement(control, GetPlacementMode(location));
            ToolTip.SetIsOpen(control, true);
            await Task.Delay(TooltipDuration);
            ToolTip.SetIsOpen(control, false);
        }

        /// <summary>
        /// Show a tooltip near the mouse position.
        /// </summary>
        public static async Task ShowTooltipAtMouse(string text, Control control) {
            ToolTip.SetTip(control, text);
            ToolTip.SetPlacement(control, PlacementMode.Pointer);
            ToolTip.SetIsOpen(control, true);
            await Task.Delay(TooltipDuration);
            ToolTip.SetIsOpen(control, false);
        }

        private static PlacementMode GetPlacementMode(TooltipLocation location) {
            return location switch {
                TooltipLocation.LEFT => PlacementMode.Left,
                TooltipLocation.TOP => PlacementMode.Top,
                TooltipLocation.RIGHT => PlacementMode.Right,
                TooltipLocation.BOTTOM => PlacementMode.Bottom,
                _ => PlacementMode.Bottom
            };
        }
    }
}
