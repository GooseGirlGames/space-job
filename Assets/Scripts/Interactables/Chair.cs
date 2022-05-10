using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chair : Interactable<bool>{
    public Transform TeleportPoint;
    public override void OnStateChange(bool previous, bool current){}
    protected override void Interaction(){
        PlayerManager.Instance.LocalPlayer.Avatar.Teleport(TeleportPoint);
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer){
            m_State.Value = true;
        }
    }
}
