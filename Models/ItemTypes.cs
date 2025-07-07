// Models/ItemTypes.cs
namespace Poe2ScoutPricer.Models
{
    public enum ItemTypes
    {
        None,
        Currency,
        UniqueWeapon,
        UniqueArmour,
        UniqueAccessory,
        UniqueJewel,
        UniqueFlask,
        UniqueMap,
        Map,
        Fragment,
        DivinationCard,
        Essence,
        Fossil,
        Resonator,
        Scarab,
        Oil,
        Incubator,
        DeliriumOrb,
        Catalyst,
        SkillGem,
        BaseItem
    }

    public static class ItemTypeExtensions
    {
        private static readonly Dictionary<string, ItemTypes> CategoryMapping = new(StringComparer.OrdinalIgnoreCase)
        {
            ["currency"] = ItemTypes.Currency,
            ["weapon"] = ItemTypes.UniqueWeapon,
            ["armour"] = ItemTypes.UniqueArmour,
            ["armor"] = ItemTypes.UniqueArmour,
            ["accessory"] = ItemTypes.UniqueAccessory,
            ["jewel"] = ItemTypes.UniqueJewel,
            ["flask"] = ItemTypes.UniqueFlask,
            ["map"] = ItemTypes.UniqueMap,
            ["fragment"] = ItemTypes.Fragment,
            ["divinationcard"] = ItemTypes.DivinationCard,
            ["essence"] = ItemTypes.Essence,
            ["fossil"] = ItemTypes.Fossil,
            ["resonator"] = ItemTypes.Resonator,
            ["scarab"] = ItemTypes.Scarab,
            ["oil"] = ItemTypes.Oil,
            ["incubator"] = ItemTypes.Incubator,
            ["deliriumorb"] = ItemTypes.DeliriumOrb,
            ["catalyst"] = ItemTypes.Catalyst,
            ["skillgem"] = ItemTypes.SkillGem
        };

        public static ItemTypes FromCategoryApiId(string categoryApiId)
        {
            if (string.IsNullOrEmpty(categoryApiId))
                return ItemTypes.None;

            return CategoryMapping.GetValueOrDefault(categoryApiId, ItemTypes.None);
        }

        public static bool IsUnique(this ItemTypes itemType)
        {
            return itemType switch
            {
                ItemTypes.UniqueWeapon or
                ItemTypes.UniqueArmour or
                ItemTypes.UniqueAccessory or
                ItemTypes.UniqueJewel or
                ItemTypes.UniqueFlask or
                ItemTypes.UniqueMap => true,
                _ => false
            };
        }

        public static bool IsCurrency(this ItemTypes itemType)
        {
            return itemType switch
            {
                ItemTypes.Currency or
                ItemTypes.Fragment or
                ItemTypes.Essence or
                ItemTypes.Fossil or
                ItemTypes.Resonator or
                ItemTypes.Scarab or
                ItemTypes.Oil or
                ItemTypes.Incubator or
                ItemTypes.DeliriumOrb or
                ItemTypes.Catalyst => true,
                _ => false
            };
        }
    }
}