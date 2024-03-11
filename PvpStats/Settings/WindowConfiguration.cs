using System.Collections.Generic;
using System.Numerics;

namespace PvpStats.Settings;
public class WindowConfiguration {
    public Dictionary<string, Vector2> TabWindowSizes = new();
    public bool FiltersCollapsed { get; set; } = false;
}
