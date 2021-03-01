﻿using AmeisenBotX.Core.Character.Inventory.Enums;
using System.Collections.Generic;

namespace AmeisenBotX.Core.Character.Comparators
{
    public class BasicArmorComparator : BasicComparator
    {
        public BasicArmorComparator(List<WowArmorType> armorTypeBlacklist = null, List<WowWeaponType> weaponTypeBlacklist = null) : base(armorTypeBlacklist, weaponTypeBlacklist)
        {
            GearscoreFactory = new(new()
            {
                { WowStatType.STAMINA, 2.0 },
                { WowStatType.ARMOR, 2.5 },
            });
        }
    }
}