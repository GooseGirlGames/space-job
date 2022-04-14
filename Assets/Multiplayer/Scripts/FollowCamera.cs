using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour {
    PlayerControllerMP player;

    // Update is called once per frame
    void Update() {
        var player = PlayerManager.Instance.LocalPlayer;

        if (player) {
            transform.LookAt(player.transform.position);
        }
    }
}
