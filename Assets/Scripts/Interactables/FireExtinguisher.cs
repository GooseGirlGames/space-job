using System.Collections.Generic;
using System.Collections;
using UnityEngine;
public class FireExtinguisher : DroppableInteractable {
    public override PlayerAnimation SelfInteraction(PlayerAvatar avatar) {
        interactionAudioSource.gameObject.transform.position = PlayerManager.Instance.LocalPlayer.Avatar.gameObject.transform.position;
        AudioClip sound = GetRandomInteractionClip();
        interactionAudioSource.PlayOneShot(sound);
        return PlayerAnimation.INTERACT;
    }

    public override string FriendlyName() {
        return "Fire Extinguisher";
    }
}
