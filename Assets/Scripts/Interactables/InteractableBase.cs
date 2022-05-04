using Unity.Netcode;

public abstract class InteractableBase : NetworkBehaviour {

    // Called when item is held in hand and right mouse button pressed
    // Returns animation to play upon interaction
    public virtual int SelfInteraction(PlayerAvatar avatar) {
        return -1;
    }
}
