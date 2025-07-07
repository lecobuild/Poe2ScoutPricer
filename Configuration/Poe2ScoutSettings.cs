// Configuration/Poe2ScoutSettings.cs
using System.Drawing;
using System.Windows.Forms;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using Newtonsoft.Json;

namespace Poe2ScoutPricer.Configuration
{
    public class Poe2ScoutSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new(true);
        
        public ApiSettings ApiSettings { get; set; } = new();
        public DebugSettings DebugSettings { get; set; } = new();
        public HoveredItemSettings HoveredItemSettings { get; set; } = new();
        public InventorySettings InventorySettings { get; set; } = new();
        public GroundItemSettings GroundItemSettings { get; set; } = new();
        public PriceDisplaySettings PriceDisplaySettings { get; set; } = new();
        public UpdateSettings UpdateSettings { get; set; } = new();
    }

    [Submenu(CollapsedByDefault = false)]
    public class ApiSettings
    {
        [Menu("League", "Current league to get prices for")]
        public ListNode League { get; set; } = new();

        [Menu("Sync Current League", "Automatically sync with current character league")]
        public ToggleNode SyncCurrentLeague { get; set; } = new(true);

        [Menu("API Timeout", "Timeout for API requests in seconds")]
        public RangeNode<int> ApiTimeoutSeconds { get; set; } = new(30, 5, 120);

        [Menu("Max Items Per Page", "Maximum items to request per API call")]
        public RangeNode<int> MaxItemsPerPage { get; set; } = new(1000, 100, 10000);

        [JsonIgnore]
        [Menu("Reload Prices", "Force reload all price data")]
        public ButtonNode ReloadPrices { get; set; } = new();

        [JsonIgnore]
        [Menu("Clear Cache", "Clear all cached data")]
        public ButtonNode ClearCache { get; set; } = new();
    }

    [Submenu(CollapsedByDefault = true)]
    public class DebugSettings
    {
        [Menu("Enable Debug Logging", "Enable detailed logging for debugging")]
        public ToggleNode EnableDebugLogging { get; set; } = new(false);

        [Menu("Inspect Hover Hotkey", "Hotkey to inspect hovered items")]
        public HotkeyNode InspectHoverHotkey { get; set; } = new(Keys.None);

        [JsonIgnore]
        [Menu("Reset Inspected Item", "Reset currently inspected item")]
        public ButtonNode ResetInspectedItem { get; set; } = new();

        [Menu("Show API Requests", "Show API request details in logs")]
        public ToggleNode ShowApiRequests { get; set; } = new(false);

        [Menu("Show Item Matching", "Show item matching details in logs")]
        public ToggleNode ShowItemMatching { get; set; } = new(false);
    }

    [Submenu(CollapsedByDefault = false)]
    public class HoveredItemSettings
    {
        [Menu("Show Hovered Item Prices", "Display prices for items under cursor")]
        public ToggleNode ShowHoveredItemPrices { get; set; } = new(true);

        [Menu("Show Price Change", "Show price change percentage")]
        public ToggleNode ShowPriceChange { get; set; } = new(true);

        [Menu("Show Divine Price", "Show prices in divine orbs when applicable")]
        public ToggleNode ShowDivinePrice { get; set; } = new(true);

        [Menu("Position Offset X", "Horizontal offset from cursor")]
        public RangeNode<int> PositionOffsetX { get; set; } = new(50, -500, 500);

        [Menu("Position Offset Y", "Vertical offset from cursor")]
        public RangeNode<int> PositionOffsetY { get; set; } = new(50, -500, 500);
    }

    [Submenu(CollapsedByDefault = false)]
    public class InventorySettings
    {
        [Menu("Show Inventory Value", "Display total value of inventory")]
        public ToggleNode ShowInventoryValue { get; set; } = new(true);

        [Menu("Show Individual Prices", "Show price for each item in inventory")]
        public ToggleNode ShowIndividualPrices { get; set; } = new(false);

        [Menu("Position X", "Horizontal position of inventory value display")]
        public RangeNode<int> PositionX { get; set; } = new(100, 0, 5000);

        [Menu("Position Y", "Vertical position of inventory value display")]
        public RangeNode<int> PositionY { get; set; } = new(800, 0, 5000);

        [Menu("Minimum Value to Show", "Minimum chaos value to display price")]
        public RangeNode<double> MinValueToShow { get; set; } = new(1.0, 0.1, 100.0);
    }

    [Submenu(CollapsedByDefault = false)]
    public class GroundItemSettings
    {
        [Menu("Show Ground Item Prices", "Display prices for items on ground")]
        public ToggleNode ShowGroundItemPrices { get; set; } = new(true);

        [Menu("Only Show Valuable Items", "Only show prices for items above minimum value")]
        public ToggleNode OnlyShowValuableItems { get; set; } = new(true);

        [Menu("Minimum Value", "Minimum chaos value to display")]
        public RangeNode<double> MinimumValue { get; set; } = new(5.0, 0.1, 1000.0);

        [Menu("Only Price Uniques", "Only show prices for unique items")]
        public ToggleNode OnlyPriceUniques { get; set; } = new(false);

        [Menu("Text Scale", "Scale of the price text")]
        public RangeNode<float> TextScale { get; set; } = new(1.0f, 0.5f, 3.0f);

        [Menu("Background Color", "Background color for price text")]
        public ColorNode BackgroundColor { get; set; } = new(Color.FromArgb(200, 0, 0, 0));

        [Menu("Text Color", "Color of the price text")]
        public ColorNode TextColor { get; set; } = new(Color.White);

        [Menu("Show Real Unique Names", "Show actual unique names for unidentified items")]
        public ToggleNode ShowRealUniqueNames { get; set; } = new(true);

        [Menu("Unique Name Color", "Color for unique item names")]
        public ColorNode UniqueNameColor { get; set; } = new(Color.FromArgb(175, 96, 37));
    }

    [Submenu(CollapsedByDefault = true)]
    public class PriceDisplaySettings
    {
        [Menu("Price Format", "How to format price values")]
        public ListNode PriceFormat { get; set; } = new() { Values = new List<string> { "Chaos Only", "Divine + Chaos", "Auto" }, Value = "Auto" };

        [Menu("Show Price Confidence", "Show confidence level of price data")]
        public ToggleNode ShowPriceConfidence { get; set; } = new(false);

        [Menu("Decimal Places", "Number of decimal places for prices")]
        public RangeNode<int> DecimalPlaces { get; set; } = new(2, 0, 4);

        [Menu("Use Short Numbers", "Use abbreviated numbers (1.5k instead of 1500)")]
        public ToggleNode UseShortNumbers { get; set; } = new(false);

        [Menu("Show Shard Calculations", "Show full item value for shards")]
        public ToggleNode ShowShardCalculations { get; set; } = new(true);

        [Menu("Divine Threshold", "Minimum chaos value to show as divine")]
        public RangeNode<double> DivineThreshold { get; set; } = new(50.0, 10.0, 500.0);
    }

    [Submenu(CollapsedByDefault = true)]
    public class UpdateSettings
    {
        [Menu("Auto Reload", "Automatically reload price data periodically")]
        public ToggleNode AutoReload { get; set; } = new(true);

        [Menu("Reload Interval", "Minutes between automatic data reloads")]
        public RangeNode<int> ReloadIntervalMinutes { get; set; } = new(30, 5, 180);

        [Menu("Update Check Interval", "Milliseconds between item update checks")]
        public RangeNode<int> UpdateCheckIntervalMs { get; set; } = new(250, 50, 1000);

        [Menu("Cache Duration", "Hours to keep cached data")]
        public RangeNode<int> CacheDurationHours { get; set; } = new(2, 1, 24);

        [Menu("Retry Failed Requests", "Retry failed API requests")]
        public ToggleNode RetryFailedRequests { get; set; } = new(true);

        [Menu("Max Retry Attempts", "Maximum number of retry attempts")]
        public RangeNode<int> MaxRetryAttempts { get; set; } = new(3, 1, 10);
    }
}