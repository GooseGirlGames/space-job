public class DoorSwitch : RangedInteractableBase {
    public Door ConnectedDoor;
    protected override void Interaction(){
        ConnectedDoor.ToogleDoorStatusServerRpc();
    }

    public override string FriendlyName() {
        return "Door Switch";
    }
}
