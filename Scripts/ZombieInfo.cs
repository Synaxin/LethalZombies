using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GameNetcodeStuff;
using Time = UnityEngine.Time;

namespace Zombies.Scripts
{
    internal class InfectionInfo
    {
        private System.Random rand = new System.Random((int)Time.time);
        private int deadTime = 0;
        private readonly int deadTicks;
        private readonly float infectionChance;
        private int infectionTime = 0;
        private readonly int infectionTicks = 10;
        private bool infected = false;
        private PlayerControllerB parentalFigure;
        private bool parentSet = false;
        private GameObject parentRagdoll;
        private PlayerControllerB targetPlayer;
        private readonly bool proximity = false;
        private bool done = false;
        private int state = 0;
        private int wakeTicks = 10;
        private int wakeTime = 0;

        public InfectionInfo(PlayerControllerB player, int deadTicks, float infectionChance, int inMinTicks, int inMaxTicks, int proxChance, int wakeTicks)
        {
            this.targetPlayer = player;
            this.deadTicks = deadTicks;
            this.infectionChance = infectionChance;
            this.infectionTicks = inMinTicks + rand.Next(inMaxTicks - inMinTicks);
            this.proximity = rand.Next(100) < proxChance;
            this.wakeTicks = wakeTicks;
        }
        public int Tick()
        {
            //Zombies.Logger.LogInfo("Tick Started");
            //0 == OK, 1 == INFECTED, -1 == TIME OUT, 2 == INFECTING, 3 == READY, 4 == PROX, 5 == WAKING, 6 == WAKE, -2 == DONE
            if (state < 0 || state == 5)
            {
                return state;
            }
            if (!infected)
            {
                bool infection = RollInfection(this.infectionChance);
                this.deadTime++;
                if (infection)
                {
                    state = 1;
                    return 1;
                }
                else if (this.deadTime >= this.deadTicks)
                {
                    state = -1;
                    return -1;
                }
                else
                {
                    state = 0;
                    return 0;
                }
            }
            else
            {
                this.infectionTime++;
                if (this.infectionTime >= this.infectionTicks && state <= 4)
                {
                    if (proximity)
                    {
                        state = 4;
                        return 4;
                    }
                    else
                    {
                        state = 3;
                        return 3;
                    }
                }
                else
                {
                    state = 2;
                    return 2;
                }
            }
        }

        public int WakeTick()
        {
            wakeTime++;
            if (wakeTime >= wakeTicks)
            {
                state = 6;
                return 6;
            }
            return 5;
        }

        public void Finish()
        {
            done = true;
            state = -2;
        }
        public bool GetProximity()
        {
            return proximity;
        }
        public int GetState()
        {
            return state;
        }

        public void SetParentalFigure(PlayerControllerB parent, GameObject ragdoll)
        {
            parentSet = true;
            parentalFigure = parent;
            parentRagdoll = ragdoll;
        }

        public void SetWakeTicks(int newWakeTicks)
        {
            wakeTicks = newWakeTicks;
        }

        public void SetTarget(PlayerControllerB target)
        {
            targetPlayer = target;
        }

        public bool GetParentSet()
        {
            return parentSet;
        }

        public PlayerControllerB GetParentalFigure()
        {
            return parentalFigure;
        }

        public GameObject GetRagdoll()
        {
            return parentRagdoll;
        }

        public PlayerControllerB GetTarget()
        {
            return targetPlayer;
        }

        public void WakeUp()
        {
            state = 5;
        }

        private bool RollInfection(float chance)
        {
            float baseNumber = rand.Next(1000) / 10;
            if (baseNumber <= chance)
            {
                infected = true;
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}