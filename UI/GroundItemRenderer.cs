// UI/GroundItemRenderer.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ExileCore2.PoEMemory;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.Elements;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Cache;
using ExileCore2.Shared.Enums;
using ExileCore2.Shared.Helpers;
using ImGuiNET;
using Poe2ScoutPricer.Configuration;
using Poe2ScoutPricer.Models;
using Poe2ScoutPricer.Services;
using Poe2ScoutPricer.Utils;

namespace Poe2ScoutPricer.UI
{
    public class GroundItemRenderer : IDisposable
    {
        private readonly Poe2ScoutSettings _settings;
        private readonly IPriceService _priceService;
        private readonly Dictionary<Entity, CustomItem> _groundItems = new();
        private readonly CachedValue<List<LabelOnGround>> _groundLabels;
        private bool _disposed = false;

        public GroundItemRenderer(Poe2ScoutSettings settings, IPriceService priceService)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _priceService = priceService ?? throw new ArgumentNullException(nameof(priceService));
            _groundLabels = new FrameCache<List<LabelOnGround>>(GetGroundLabels);
        }

        public void Render()
        {
            if (!_settings.GroundItemSettings.ShowGroundItemPrices || _disposed)
                return;

            try
            {
                UpdateGroundItems();
                RenderGroundItemPrices();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error rendering ground items: {ex.Message}");
            }
        }

        private List<LabelOnGround> GetGroundLabels()
        {
            try
            {
                var gameController = CustomItem.GameController.Instance;
                if (gameController?.IngameState?.IngameUi?.ItemsOnGroundLabelElement?.VisibleGroundItemLabels == null)
                    return new List<LabelOnGround>();

                return gameController.IngameState.IngameUi.ItemsOnGroundLabelElement.VisibleGroundItemLabels.ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error getting ground labels: {ex.Message}");
                return new List<LabelOnGround>();
            }
        }

        private void UpdateGroundItems()
        {
            var currentLabels = _groundLabels.Value;
            var currentLeague = CustomItem.GameController.Instance?.Game?.IngameState?.ServerData?.League ?? "Standard";
            
            // Remove items that are no longer on ground
            var currentEntities = new HashSet<Entity>(currentLabels
                .Where(l => l.ItemOnGround?.TryGetComponent<WorldItem>(out var worldItem) == true && 
                           worldItem.ItemEntity != null && worldItem.ItemEntity.IsValid)
                .Select(l => l.ItemOnGround.GetComponent<WorldItem>().ItemEntity));
            
            var toRemove = _groundItems.Keys.Where(entity => !currentEntities.Contains(entity)).ToList();
            foreach (var entity in toRemove)
            {
                _groundItems.Remove(entity);
            }

            // Add new items
            foreach (var label in currentLabels)
            {
                if (!label.ItemOnGround.TryGetComponent<WorldItem>(out var worldItem) || 
                    worldItem.ItemEntity == null || 
                    !worldItem.ItemEntity.IsValid)
                    continue;

                var entity = worldItem.ItemEntity;
                if (_groundItems.ContainsKey(entity))
                    continue;

                var customItem = new CustomItem(entity, label.Label);
                
                // Filter based on settings
                if (_settings.GroundItemSettings.OnlyPriceUniques && 
                    customItem.Rarity != ItemRarity.Unique)
                    continue;

                if (customItem.ItemType == ItemTypes.None)
                    continue;

                _groundItems[entity] = customItem;

                // Get price asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        customItem.PriceData = await _priceService.GetItemPriceAsync(customItem, currentLeague);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error getting price for ground item: {ex.Message}");
                    }
                });
            }
        }

        private void RenderGroundItemPrices()
        {
            var camera = CustomItem.GameController.Instance?.Game?.IngameState?.Camera;
            if (camera == null)
                return;

            foreach (var kvp in _groundItems.ToList())
            {
                var entity = kvp.Key;
                var customItem = kvp.Value;

                if (!entity.IsValid)
                {
                    _groundItems.Remove(entity);
                    continue;
                }

                if (!customItem.HasValidPrice)
                    continue;

                var itemValue = customItem.PriceData.GetBestPrice();
                
                // Apply value filtering
                if (_settings.GroundItemSettings.OnlyShowValuableItems && 
                    itemValue < _settings.GroundItemSettings.MinimumValue)
                    continue;

                if (!entity.TryGetComponent<Render>(out var render))
                    continue;

                var worldPosition = render.Position;
                var screenPosition = camera.WorldToScreen(worldPosition);
                
                if (screenPosition == Vector2.Zero)
                    continue;

                // Render price text
                RenderPriceText(screenPosition, customItem, itemValue);
                
                // Render unique name if applicable
                if (_settings.GroundItemSettings.ShowRealUniqueNames && 
                    customItem.Rarity == ItemRarity.Unique && 
                    !string.IsNullOrEmpty(customItem.UniqueName))
                {
                    RenderUniqueName(screenPosition, customItem);
                }
            }
        }

        private void RenderPriceText(Vector2 position, CustomItem item, double value)
        {
            var priceText = value.FormatPrice(_settings.PriceDisplaySettings.DecimalPlaces);
            var textSize = ImGui.CalcTextSize(priceText) * _settings.GroundItemSettings.TextScale;
            
            var drawPosition = new Vector2(
                position.X - textSize.X / 2,
                position.Y - textSize.Y - 20
            );

            var backgroundColor = ColorToVector4(_settings.GroundItemSettings.BackgroundColor);
            var textColor = ColorToVector4(_settings.GroundItemSettings.TextColor);
            
            // Draw background
            var drawList = ImGui.GetBackgroundDrawList();
            drawList.AddRectFilled(
                drawPosition - new Vector2(2, 2),
                drawPosition + textSize + new Vector2(2, 2),
                ImGui.ColorConvertFloat4ToU32(backgroundColor)
            );
            
            // Draw text
            ImGui.PushFont(ImGui.GetFont());
            drawList.AddText(
                ImGui.GetFont(),
                ImGui.GetFontSize() * _settings.GroundItemSettings.TextScale,
                drawPosition,
                ImGui.ColorConvertFloat4ToU32(textColor),
                priceText
            );
            ImGui.PopFont();
        }

        private void RenderUniqueName(Vector2 position, CustomItem item)
        {
            var nameText = item.UniqueName;
            var textSize = ImGui.CalcTextSize(nameText) * _settings.GroundItemSettings.TextScale;
            
            var drawPosition = new Vector2(
                position.X - textSize.X / 2,
                position.Y + 5
            );

            var backgroundColor = ColorToVector4(_settings.GroundItemSettings.BackgroundColor);
            var textColor = ColorToVector4(_settings.GroundItemSettings.UniqueNameColor);
            
            // Draw background
            var drawList = ImGui.GetBackgroundDrawList();
            drawList.AddRectFilled(
                drawPosition - new Vector2(2, 2),
                drawPosition + textSize + new Vector2(2, 2),
                ImGui.ColorConvertFloat4ToU32(backgroundColor)
            );
            
            // Draw text
            ImGui.PushFont(ImGui.GetFont());
            drawList.AddText(
                ImGui.GetFont(),
                ImGui.GetFontSize() * _settings.GroundItemSettings.TextScale,
                drawPosition,
                ImGui.ColorConvertFloat4ToU32(textColor),
                nameText
            );
            ImGui.PopFont();
        }

        private static Vector4 ColorToVector4(Color color)
        {
            return new Vector4(
                color.R / 255f,
                color.G / 255f,
                color.B / 255f,
                color.A / 255f
            );
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
                _groundItems.Clear();
                _disposed = true;
            }
        }
    }
}