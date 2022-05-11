using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllPanel : InteractableBase
{
    [SerializeField]
    private float accaleration = 0.1f;
    public bool stearing;

    protected override void Interaction(){
        if(!stearing){
            ShipManager.Instance.AccillerateSpeedServerRpc(accaleration);
        }
        else{
            ShipManager.Instance.AccillerateAngleServerRpc(accaleration);
        }
    }
}
