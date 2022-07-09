using System.Collections.Generic;
using System.Collections;
using UnityEngine;
public class MetalPlate : DroppableInteractable {
    public override PlayerAnimation SelfInteraction(PlayerAvatar avatar) {
        interactionAudioSource.gameObject.transform.position = PlayerManager.Instance.LocalPlayer.Avatar.gameObject.transform.position;
        AudioClip sound = GetRandomInteractionClip();
        interactionAudioSource.PlayOneShot(sound);
        return PlayerAnimation.DRINK;
    }
    public override string FriendlyName() {
        return "Metal Plate";
    }
}
