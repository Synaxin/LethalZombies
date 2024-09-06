using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GameNetcodeStuff;

namespace Zombies.Scripts
{
    internal class BodySyncInfo
    {
        public readonly ulong bodyID;
        public readonly Vector3 position;

        public BodySyncInfo(ulong id, Vector3 position)
        {
            this.bodyID = id;
            this.position = position;
        }

        public bool GetDifference(Vector3 newPos, float posDif)
        {
            bool outOfSync = false;
            if (Vector3.Distance(position, newPos) >= posDif)
            {
                outOfSync = true;
            }
            return outOfSync;
        }
    }

    internal class ZombieSpawnInfo
    {
        public readonly ulong playerID;
        public readonly ulong targetID;

        public ZombieSpawnInfo(ulong playerID, ulong targetID)
        {
            this.playerID = playerID;
            this.targetID = targetID;
        }
    }
}
