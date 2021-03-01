﻿using AmeisenBotX.Core.Character.Comparators.Objects;
using AmeisenBotX.Core.Character.Inventory.Enums;
using AmeisenBotX.Core.Character.Inventory.Objects;
using System;
using System.Collections.Generic;

namespace AmeisenBotX.Core.Character.Comparators
{
    public class BasicComparator : IItemComparator
    {
        public BasicComparator(List<WowArmorType> armorTypeBlacklist, List<WowWeaponType> weaponTypeBlacklist)
        {
            ArmorTypeBlacklist = armorTypeBlacklist;
            WeaponTypeBlacklist = weaponTypeBlacklist;
        }

        public BasicComparator(List<WowArmorType> armorTypeBlacklist, List<WowWeaponType> weaponTypeBlacklist, Dictionary<string, double> statPriorities)
        {
            ArmorTypeBlacklist = armorTypeBlacklist;
            WeaponTypeBlacklist = weaponTypeBlacklist;
            GearscoreFactory = new(statPriorities);
        }

        protected GearscoreFactory GearscoreFactory { get; set; }

        private List<WowArmorType> ArmorTypeBlacklist { get; }

        private List<WowWeaponType> WeaponTypeBlacklist { get; }

        public bool IsBetter(IWowItem current, IWowItem item)
        {
            if ((ArmorTypeBlacklist != null && item.GetType() == typeof(WowArmor) && ArmorTypeBlacklist.Contains(((WowArmor)item).ArmorType))
                || (WeaponTypeBlacklist != null && item.GetType() == typeof(WowWeapon) && WeaponTypeBlacklist.Contains(((WowWeapon)item).WeaponType)))
            {
                return false;
            }

            double scoreCurrent = GearscoreFactory.Calculate(current);
            double scoreNew = GearscoreFactory.Calculate(item);
            return scoreCurrent < scoreNew;
        }

        public bool IsBlacklistedItem(IWowItem item)
        {
            if (ArmorTypeBlacklist != null && string.Equals(item.Type, "Armor", StringComparison.OrdinalIgnoreCase) && ArmorTypeBlacklist.Contains(((WowArmor)item).ArmorType))
            {
                return true;
            }
            else if (WeaponTypeBlacklist != null && string.Equals(item.Type, "Weapon", StringComparison.OrdinalIgnoreCase) && WeaponTypeBlacklist.Contains(((WowWeapon)item).WeaponType))
            {
                return true;
            }

            return false;
        }
    }
}