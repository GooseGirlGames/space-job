using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Kasette : DroppableInteractable{
    public override PlayerAnimation SelfInteraction(PlayerAvatar avatar) {
        interactionAudioSource.gameObject.transform.position = PlayerManager.Instance.LocalPlayer.Avatar.gameObject.transform.position;
        AudioClip sound = GetRandomInteractionClip();
        interactionAudioSource.PlayOneShot(sound);
        return PlayerAnimation.INTERACT;
    }

    public override string FriendlyName() {
        return "Cassette";
    }
}
