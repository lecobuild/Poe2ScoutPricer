// Models/CustomItem.cs
using ExileCore2.PoEMemory;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.Elements;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.PoEMemory.FilesInMemory;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.PoEMemory.Models;
using ExileCore2.Shared.Enums;
using Poe2ScoutPricer.Models;
using Poe2ScoutPricer.Utils;

namespace Poe2ScoutPricer.Models
{
    public class CustomItem
    {
        public string BaseName { get; set; } = string.Empty;
        public string UniqueName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Name => !string.IsNullOrEmpty(UniqueName) ? UniqueName : BaseName;
        public string CategoryApiId { get; set; } = string.Empty;
        
        public bool IsIdentified { get; set; }
        public bool IsCorrupted { get; set; }
        public bool IsWeapon { get; set; }
        public bool IsHovered { get; set; }
        
        public Element? Element { get; set; }
        public Entity? Entity { get; set; }
        
        public int ItemLevel { get; set; }
        public int Quality { get; set; }
        public int GemLevel { get; set; }
        public string GemName { get; set; } = string.Empty;
        public ItemRarity Rarity { get; set; }
        public int Sockets { get; set; }
        public List<string> UniqueNameCandidates { get; set; } = new();
        public ItemTypes ItemType { get; set; }
        public List<string> EnchantedStats { get; set; } = new();
        public string CapturedMonsterName { get; set; } = string.Empty;
        
        // Map data
        public MapData MapInfo { get; set; } = new();
        
        // Currency data
        public CurrencyData CurrencyInfo { get; set; } = new();
        
        // Price data
        public PriceData PriceData { get; set; } = new();
        
        public CustomItem()
        {
        }
        
        public CustomItem(NormalInventoryItem item) : this(item?.Item, item)
        {
        }
        
        public CustomItem(BaseItemType baseItemType)
        {
            if (baseItemType == null)
            {
                Logger.LogWarning("BaseItemType is null in CustomItem constructor");
                return;
            }

            Path = baseItemType.Metadata ?? string.Empty;
            ClassName = baseItemType.ClassName ?? string.Empty;
            BaseName = baseItemType.BaseName ?? string.Empty;
            DetermineItemType();
        }
        
        public CustomItem(Entity? itemEntity, Element? element = null)
        {
            if (itemEntity == null || !itemEntity.IsValid)
            {
                Logger.LogWarning("Invalid entity provided to CustomItem constructor");
                return;
            }

            try
            {
                Element = element;
                Path = itemEntity.Path ?? string.Empty;
                Entity = itemEntity;
                
                // Check GameController availability
                var gameController = GameController.Instance;
                if (gameController?.Files?.BaseItemTypes == null)
                {
                    Logger.LogWarning("GameController or BaseItemTypes not available during CustomItem creation");
                    // Still try to extract what we can from the entity
                    ExtractBasicEntityInfo(itemEntity);
                    return;
                }
                
                // Get base item type info
                var baseItemType = gameController.Files.BaseItemTypes.Translate(itemEntity.Path);
                ClassName = baseItemType?.ClassName ?? string.Empty;
                BaseName = baseItemType?.BaseName ?? string.Empty;
                
                // Extract all component data
                ExtractEntityComponents(itemEntity);
                
                DetermineItemType();
                DetermineCategoryApiId();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error creating CustomItem from entity: {ex.Message}");
            }
        }

        private void ExtractBasicEntityInfo(Entity itemEntity)
        {
            try
            {
                // Extract what we can without GameController
                if (itemEntity.TryGetComponent<Mods>(out var mods))
                {
                    Rarity = mods.ItemRarity;
                    IsIdentified = mods.Identified;
                    ItemLevel = mods.ItemLevel;
                    UniqueName = mods.UniqueName?.Replace('\x2019', '\x27') ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error extracting basic entity info: {ex.Message}");
            }
        }

        private void ExtractEntityComponents(Entity itemEntity)
        {
            try
            {
                // Determine if it's a weapon
                IsWeapon = IsWeaponClass(ClassName);
                
                // Get quality
                if (itemEntity.TryGetComponent<Quality>(out var quality))
                {
                    Quality = quality.ItemQuality;
                }
                
                // Get gem info
                if (itemEntity.TryGetComponent<SkillGem>(out var skillGem))
                {
                    GemLevel = skillGem.Level;
                    GemName = skillGem.GemEffect?.Name ?? string.Empty;
                }
                
                // Get base properties
                if (itemEntity.TryGetComponent<Base>(out var baseComponent))
                {
                    IsCorrupted = baseComponent.isCorrupted;
                    ItemLevel = Math.Max(ItemLevel, baseComponent.CurrencyItemLevel);
                }
                
                // Get mods and rarity
                if (itemEntity.TryGetComponent<Mods>(out var mods))
                {
                    Rarity = mods.ItemRarity;
                    IsIdentified = mods.Identified;
                    ItemLevel = Math.Max(ItemLevel, mods.ItemLevel);
                    EnchantedStats = mods.EnchantedStats?.ToList() ?? new List<string>();
                    UniqueName = mods.UniqueName?.Replace('\x2019', '\x27') ?? string.Empty;
                    
                    // Handle unidentified uniques
                    if (!IsIdentified && Rarity == ItemRarity.Unique)
                    {
                        var artPath = itemEntity.GetComponent<RenderItem>()?.ResourcePath;
                        if (!string.IsNullOrEmpty(artPath))
                        {
                            // This would need to be implemented with art mapping similar to original
                            // For now, we'll leave it empty
                            UniqueNameCandidates = new List<string>();
                        }
                    }
                }
                
                // Get sockets
                if (itemEntity.TryGetComponent<Sockets>(out var sockets))
                {
                    try
                    {
                        Sockets = sockets.NumberOfSockets;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug($"Error reading sockets: {ex.Message}");
                        Sockets = 0;
                    }
                }
                
                // Get map info
                if (itemEntity.TryGetComponent<Map>(out var map))
                {
                    MapInfo.MapTier = map.Tier;
                    MapInfo.IsMap = true;
                }
                
                // Get stack info
                if (itemEntity.TryGetComponent<Stack>(out var stack))
                {
                    CurrencyInfo.StackSize = Math.Max(1, stack.Size);
                    CurrencyInfo.MaxStackSize = stack.Info?.MaxStackSize ?? 0;
                }
                else
                {
                    CurrencyInfo.StackSize = 1;
                }
                
                // Determine if it's a shard
                CurrencyInfo.IsShard = BaseName.Contains("Shard", StringComparison.OrdinalIgnoreCase) || 
                                      BaseName.Contains("Fragment", StringComparison.OrdinalIgnoreCase) || 
                                      BaseName.Contains("Splinter", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error extracting entity components: {ex.Message}");
            }
        }
        
        private void DetermineItemType()
        {
            try
            {
                // Determine item type based on class name and other properties
                if (IsWeapon)
                {
                    ItemType = Rarity == ItemRarity.Unique ? ItemTypes.UniqueWeapon : ItemTypes.BaseItem;
                    return;
                }
                
                var classNameLower = ClassName.ToLowerInvariant();
                
                // Currency types
                if (classNameLower.Contains("currency") || 
                    classNameLower.Contains("stackable") ||
                    CurrencyInfo.IsShard ||
                    CurrencyInfo.StackSize > 1)
                {
                    ItemType = ItemTypes.Currency;
                    return;
                }
                
                // Map types
                if (MapInfo.IsMap || classNameLower.Contains("map"))
                {
                    ItemType = Rarity == ItemRarity.Unique ? ItemTypes.UniqueMap : ItemTypes.Map;
                    return;
                }
                
                // Armor types
                if (classNameLower.Contains("helmet") || 
                    classNameLower.Contains("boots") || 
                    classNameLower.Contains("gloves") || 
                    classNameLower.Contains("body") ||
                    classNameLower.Contains("shield"))
                {
                    ItemType = Rarity == ItemRarity.Unique ? ItemTypes.UniqueArmour : ItemTypes.BaseItem;
                    return;
                }
                
                // Accessory types
                if (classNameLower.Contains("ring") || 
                    classNameLower.Contains("amulet") || 
                    classNameLower.Contains("belt"))
                {
                    ItemType = Rarity == ItemRarity.Unique ? ItemTypes.UniqueAccessory : ItemTypes.BaseItem;
                    return;
                }
                
                // Jewel types
                if (classNameLower.Contains("jewel"))
                {
                    ItemType = Rarity == ItemRarity.Unique ? ItemTypes.UniqueJewel : ItemTypes.BaseItem;
                    return;
                }
                
                // Flask types
                if (classNameLower.Contains("flask"))
                {
                    ItemType = Rarity == ItemRarity.Unique ? ItemTypes.UniqueFlask : ItemTypes.BaseItem;
                    return;
                }
                
                // Gem types
                if (!string.IsNullOrEmpty(GemName) || classNameLower.Contains("gem"))
                {
                    ItemType = ItemTypes.SkillGem;
                    return;
                }
                
                // Fragment types
                if (classNameLower.Contains("fragment") || 
                    classNameLower.Contains("splinter") ||
                    classNameLower.Contains("breach"))
                {
                    ItemType = ItemTypes.Fragment;
                    return;
                }
                
                // Divination card
                if (classNameLower.Contains("divination"))
                {
                    ItemType = ItemTypes.DivinationCard;
                    return;
                }
                
                // Essence
                if (classNameLower.Contains("essence"))
                {
                    ItemType = ItemTypes.Essence;
                    return;
                }
                
                ItemType = ItemTypes.None;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error determining item type: {ex.Message}");
                ItemType = ItemTypes.None;
            }
        }
        
        private void DetermineCategoryApiId()
        {
            try
            {
                CategoryApiId = ItemType switch
                {
                    ItemTypes.Currency => "currency",
                    ItemTypes.UniqueWeapon => "weapon",
                    ItemTypes.UniqueArmour => "armour",
                    ItemTypes.UniqueAccessory => "accessory",
                    ItemTypes.UniqueJewel => "jewel",
                    ItemTypes.UniqueFlask => "flask",
                    ItemTypes.UniqueMap => "map",
                    ItemTypes.Map => "map",
                    ItemTypes.Fragment => "fragment",
                    ItemTypes.DivinationCard => "divinationcard",
                    ItemTypes.Essence => "essence",
                    ItemTypes.Fossil => "fossil",
                    ItemTypes.Resonator => "resonator",
                    ItemTypes.Scarab => "scarab",
                    ItemTypes.Oil => "oil",
                    ItemTypes.Incubator => "incubator",
                    ItemTypes.DeliriumOrb => "deliriumorb",
                    ItemTypes.Catalyst => "catalyst",
                    ItemTypes.SkillGem => "skillgem",
                    _ => string.Empty
                };
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error determining category API ID: {ex.Message}");
                CategoryApiId = string.Empty;
            }
        }
        
        private static bool IsWeaponClass(string className)
        {
            if (string.IsNullOrEmpty(className))
                return false;

            var weaponClasses = new[]
            {
                "One Hand Mace", "Two Hand Mace", "One Hand Axe", "Two Hand Axe",
                "One Hand Sword", "Two Hand Sword", "Thrusting One Hand Sword",
                "Bow", "Claw", "Dagger", "Sceptre", "Staff", "Wand", "Crossbow",
                "Quarterstaff"
            };
            
            return weaponClasses.Contains(className, StringComparer.OrdinalIgnoreCase);
        }
        
        public bool HasValidPrice => PriceData?.HasValidPrice == true;
        
        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(UniqueName))
                return UniqueName;
            
            if (!string.IsNullOrEmpty(BaseName))
                return BaseName;
            
            return "Unknown Item";
        }
        
        public string GetPriceString()
        {
            if (!HasValidPrice)
                return "No price";
            
            return PriceData.GetPriceRange();
        }
        
        public override string ToString()
        {
            return $"{GetDisplayName()} ({ItemType}) - {GetPriceString()}";
        }

        public override int GetHashCode()
        {
            // Create a hash based on entity address if available, otherwise use path and name
            if (Entity?.Address != null)
                return Entity.Address.GetHashCode();
            
            return HashCode.Combine(Path, BaseName, UniqueName);
        }
    }
    
    public class MapData
    {
        public bool IsMap { get; set; }
        public int MapTier { get; set; }
    }
    
    public class CurrencyData
    {
        public bool IsShard { get; set; }
        public int StackSize { get; set; } = 1;
        public int MaxStackSize { get; set; } = 0;
    }
    
    // Static class to hold reference to GameController
    public static class GameController
    {
        public static ExileCore2.GameController? Instance { get; set; }
    }
}