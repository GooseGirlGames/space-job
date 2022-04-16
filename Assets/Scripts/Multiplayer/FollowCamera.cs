using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour {
    PlayerAvatar player;

    // Update is called once per frame
    void Update() {
        var player = PlayerManager.Instance.LocalPlayer.Avatar;

        if (player) {
            transform.LookAt(player.transform.position);
        }
    }
}
