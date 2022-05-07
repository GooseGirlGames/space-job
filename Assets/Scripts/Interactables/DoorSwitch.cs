using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorSwitch : InteractableBase
{
    public Door ConnectedDoor;
    protected override void Interaction(){
        ConnectedDoor.SetRoomStatusServerRpc();
    }
}
