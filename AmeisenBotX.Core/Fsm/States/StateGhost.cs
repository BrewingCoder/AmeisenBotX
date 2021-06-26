﻿using AmeisenBotX.BehaviorTree;
using AmeisenBotX.BehaviorTree.Enums;
using AmeisenBotX.BehaviorTree.Objects;
using AmeisenBotX.Core.Common;
using AmeisenBotX.Core.Data.Enums;
using AmeisenBotX.Core.Data.Objects;
using AmeisenBotX.Core.Fsm.Enums;
using AmeisenBotX.Core.Fsm.States.StaticDeathRoutes;
using AmeisenBotX.Core.Movement.Enums;
using AmeisenBotX.Core.Movement.Pathfinding.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AmeisenBotX.Core.Fsm.States
{
    public class StateGhost : BasicState
    {
        private readonly List<IStaticDeathRoute> StaticDeathRoutes = new()
        {
            new ForgeOfSoulsDeathRoute()
        };

        private ulong playerToFollowGuid = 0;

        public StateGhost(AmeisenBotFsm stateMachine, AmeisenBotConfig config, WowInterface wowInterface) : base(stateMachine, config, wowInterface)
        {
            DungeonSelector = new
            (
                () => Config.DungeonUsePartyMode,
                new Selector
                (
                    () => WowInterface.DungeonEngine.TryGetProfileByMapId(StateMachine.LastDiedMap) != null,
                    new Leaf(RunToDungeonProfileEntry),
                    new Selector
                    (
                        () => IsUnitToFollowNear(out playerToFollowGuid),
                        new Leaf(FollowNearestUnit),
                        new Leaf(RunToCorpsePositionAndSearchForPortals)
                    )
                ),
                new Selector
                (
                    () => WowInterface.DungeonEngine.Profile.WorldEntry != default,
                    new Leaf(RunToDungeonEntry),
                    new Leaf(RunToCorpsePositionAndSearchForPortals)
                )
            );

            BehaviorTree = new
            (
                new Selector
                (
                    () => WowInterface.ObjectManager.MapId.IsBattlegroundMap(),
                    new Leaf(() =>
                    {
                        WowInterface.MovementEngine.StopMovement();
                        return BehaviorTreeStatus.Ongoing;
                    }),
                    new Selector
                    (
                        () => CanUseStaticPaths(),
                        new Leaf(FollowStaticPath),
                        new Selector
                        (
                            () => StateMachine.LastDiedMap.IsDungeonMap(),
                            DungeonSelector,
                            new Leaf(RunToCorpseAndRetrieveIt)
                        )
                    )
                )
            );
        }

        private AmeisenBotBehaviorTree BehaviorTree { get; }

        private Vector3 CorpsePosition { get; set; }

        private Selector DungeonSelector { get; }

        private bool HasSearchedStaticRoutes { get; set; }

        private IEnumerable<WowGameobject> NearPortals { get; set; }

        private WowPlayer PlayerToFollow => WowInterface.ObjectManager.GetWowObjectByGuid<WowPlayer>(playerToFollowGuid);

        private IStaticDeathRoute StaticRoute { get; set; }

        public override void Enter()
        {
        }

        public override void Execute()
        {
            if (WowInterface.Player.Health > 1)
            {
                StateMachine.SetState(BotState.Idle);
                return;
            }

            NearPortals = WowInterface.ObjectManager.WowObjects
                .OfType<WowGameobject>()
                .Where(e => e.DisplayId == (int)WowGameobjectDisplayId.UtgardeKeepDungeonPortalNormal
                         || e.DisplayId == (int)WowGameobjectDisplayId.UtgardeKeepDungeonPortalHeroic);

            if (WowInterface.XMemory.ReadStruct(WowInterface.OffsetList.CorpsePosition, out Vector3 corpsePosition))
            {
                CorpsePosition = corpsePosition;
            }

            BehaviorTree.Tick();
        }

        public override void Leave()
        {
            WowInterface.Player.IsGhost = false;
            HasSearchedStaticRoutes = false;
            StaticRoute = null;
        }

        /// <summary>
        /// This method searches for static death routes, this is needed when pathfinding
        /// cannot find a good route from the graveyard to th dungeon entry. For example
        /// the ICC dungeons are only reachable by flying, its easier to use static routes.
        /// </summary>
        /// <returns>True when a static path can be used, false if not</returns>
        private bool CanUseStaticPaths()
        {
            if (!HasSearchedStaticRoutes)
            {
                HasSearchedStaticRoutes = true;

                IStaticDeathRoute staticRoute = StaticDeathRoutes.FirstOrDefault(e => e.IsUseable(WowInterface.ObjectManager.MapId, WowInterface.Player.Position, WowInterface.DungeonEngine.Profile.WorldEntry));

                if (staticRoute != null)
                {
                    StaticRoute = staticRoute;
                }
                else
                {
                    staticRoute = StaticDeathRoutes.FirstOrDefault(e => e.IsUseable(WowInterface.ObjectManager.MapId, WowInterface.Player.Position, CorpsePosition));

                    if (staticRoute != null)
                    {
                        StaticRoute = staticRoute;
                    }
                }
            }

            return StaticRoute != null;
        }

        private BehaviorTreeStatus FollowNearestUnit()
        {
            WowPlayer player = PlayerToFollow;

            if (WowInterface.Player.Position.GetDistance(player.Position) > Config.MinFollowDistance)
            {
                WowInterface.MovementEngine.SetMovementAction(MovementAction.Move, player.Position);
            }

            return BehaviorTreeStatus.Ongoing;
        }

        private BehaviorTreeStatus FollowStaticPath()
        {
            Vector3 nextPosition = StaticRoute.GetNextPoint(WowInterface.Player.Position);

            if (nextPosition != Vector3.Zero)
            {
                WowInterface.MovementEngine.SetMovementAction(MovementAction.DirectMove, nextPosition);
                return BehaviorTreeStatus.Ongoing;
            }
            else
            {
                // we should be in the dungeon now
                return BehaviorTreeStatus.Ongoing;
            }
        }

        /// <summary>
        /// Check wether the unit is out of range based on the config entries "MinFollowDistance" and "MaxFollowDistance".
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <returns></returns>
        private bool IsUnitOutOfRange(WowPlayer player)
        {
            double distance = player.Position.GetDistance(WowInterface.Player.Position);
            return distance < Config.MinFollowDistance || distance > Config.MaxFollowDistance;
        }

        /// <summary>
        /// This method tries to find anyone that we can follow based on the config entries set.
        /// </summary>
        /// <param name="guid">Guid of the found entity</param>
        /// <returns>True when a valid unit has been found, false if not</returns>
        private bool IsUnitToFollowNear(out ulong guid)
        {
            IEnumerable<WowPlayer> wowPlayers = WowInterface.ObjectManager.WowObjects.OfType<WowPlayer>();
            guid = 0;

            if (wowPlayers.Any())
            {
                if (Config.FollowSpecificCharacter)
                {
                    WowPlayer specificPlayer = wowPlayers.FirstOrDefault(p => p.Name == Config.SpecificCharacterToFollow && !IsUnitOutOfRange(p));

                    if (specificPlayer != null)
                    {
                        guid = specificPlayer.Guid;
                    }
                }

                // check the group/raid leader
                if (guid == 0 && Config.FollowGroupLeader)
                {
                    WowPlayer groupLeader = wowPlayers.FirstOrDefault(p => p.Name == Config.SpecificCharacterToFollow && !IsUnitOutOfRange(p));

                    if (groupLeader != null)
                    {
                        guid = groupLeader.Guid;
                    }
                }

                // check the group members
                if (guid == 0 && Config.FollowGroupMembers)
                {
                    WowPlayer groupMember = wowPlayers.FirstOrDefault(p => p.Name == Config.SpecificCharacterToFollow && !IsUnitOutOfRange(p));

                    if (groupMember != null)
                    {
                        guid = groupMember.Guid;
                    }
                }
            }

            return guid != 0;
        }

        private BehaviorTreeStatus RunToAndExecute(Vector3 position, Action action, double distance = 20.0)
        {
            if (WowInterface.Player.Position.GetDistance(position) > distance)
            {
                WowInterface.MovementEngine.SetMovementAction(MovementAction.Move, position);
                return BehaviorTreeStatus.Ongoing;
            }
            else
            {
                action();
                return BehaviorTreeStatus.Success;
            }
        }

        private BehaviorTreeStatus RunToCorpseAndRetrieveIt()
        {
            if (WowInterface.Player.Position.GetDistance(CorpsePosition) > Config.GhostResurrectThreshold)
            {
                WowInterface.MovementEngine.SetMovementAction(MovementAction.Move, CorpsePosition);
                return BehaviorTreeStatus.Ongoing;
            }
            else
            {
                WowInterface.HookManager.LuaRetrieveCorpse();
                return BehaviorTreeStatus.Success;
            }
        }

        private BehaviorTreeStatus RunToCorpsePositionAndSearchForPortals()
        {
            // we need to uplift the corpse to ground level because
            // blizz decided its good to place the corpse -100000m
            // below the dungeon entry when a player dies inside
            Vector3 upliftedPosition = CorpsePosition;

            // TODO: get real ground level from maps
            upliftedPosition.Z = 0f;

            return RunToAndExecute(upliftedPosition, () => RunToNearestPortal());
        }

        private BehaviorTreeStatus RunToDungeonEntry()
        {
            return RunToAndExecute(WowInterface.DungeonEngine.Profile.WorldEntry, () => RunToNearestPortal());
        }

        private BehaviorTreeStatus RunToDungeonProfileEntry()
        {
            Vector3 position = WowInterface.DungeonEngine.TryGetProfileByMapId(StateMachine.LastDiedMap).WorldEntry;
            return RunToAndExecute(position, () => RunToNearestPortal());
        }

        private void RunToNearestPortal()
        {
            if (NearPortals.Any())
            {
                WowInterface.MovementEngine.SetMovementAction(MovementAction.Move, BotUtils.MoveAhead(WowInterface.Player.Position, NearPortals.OrderBy(e => e.Position.GetDistance(WowInterface.Player.Position)).First().Position, 4f));
            }
        }
    }
}