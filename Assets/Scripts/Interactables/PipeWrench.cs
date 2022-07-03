using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipeWrench : DroppableInteractable{

    public override PlayerAnimation SelfInteraction(PlayerAvatar avatar) {
        interactionAudioSource.gameObject.transform.position = PlayerManager.Instance.LocalPlayer.Avatar.gameObject.transform.position;
        AudioClip sound = GetRandomInteractionClip();
        interactionAudioSource.PlayOneShot(sound);
    return PlayerAnimation.INTERACT;
    }

}
