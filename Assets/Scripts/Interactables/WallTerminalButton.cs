using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallTerminalButton : InteractableBase
{
    public Door ConnectedDoor;
    public bool ShouldClose;
    //public WallTerminal DoorTerminal;
    protected override void Interaction(){
        ConnectedDoor.SetDoorStatusServerRpc(!ShouldClose);
        //DoorTerminal.DisplayDoorState(ShouldClose);
    }
    public override string FriendlyName() {
        if (ShouldClose) {
            return "Close Door";
        }
        return "Open Door";
    }
}
