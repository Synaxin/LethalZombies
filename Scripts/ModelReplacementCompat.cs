using System;
using System.Collections.Generic;
using System.Text;
using GameNetcodeStuff;
using ModelReplacement;

namespace Zombies.Scripts
{
    public class ModelReplacementCompat
    {
        public void SetBodyVisible(PlayerControllerB player)
        {
            Zombies.Logger.LogDebug("Model Replacement API Found");
            BodyReplacementBase bodyScript;
            if (player == null)
            {
                return;
            }
            if (player.gameObject.TryGetComponent<BodyReplacementBase>(out bodyScript))
            {
                Zombies.Logger.LogDebug("Found Body Replacement Base");
                if (bodyScript.replacementDeadBody != null)
                {
                    bodyScript.replacementDeadBody.gameObject.SetActive(true);
                }
            }
            else
            {
                Zombies.Logger.LogDebug("Did not find Body Replacement Base");
            }
        }
    }
}
