using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;


namespace SextantHelper
{
    public class MainSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(false);
        [Menu("Sextant Cost")] public RangeNode<double> Sextant_Cost { get; set; } = new RangeNode<double>(5.0, 1.0, 20.0);
        [Menu("Minimum Chaos Value")] public RangeNode<int> Min_Chaos_Value { get; set; } = new RangeNode<int>(6, 1, 50);
        [Menu("Reduce Low Confidence Value")] public RangeNode<int> Reduce_Low_Confidence_Value { get; set; } = new RangeNode<int>(1, 0, 20);
        [Menu("Base Mouse Delay")] public RangeNode<int> Base_Mouse_Delay { get; set; } = new RangeNode<int>(250, 50, 500);
        [Menu("Roll Sextant")] public HotkeyNode Roll_Sextant_Hotkey { get; set; } = Keys.ControlKey;

    }
}
