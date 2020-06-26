﻿using AmeisenBotX.Core.Common;
using AmeisenBotX.Core.Data.Objects.WowObject;
using System.Collections.Generic;
using System.Linq;

namespace AmeisenBotX.Core.Statemachine.Utils.TargetSelectionLogic
{
    public class DpsTargetSelectionLogic : ITargetSelectionLogic
    {
        public DpsTargetSelectionLogic(WowInterface wowInterface)
        {
            WowInterface = wowInterface;
        }

        private WowInterface WowInterface { get; }

        public void Reset()
        {
        }

        public bool SelectTarget(out List<WowUnit> targetToSelect)
        {
            if (WowInterface.ObjectManager.Target != null
                && (WowInterface.ObjectManager.Target.IsDead
                    || WowInterface.ObjectManager.Target.IsNotAttackable
                    || !BotUtils.IsValidUnit(WowInterface.ObjectManager.Target)
                    || WowInterface.HookManager.GetUnitReaction(WowInterface.ObjectManager.Player, WowInterface.ObjectManager.Target) == WowUnitReaction.Friendly))
            {
                WowInterface.HookManager.ClearTarget();
            }

            List<WowUnit> Enemies = WowInterface.ObjectManager.ExecuteWithQueryLock(() => WowInterface.ObjectManager.GetNearEnemies<WowUnit>(WowInterface.ObjectManager.Player.Position, 100)
                .Where(e => BotUtils.IsValidUnit(e) && e.TargetGuid != 0 && WowInterface.ObjectManager.PartymemberGuids.Contains(e.TargetGuid)).ToList());

            // TODO: need to handle duels, our target will
            // be friendly there but is attackable

            if (Enemies.Count > 0)
            {
                targetToSelect = new List<WowUnit>() { Enemies.FirstOrDefault(e => BotUtils.IsValidUnit(e) && !e.IsDead) };

                if (targetToSelect != null)
                {
                    return true;
                }
            }

            // remove all invalid, dead units
            List<WowUnit> nonFriendlyUnits = Enemies.Where(e => BotUtils.IsValidUnit(e) && !e.IsDead && !e.IsNotAttackable).ToList();

            // if there are no non Friendly units, we can't attack anything
            if (nonFriendlyUnits.Count > 0)
            {
                List<WowUnit> unitsInCombatTargetingUs = nonFriendlyUnits
                    .OrderBy(e => e.Position.GetDistance(WowInterface.ObjectManager.Player.Position)).ToList();

                targetToSelect = unitsInCombatTargetingUs;
                if (targetToSelect != null && targetToSelect.Count > 0)
                {
                    return true;
                }
                else
                {
                    // maybe we are able to assist our partymembers
                    if (WowInterface.ObjectManager.PartymemberGuids.Count > 0)
                    {
                        Dictionary<WowUnit, int> partymemberTargets = new Dictionary<WowUnit, int>();

                        for (int i = 0; i < WowInterface.ObjectManager.Partymembers.Count; ++i)
                        {
                            WowUnit partymember = WowInterface.ObjectManager.Partymembers[i];

                            if (partymember.TargetGuid > 0)
                            {
                                WowUnit target = WowInterface.ObjectManager.GetWowObjectByGuid<WowUnit>(partymember.TargetGuid);

                                if (target != null
                                    && BotUtils.IsValidUnit(target)
                                    && target.IsInCombat
                                    && WowInterface.HookManager.GetUnitReaction(WowInterface.ObjectManager.Player, target) != WowUnitReaction.Friendly)
                                {
                                    if (partymemberTargets.ContainsKey(target))
                                    {
                                        partymemberTargets[target]++;
                                    }
                                    else
                                    {
                                        partymemberTargets.Add(target, 1);
                                    }
                                }
                            }
                        }

                        List<KeyValuePair<WowUnit, int>> selectedTargets = partymemberTargets.OrderByDescending(e => e.Value).ToList();

                        if (selectedTargets != null && selectedTargets.Count > 0)
                        {
                            targetToSelect = selectedTargets.Select(e => e.Key).ToList();
                            return true;
                        }
                    }
                }
            }

            WowUnit lastFallbackUnit = Enemies.Where(e => e.IsTaggedByMe).FirstOrDefault();
            if (lastFallbackUnit != null)
            {
                targetToSelect = new List<WowUnit>() { lastFallbackUnit };
                return true;
            }

            targetToSelect = null;
            return false;
        }
    }
}