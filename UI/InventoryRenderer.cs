// UI/InventoryRenderer.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ExileCore2.PoEMemory;
using ExileCore2.PoEMemory.Elements;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.Shared.Enums;
using ExileCore2.Shared.Helpers;
using ImGuiNET;
using Poe2ScoutPricer.Configuration;
using Poe2ScoutPricer.Models;
using Poe2ScoutPricer.Services;
using Poe2ScoutPricer.Utils;

namespace Poe2ScoutPricer.UI
{
    public class InventoryRenderer : IDisposable
    {
        private readonly Poe2ScoutSettings _settings;
        private readonly IPriceService _priceService;
        private readonly List<CustomItem> _inventoryItems = new();
        private double _totalValue = 0;
        private DateTime _lastUpdate = DateTime.MinValue;
        private bool _disposed = false;

        public InventoryRenderer(Poe2ScoutSettings settings, IPriceService priceService)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _priceService = priceService ?? throw new ArgumentNullException(nameof(priceService));
        }

        public void Render()
        {
            if (!_settings.InventorySettings.ShowInventoryValue || _disposed)
                return;

            try
            {
                UpdateInventoryItems();
                RenderInventoryValue();
                
                if (_settings.InventorySettings.ShowIndividualPrices)
                {
                    RenderIndividualPrices();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error rendering inventory: {ex.Message}");
            }
        }

        private void UpdateInventoryItems()
        {
            // Update inventory items every second
            if (DateTime.UtcNow - _lastUpdate < TimeSpan.FromSeconds(1))
                return;

            _inventoryItems.Clear();
            _totalValue = 0;

            try
            {
                var gameController = CustomItem.GameController.Instance;
                if (gameController?.Game?.IngameState?.IngameUi?.InventoryPanel is not { IsVisible: true } inventoryPanel)
                    return;

                var inventory = inventoryPanel[InventoryIndex.PlayerInventory];
                if (inventory?.VisibleInventoryItems == null)
                    return;

                var currentLeague = gameController.Game.IngameState.ServerData.League ?? "Standard";

                foreach (var inventoryItem in inventory.VisibleInventoryItems)
                {
                    if (inventoryItem?.Item?.IsValid != true)
                        continue;

                    var customItem = new CustomItem(inventoryItem);
                    if (customItem.ItemType == ItemTypes.None)
                        continue;

                    // Get price asynchronously if not already loaded
                    if (!customItem.HasValidPrice)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                customItem.PriceData = await _priceService.GetItemPriceAsync(customItem, currentLeague);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError($"Error getting price for inventory item: {ex.Message}");
                            }
                        });
                    }

                    _inventoryItems.Add(customItem);
                    
                    var itemValue = customItem.PriceData.GetBestPrice();
                    if (customItem.CurrencyInfo.StackSize > 1)
                    {
                        itemValue *= customItem.CurrencyInfo.StackSize;
                    }
                    
                    _totalValue += itemValue;
                }

                _lastUpdate = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error updating inventory items: {ex.Message}");
            }
        }

        private void RenderInventoryValue()
        {
            var position = new Vector2(_settings.InventorySettings.PositionX, _settings.InventorySettings.PositionY);
            
            ImGui.SetNextWindowPos(position);
            ImGui.SetNextWindowBgAlpha(0.8f);
            
            var windowFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                             ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize |
                             ImGuiWindowFlags.NoInputs;

            if (ImGui.Begin("##InventoryValue", windowFlags))
            {
                ImGui.Text("Inventory Value:");
                
                var divinePrice = _priceService.DivinePrice;
                var valueText = _totalValue.FormatDivinePrice(divinePrice);
                
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.8f, 0f, 1f)); // Gold color
                ImGui.Text(valueText);
                ImGui.PopStyleColor();
                
                ImGui.Text($"Items: {_inventoryItems.Count}");
            }
            ImGui.End();
        }

        private void RenderIndividualPrices()
        {
            var gameController = CustomItem.GameController.Instance;
            if (gameController?.Game?.IngameState?.IngameUi?.InventoryPanel is not { IsVisible: true } inventoryPanel)
                return;

            var inventory = inventoryPanel[InventoryIndex.PlayerInventory];
            if (inventory?.VisibleInventoryItems == null)
                return;

            foreach (var inventoryItem in inventory.VisibleInventoryItems)
            {
                if (inventoryItem?.Item?.IsValid != true)
                    continue;

                var customItem = _inventoryItems.FirstOrDefault(i => i.Entity?.Address == inventoryItem.Item.Address);
                if (customItem == null || !customItem.HasValidPrice)
                    continue;

                var itemValue = customItem.PriceData.GetBestPrice();
                if (itemValue < _settings.InventorySettings.MinValueToShow)
                    continue;

                var rect = inventoryItem.GetClientRect();
                var position = new Vector2(rect.Right + 5, rect.Top);
                
                ImGui.SetNextWindowPos(position);
                ImGui.SetNextWindowBgAlpha(0.7f);
                
                var windowFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                                 ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize |
                                 ImGuiWindowFlags.NoInputs;

                if (ImGui.Begin($"##ItemPrice_{customItem.GetHashCode()}", windowFlags))
                {
                    var priceText = itemValue.FormatPrice(_settings.PriceDisplaySettings.DecimalPlaces);
                    ImGui.TextColored(new Vector4(1f, 1f, 0f, 1f), priceText);
                }
                ImGui.End();
            }
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
                _inventoryItems.Clear();
                _disposed = true;
            }
        }
    }
}

