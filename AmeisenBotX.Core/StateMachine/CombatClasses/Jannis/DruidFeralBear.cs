﻿using AmeisenBotX.Core.Character.Comparators;
using AmeisenBotX.Core.Character.Inventory.Enums;
using AmeisenBotX.Core.Character.Talents.Objects;
using AmeisenBotX.Core.Data.Enums;
using AmeisenBotX.Core.Data.Objects.WowObjects;
using AmeisenBotX.Core.Statemachine.Enums;
using System.Collections.Generic;
using System.Linq;
using static AmeisenBotX.Core.Statemachine.Utils.AuraManager;
using static AmeisenBotX.Core.Statemachine.Utils.InterruptManager;

namespace AmeisenBotX.Core.Statemachine.CombatClasses.Jannis
{
    public class DruidFeralBear : BasicCombatClass
    {
        public DruidFeralBear(WowInterface wowInterface, AmeisenBotStateMachine stateMachine) : base(wowInterface, stateMachine)
        {
            MyAuraManager.BuffsToKeepActive = new Dictionary<string, CastFunction>()
            {
                { markOfTheWildSpell, () => CastSpellIfPossible(markOfTheWildSpell, WowInterface.ObjectManager.PlayerGuid, true, 0, true) },
                { direBearFormSpell, () => CastSpellIfPossible(direBearFormSpell, 0, true) }
            };

            TargetAuraManager.DebuffsToKeepActive = new Dictionary<string, CastFunction>()
            {
                // { faerieFireSpell, () => CastSpellIfPossible(faerieFireSpell, WowInterface.ObjectManager.TargetGuid, true) },
                { mangleBearSpell, () => CastSpellIfPossible(mangleBearSpell, WowInterface.ObjectManager.TargetGuid, true) }
            };

            TargetInterruptManager.InterruptSpells = new SortedList<int, CastInterruptFunction>()
            {
                { 0, (x) => CastSpellIfPossible(bashSpell, x.Guid, true) },
            };

            GroupAuraManager.SpellsToKeepActiveOnParty.Add((markOfTheWildSpell, (spellName, guid) => CastSpellIfPossible(spellName, guid, true)));
        }

        public override string Author => "Jannis";

        public override WowClass WowClass => WowClass.Druid;

        public override Dictionary<string, dynamic> Configureables { get; set; } = new Dictionary<string, dynamic>();

        public override string Description => "FCFS based CombatClass for the Feral (Bear) Druid spec.";

        public override string Displayname => "Druid Feral Bear";

        public override bool HandlesMovement => false;

        public override bool IsMelee => true;

        public override IWowItemComparator ItemComparator { get; set; } = new BasicArmorComparator(new List<ArmorType>() { ArmorType.SHIELDS }, new List<WeaponType>() { WeaponType.ONEHANDED_SWORDS, WeaponType.ONEHANDED_MACES, WeaponType.ONEHANDED_AXES });

        public override CombatClassRole Role => CombatClassRole.Tank;

        public override TalentTree Talents { get; } = new TalentTree()
        {
            Tree1 = new Dictionary<int, Talent>(),
            Tree2 = new Dictionary<int, Talent>()
            {
                { 1, new Talent(2, 1, 5) },
                { 3, new Talent(2, 3, 1) },
                { 4, new Talent(2, 4, 2) },
                { 5, new Talent(2, 5, 3) },
                { 6, new Talent(2, 6, 2) },
                { 7, new Talent(2, 7, 1) },
                { 8, new Talent(2, 8, 3) },
                { 10, new Talent(2, 10, 3) },
                { 11, new Talent(2, 11, 2) },
                { 12, new Talent(2, 12, 2) },
                { 13, new Talent(2, 13, 1) },
                { 14, new Talent(2, 14, 1) },
                { 16, new Talent(2, 16, 3) },
                { 17, new Talent(2, 17, 5) },
                { 18, new Talent(2, 18, 3) },
                { 19, new Talent(2, 19, 1) },
                { 20, new Talent(2, 20, 2) },
                { 22, new Talent(2, 22, 3) },
                { 24, new Talent(2, 24, 3) },
                { 25, new Talent(2, 25, 3) },
                { 26, new Talent(2, 26, 1) },
                { 27, new Talent(2, 27, 3) },
                { 28, new Talent(2, 28, 5) },
                { 29, new Talent(2, 29, 1) },
                { 30, new Talent(2, 30, 1) },
            },
            Tree3 = new Dictionary<int, Talent>()
            {
                { 1, new Talent(3, 1, 2) },
                { 3, new Talent(3, 3, 3) },
                { 4, new Talent(3, 4, 5) },
                { 8, new Talent(3, 8, 1) },
            },
        };

        public override bool UseAutoAttacks => true;

        public override string Version => "1.0";

        public override bool WalkBehindEnemy => false;

        public override void ExecuteCC()
        {
            if (SelectTarget(DpsTargetManager))
            {
                double distanceToTarget = WowInterface.ObjectManager.Target.Position.GetDistance(WowInterface.ObjectManager.Player.Position);

                if (distanceToTarget > 9.0
                    && CastSpellIfPossible(feralChargeBearSpell, WowInterface.ObjectManager.Target.Guid, true))
                {
                    return;
                }

                if (WowInterface.ObjectManager.Player.HealthPercentage < 40
                    && CastSpellIfPossible(survivalInstinctsSpell, 0, true))
                {
                    return;
                }

                if (WowInterface.ObjectManager.Target.TargetGuid != WowInterface.ObjectManager.PlayerGuid
                    && CastSpellIfPossible(growlSpell, 0, true))
                {
                    return;
                }

                if (CastSpellIfPossible(berserkSpell, 0))
                {
                    return;
                }

                if (NeedToHealMySelf())
                {
                    return;
                }

                int nearEnemies = WowInterface.ObjectManager.GetNearEnemies<WowUnit>(WowInterface.ObjectManager.Player.Position, 10).Count();

                if ((WowInterface.ObjectManager.Player.HealthPercentage > 80
                        && CastSpellIfPossible(enrageSpell, 0, true))
                    || (WowInterface.ObjectManager.Player.HealthPercentage < 70
                        && CastSpellIfPossible(barkskinSpell, 0, true))
                    || (WowInterface.ObjectManager.Player.HealthPercentage < 75
                        && CastSpellIfPossible(frenziedRegenerationSpell, 0, true))
                    || (nearEnemies > 2 && CastSpellIfPossible(challengingRoarSpell, 0, true))
                    || CastSpellIfPossible(lacerateSpell, WowInterface.ObjectManager.TargetGuid, true)
                    || (nearEnemies > 2 && CastSpellIfPossible(swipeSpell, 0, true))
                    || CastSpellIfPossible(mangleBearSpell, WowInterface.ObjectManager.TargetGuid, true))
                {
                    return;
                }
            }
        }

        public override void OutOfCombatExecute()
        {
            if (GroupAuraManager.Tick()
                || MyAuraManager.Tick()
                || NeedToHealMySelf())
            {
                return;
            }
        }

        private bool NeedToHealMySelf()
        {
            if (WowInterface.ObjectManager.Player.HealthPercentage < 60
                && !WowInterface.ObjectManager.Player.HasBuffByName(rejuvenationSpell)
                && CastSpellIfPossible(rejuvenationSpell, 0, true))
            {
                return true;
            }

            if (WowInterface.ObjectManager.Player.HealthPercentage < 40
                && CastSpellIfPossible(healingTouchSpell, 0, true))
            {
                return true;
            }

            return false;
        }
    }
}