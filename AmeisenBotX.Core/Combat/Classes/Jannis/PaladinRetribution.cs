﻿using AmeisenBotX.Core.Character.Comparators;
using AmeisenBotX.Core.Character.Inventory.Enums;
using AmeisenBotX.Core.Character.Talents.Objects;
using AmeisenBotX.Core.Data.Enums;
using AmeisenBotX.Core.Fsm;
using AmeisenBotX.Core.Fsm.Utils.Auras.Objects;

namespace AmeisenBotX.Core.Combat.Classes.Jannis
{
    public class PaladinRetribution : BasicCombatClass
    {
        public PaladinRetribution(WowInterface wowInterface, AmeisenBotFsm stateMachine) : base(wowInterface, stateMachine)
        {
            MyAuraManager.Jobs.Add(new KeepActiveAuraJob(blessingOfMightSpell, () => TryCastSpell(blessingOfMightSpell, WowInterface.ObjectManager.PlayerGuid, true)));
            MyAuraManager.Jobs.Add(new KeepActiveAuraJob(retributionAuraSpell, () => TryCastSpell(retributionAuraSpell, 0, true)));
            MyAuraManager.Jobs.Add(new KeepActiveAuraJob(sealOfVengeanceSpell, () => TryCastSpell(sealOfVengeanceSpell, 0, true)));

            InterruptManager.InterruptSpells = new()
            {
                { 0, (x) => TryCastSpell(hammerOfJusticeSpell, x.Guid, true) }
            };

            GroupAuraManager.SpellsToKeepActiveOnParty.Add((blessingOfMightSpell, (spellName, guid) => TryCastSpell(spellName, guid, true)));
        }

        public override string Description => "FCFS based CombatClass for the Retribution Paladin spec.";

        public override string Displayname => "Paladin Retribution";

        public override bool HandlesMovement => false;

        public override bool IsMelee => true;

        public override IItemComparator ItemComparator { get; set; } = new BasicStrengthComparator(new() { WowArmorType.SHIELDS }, new() { WowWeaponType.ONEHANDED_AXES, WowWeaponType.ONEHANDED_MACES, WowWeaponType.ONEHANDED_SWORDS });

        public override WowRole Role => WowRole.Dps;

        public override TalentTree Talents { get; } = new()
        {
            Tree1 = new()
            {
                { 2, new(1, 2, 5) },
                { 4, new(1, 4, 5) },
                { 6, new(1, 6, 1) },
            },
            Tree2 = new()
            {
                { 2, new(2, 2, 5) },
            },
            Tree3 = new()
            {
                { 2, new(3, 2, 5) },
                { 3, new(3, 3, 2) },
                { 4, new(3, 4, 3) },
                { 5, new(3, 5, 2) },
                { 7, new(3, 7, 5) },
                { 8, new(3, 8, 1) },
                { 9, new(3, 9, 2) },
                { 11, new(3, 11, 3) },
                { 12, new(3, 12, 3) },
                { 13, new(3, 13, 3) },
                { 14, new(3, 14, 1) },
                { 15, new(3, 15, 3) },
                { 17, new(3, 17, 2) },
                { 18, new(3, 18, 1) },
                { 19, new(3, 19, 3) },
                { 20, new(3, 20, 3) },
                { 21, new(3, 21, 2) },
                { 22, new(3, 22, 3) },
                { 23, new(3, 23, 1) },
                { 24, new(3, 24, 3) },
                { 25, new(3, 25, 3) },
                { 26, new(3, 26, 1) },
            },
        };

        public override bool UseAutoAttacks => true;

        public override string Version => "1.0";

        public override bool WalkBehindEnemy => false;

        public override WowClass WowClass => WowClass.Paladin;

        public override void Execute()
        {
            base.Execute();

            if (SelectTarget(TargetManagerDps))
            {
                if ((WowInterface.ObjectManager.Player.HealthPercentage < 20.0
                        && TryCastSpell(layOnHandsSpell, WowInterface.ObjectManager.PlayerGuid))
                    || (WowInterface.ObjectManager.Player.HealthPercentage < 60.0
                        && TryCastSpell(holyLightSpell, WowInterface.ObjectManager.PlayerGuid, true)))
                {
                    return;
                }

                if (((WowInterface.ObjectManager.Player.HasBuffByName(sealOfVengeanceSpell) || WowInterface.ObjectManager.Player.HasBuffByName(sealOfWisdomSpell))
                        && TryCastSpell(judgementOfLightSpell, WowInterface.ObjectManager.TargetGuid, true))
                    || TryCastSpell(avengingWrathSpell, 0, true)
                    || (WowInterface.ObjectManager.Player.ManaPercentage < 80.0
                        && TryCastSpell(divinePleaSpell, 0, true)))
                {
                    return;
                }

                if (WowInterface.ObjectManager.Target != null)
                {
                    if ((WowInterface.ObjectManager.Player.HealthPercentage < 20.0
                            && TryCastSpell(hammerOfWrathSpell, WowInterface.ObjectManager.TargetGuid, true))
                        || TryCastSpell(crusaderStrikeSpell, WowInterface.ObjectManager.TargetGuid, true)
                        || TryCastSpell(divineStormSpell, WowInterface.ObjectManager.TargetGuid, true)
                        || TryCastSpell(consecrationSpell, WowInterface.ObjectManager.TargetGuid, true)
                        || TryCastSpell(exorcismSpell, WowInterface.ObjectManager.TargetGuid, true)
                        || TryCastSpell(holyWrathSpell, WowInterface.ObjectManager.TargetGuid, true))
                    {
                        return;
                    }
                }
            }
        }

        public override void OutOfCombatExecute()
        {
            base.OutOfCombatExecute();
        }
    }
}