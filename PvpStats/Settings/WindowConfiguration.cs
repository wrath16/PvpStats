﻿using System.Collections.Generic;
using System.Numerics;

namespace PvpStats.Settings;
public class WindowConfiguration {
    public Dictionary<string, Vector2> TabWindowSizes = new();
    public bool FiltersCollapsed { get; set; } = false;
    public float FilterRatio { get; set; } = 3.2f;
    public uint FilterHeight { get; set; } = 250;
    public FilterConfiguration MatchFilters { get; set; } = new();
    public FilterConfiguration JobStatFilters { get; set; } = new();
    public FilterConfiguration PlayerStatFilters { get; set; } = new();
}
