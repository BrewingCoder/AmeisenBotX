﻿using AmeisenBotX.Core.Common;
using AmeisenBotX.Core.Movement.Pathfinding.Enums;
using AmeisenBotX.Core.Movement.Pathfinding.Objects;
using AmeisenBotX.Logging;
using AmeisenBotX.Logging.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AmeisenBotX.Core.Movement.Pathfinding
{
    public class NavmeshServerPathfindingHandler : CustomTcpClient, IPathfindingHandler
    {
        public NavmeshServerPathfindingHandler(string ip, int port) : base(ip, port)
        {
        }

        public bool CastMovementRay(int mapId, Vector3 start, Vector3 end)
        {
            return BuildAndSendPathRequest<Vector3>(1, mapId, start, end, MovementType.CastMovementRay) != default;
        }

        public List<Vector3> GetPath(int mapId, Vector3 start, Vector3 end)
        {
            return BuildAndSendPathRequest<List<Vector3>>(1, mapId, start, end, MovementType.FindPath, PathRequestFlags.CatmullRomSpline);
        }

        public Vector3 GetRandomPoint(int mapId)
        {
            return BuildAndSendRandomPointRequest(2, mapId, Vector3.Zero, 0f);
        }

        public Vector3 GetRandomPointAround(int mapId, Vector3 start, float maxRadius)
        {
            return BuildAndSendRandomPointRequest(2, mapId, start, maxRadius);
        }

        public Vector3 MoveAlongSurface(int mapId, Vector3 start, Vector3 end)
        {
            return BuildAndSendPathRequest<Vector3>(1, mapId, start, end, MovementType.MoveAlongSurface);
        }

        private T BuildAndSendPathRequest<T>(int msgType, int mapId, Vector3 start, Vector3 end, MovementType movementType, PathRequestFlags pathRequestFlags = PathRequestFlags.None)
        {
            if (IsConnected)
            {
                try
                {
                    string response = SendString(msgType, JsonConvert.SerializeObject(new PathRequest(mapId, start, end, pathRequestFlags, movementType)));

                    if (BotUtils.IsValidJson(response))
                    {
                        return JsonConvert.DeserializeObject<T>(response);
                    }
                }
                catch (Exception e)
                {
                    AmeisenLogger.I.Log("Pathfinding", $"BuildAndSendPathRequest failed:\n{e}", LogLevel.Error);
                }
            }

            return default;
        }

        private Vector3 BuildAndSendRandomPointRequest(int msgType, int mapId, Vector3 start, float maxRadius)
        {
            if (IsConnected)
            {
                try
                {
                    string response = SendString(msgType, JsonConvert.SerializeObject(new RandomPointRequest(mapId, start, maxRadius)));

                    if (string.IsNullOrWhiteSpace(response))
                    {
                        return Vector3.Zero;
                    }
                    else if (BotUtils.IsValidJson(response))
                    {
                        return JsonConvert.DeserializeObject<Vector3>(response);
                    }
                }
                catch (Exception e)
                {
                    AmeisenLogger.I.Log("Pathfinding", $"BuildAndSendRandomPointRequest failed:\n{e}", LogLevel.Error);
                }
            }

            return Vector3.Zero;
        }
    }
}