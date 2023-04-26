using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
    public class AIPlayer : Player
    {
        public override void FixedUpdateNetwork()
        {
            if (Object.IsProxy == true)
                return;

            if (IsInitialized == false && Object.HasInputAuthority == true && Runner.Stage == 
                SimulationStages.Forward)
            {
                var unityID = Context.PlayerData.UnityID != null ? Context.PlayerData.UnityID : string.Empty;

                // RPC_Initialize(Context.PeerUserID, "Enemy ABC", Context.PlayerData.AgentPrefabID, unityID);
                IsInitialized = true;
            }
        }

    }
}
