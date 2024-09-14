using System;
using System.Collections.Generic;
using System.Text;
using GameNetcodeStuff;

namespace Zombies.Scripts
{
    public class BodySpawnHandler
    {
        public List<PlayerControllerB> convertedList = new List<PlayerControllerB>();

        public void AddBody(PlayerControllerB player)
        {
            Zombies.Logger.LogMessage("Addbody fired!");
            if (!convertedList.Contains(player))
            {
                Zombies.Logger.LogMessage("Body added to list!");
                convertedList.Add(player);
            }
        }

        public bool ContainsPlayer(PlayerControllerB player)
        {
            return convertedList.Contains(player);
        }

        public void RemovePlayer(PlayerControllerB player)
        {
            if (convertedList.Contains(player))
            {
                convertedList.Remove(player);
            }
        }

        public void ResetList()
        {
            convertedList.Clear();
        }
    }
}
