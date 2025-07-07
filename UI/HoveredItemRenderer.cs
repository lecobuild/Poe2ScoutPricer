// UI/HoveredItemRenderer.cs
using System;
using System.Drawing;
using System.Numerics;
using ExileCore2.PoEMemory;
using ExileCore2.PoEMemory.Elements;
using ExileCore2.Shared.Enums;
using ExileCore2.Shared.Helpers;
using ImGuiNET;
using Poe2ScoutPricer.Configuration;
using Poe2ScoutPricer.Models;
using Poe2ScoutPricer.Services;
using Poe2ScoutPricer.Utils;

namespace Poe2ScoutPricer.UI
{
    public class HoveredItemRenderer : IDisposable
    {
        private readonly Poe2ScoutSettings _settings;
        private readonly IPriceService _priceService;
        private bool _disposed = false;

        public HoveredItemRenderer(Poe2ScoutSettings settings, IPriceService priceService)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _priceService = priceService ?? throw new ArgumentNullException(nameof(priceService));
        }

        public void Render(CustomItem item)
        {
            if (!_settings.HoveredItemSettings.ShowHoveredItemPrices || item == null || _disposed)
                return;

            try
            {
                var mousePos = Input.MousePosition;
                var drawPos = new Vector2(
                    mousePos.X + _settings.HoveredItemSettings.PositionOffsetX,
                    mousePos.Y + _settings.HoveredItemSettings.PositionOffsetY
                );

                var priceText = BuildPriceText(item);
                if (string.IsNullOrEmpty(priceText))
                    return;

                // Create tooltip window
                ImGui.SetNextWindowPos(drawPos);
                ImGui.SetNextWindowBgAlpha(0.9f);
                
                var windowFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | 
                                 ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize |
                                 ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoFocusOnAppearing;

                if (ImGui.Begin($"##HoveredPrice_{item.GetHashCode()}", windowFlags))
                {
                    // Item name
                    ImGui.PushStyleColor(ImGuiCol.Text, GetItemNameColor(item));
                    ImGui.Text(item.GetDisplayName());
                    ImGui.PopStyleColor();

                    // Price information
                    ImGui.Separator();
                    ImGui.Text(priceText);

                    // Additional info
                    if (_settings.HoveredItemSettings.ShowPriceChange && item.PriceData.ChangeInLast7Days != 0)
                    {
                        var changeColor = item.PriceData.ChangeInLast7Days > 0 ? 
                            new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1);
                        ImGui.PushStyleColor(ImGuiCol.Text, changeColor);
                        ImGui.Text($"Change: {item.PriceData.ChangeInLast7Days:+0.0;-0.0;0}%");
                        ImGui.PopStyleColor();
                    }

                    // Debug info
                    if (_settings.DebugSettings.EnableDebugLogging)
                    {
                        ImGui.Separator();
                        ImGui.TextDisabled($"Type: {item.ItemType}");
                        ImGui.TextDisabled($"Category: {item.CategoryApiId}");
                        if (item.Rarity != ItemRarity.Normal)
                        {
                            ImGui.TextDisabled($"Rarity: {item.Rarity}");
                        }
                    }
                }
                ImGui.End();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error rendering hovered item: {ex.Message}");
            }
        }

        private string BuildPriceText(CustomItem item)
        {
            if (!item.HasValidPrice)
                return "No price data";

            var divinePrice = _priceService.DivinePrice;
            var bestPrice = item.PriceData.GetBestPrice();

            if (_settings.HoveredItemSettings.ShowDivinePrice && 
                divinePrice.HasValue && 
                bestPrice >= _settings.PriceDisplaySettings.DivineThreshold)
            {
                return bestPrice.FormatDivinePrice(divinePrice);
            }

            return bestPrice.FormatPrice(_settings.PriceDisplaySettings.DecimalPlaces);
        }

        private Vector4 GetItemNameColor(CustomItem item)
        {
            return item.Rarity switch
            {
                ItemRarity.Unique => new Vector4(0.686f, 0.376f, 0.145f, 1f), // Unique brown
                ItemRarity.Rare => new Vector4(1f, 1f, 0f, 1f), // Yellow
                ItemRarity.Magic => new Vector4(0.3f, 0.3f, 1f, 1f), // Blue
                ItemRarity.Currency => new Vector4(0.686f, 0.376f, 0.145f, 1f), // Currency brown
                _ => new Vector4(1f, 1f, 1f, 1f) // White for normal
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Cleanup if needed
                _disposed = true;
            }
        }
    }
}