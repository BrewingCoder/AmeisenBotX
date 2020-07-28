﻿using AmeisenBotX.Core.Character.Comparators;
using AmeisenBotX.Core.Character.Inventory.Enums;
using AmeisenBotX.Core.Character.Talents.Objects;
using AmeisenBotX.Core.Data.Enums;
using AmeisenBotX.Core.Data.Objects.WowObject;
using AmeisenBotX.Core.Statemachine.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using static AmeisenBotX.Core.Statemachine.Utils.AuraManager;
using static AmeisenBotX.Core.Statemachine.Utils.InterruptManager;

namespace AmeisenBotX.Core.Statemachine.CombatClasses.Jannis
{
    public class DruidBalance : BasicCombatClass
    {
        public DruidBalance(WowInterface wowInterface, AmeisenBotStateMachine stateMachine) : base(wowInterface, stateMachine)
        {
            MyAuraManager.BuffsToKeepActive = new Dictionary<string, CastFunction>()
            {
                { moonkinFormSpell, () => CastSpellIfPossible(moonkinFormSpell, 0, true) },
                { thornsSpell, () => CastSpellIfPossible(thornsSpell, 0, true) },
                { markOfTheWildSpell, () => CastSpellIfPossible(markOfTheWildSpell, WowInterface.ObjectManager.PlayerGuid, true, 0, true) }
            };

            TargetAuraManager.DebuffsToKeepActive = new Dictionary<string, CastFunction>()
            {
                { moonfireSpell, () => LunarEclipse && CastSpellIfPossible(moonfireSpell, WowInterface.ObjectManager.TargetGuid, true) },
                { insectSwarmSpell, () => SolarEclipse && CastSpellIfPossible(insectSwarmSpell, WowInterface.ObjectManager.TargetGuid, true) }
            };

            TargetInterruptManager.InterruptSpells = new SortedList<int, CastInterruptFunction>()
            {
                { 0, (x) => CastSpellIfPossible(faerieFireSpell, x.Guid, true) },
            };

            GroupAuraManager.SpellsToKeepActiveOnParty.Add((markOfTheWildSpell, (spellName, guid) => CastSpellIfPossible(spellName, guid, true)));

            SolarEclipse = false;
            LunarEclipse = true;
        }

        public override string Author => "Jannis";

        public override WowClass Class => WowClass.Druid;

        public override Dictionary<string, dynamic> Configureables { get; set; } = new Dictionary<string, dynamic>();

        public override string Description => "FCFS based CombatClass for the Balance (Owl) Druid spec.";

        public override string Displayname => "Druid Balance";

        public override bool HandlesMovement => false;

        public override bool IsMelee => false;

        public override IWowItemComparator ItemComparator { get; set; } = new BasicIntellectComparator(new List<ArmorType>() { ArmorType.SHIELDS }, new List<WeaponType>() { WeaponType.ONEHANDED_SWORDS, WeaponType.ONEHANDED_MACES, WeaponType.ONEHANDED_AXES });

        public DateTime LastEclipseCheck { get; private set; }

        public bool LunarEclipse { get; set; }

        public override CombatClassRole Role => CombatClassRole.Dps;

        public bool SolarEclipse { get; set; }

        public override TalentTree Talents { get; } = new TalentTree()
        {
            Tree1 = new Dictionary<int, Talent>()
            {
                { 1, new Talent(1, 1, 5) },
                { 3, new Talent(1, 3, 1) },
                { 4, new Talent(1, 4, 2) },
                { 5, new Talent(1, 5, 2) },
                { 7, new Talent(1, 7, 3) },
                { 8, new Talent(1, 8, 1) },
                { 9, new Talent(1, 9, 2) },
                { 10, new Talent(1, 10, 5) },
                { 11, new Talent(1, 11, 3) },
                { 12, new Talent(1, 12, 3) },
                { 13, new Talent(1, 13, 1) },
                { 16, new Talent(1, 16, 3) },
                { 17, new Talent(1, 17, 2) },
                { 18, new Talent(1, 18, 1) },
                { 19, new Talent(1, 19, 3) },
                { 20, new Talent(1, 20, 3) },
                { 22, new Talent(1, 22, 5) },
                { 23, new Talent(1, 23, 3) },
                { 25, new Talent(1, 25, 1) },
                { 26, new Talent(1, 26, 2) },
                { 27, new Talent(1, 27, 3) },
                { 28, new Talent(1, 28, 1) },
            },
            Tree2 = new Dictionary<int, Talent>(),
            Tree3 = new Dictionary<int, Talent>()
            {
                { 1, new Talent(3, 1, 2) },
                { 3, new Talent(3, 3, 5) },
                { 6, new Talent(3, 6, 3) },
                { 7, new Talent(3, 7, 3) },
                { 8, new Talent(3, 8, 1) },
                { 9, new Talent(3, 9, 2) },
            },
        };

        public override bool UseAutoAttacks => false;

        public override string Version => "1.0";

        public override bool WalkBehindEnemy => false;

        public override void ExecuteCC()
        {
            CheckForEclipseProcs();

            if (SelectTarget(DpsTargetManager))
            {
                if (CastSpellIfPossible(naturesGraspSpell, 0))
                {
                    return;
                }

                double distance = WowInterface.ObjectManager.Target.Position.GetDistance(WowInterface.ObjectManager.Player.Position);

                if (distance < 12.0
                    && WowInterface.ObjectManager.Target.HasBuffByName(entanglingRootsSpell)
                    && CastSpellIfPossibleDk(entanglingRootsSpell, WowInterface.ObjectManager.TargetGuid, false, false, true))
                {
                    return;
                }

                if (NeedToHealMySelf())
                {
                    return;
                }

                if ((WowInterface.ObjectManager.Player.ManaPercentage < 30
                        && CastSpellIfPossible(innervateSpell, 0))
                    || (WowInterface.ObjectManager.Player.HealthPercentage < 70
                        && CastSpellIfPossible(barkskinSpell, 0, true))
                    || (LunarEclipse
                        && CastSpellIfPossible(starfireSpell, WowInterface.ObjectManager.TargetGuid, true))
                    || (SolarEclipse
                        && CastSpellIfPossible(wrathSpell, WowInterface.ObjectManager.TargetGuid, true))
                    || (WowInterface.ObjectManager.WowObjects.OfType<WowUnit>().Where(e => !e.IsInCombat && WowInterface.ObjectManager.Player.Position.GetDistance(e.Position) < 35).Count() < 4
                        && CastSpellIfPossible(starfallSpell, WowInterface.ObjectManager.TargetGuid, true)))
                {
                    return;
                }

                if (CastSpellIfPossible(forceOfNatureSpell, 0, true))
                {
                    WowInterface.HookManager.ClickOnTerrain(WowInterface.ObjectManager.Player.Position);
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

        private bool CheckForEclipseProcs()
        {
            if (WowInterface.ObjectManager.Player.HasBuffByName(eclipseLunarSpell))
            {
                SolarEclipse = false;
                LunarEclipse = true;
            }
            else if (WowInterface.ObjectManager.Player.HasBuffByName(eclipseSolarSpell))
            {
                SolarEclipse = true;
                LunarEclipse = false;
            }

            LastEclipseCheck = DateTime.Now;
            return false;
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