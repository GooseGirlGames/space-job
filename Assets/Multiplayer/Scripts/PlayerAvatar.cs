using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using TMPro;

[System.Serializable]
public struct PlayerPos {
    public Vector3 Position;
    public Quaternion Rotation;
}

[RequireComponent(typeof(NetworkObject))]
public class PlayerAvatar : NetworkBehaviour {

    private CharacterController controller;
    private float movementSpeed = 5f;
    public string playerName;

    //private NetworkVariable<FixedString32Bytes> playerName
    //        = new NetworkVariable<FixedString32Bytes>();

    private NetworkVariable<PlayerPos> playerPos
            = new NetworkVariable<PlayerPos>();

    public TMP_Text nameText;

    void Start() {
        controller = GetComponent<CharacterController>();

        /*
         * if (IsOwner) {
            var name = SystemInfo.deviceName + Random.RandomRange(1000, 9999);
            HelloServerRpc(name);
        }
        */

        //Debug.Log("Player avatar " + playerName.Value + " created!");
    }

    void Update() {
        if (IsClient) {
            UpdateNameTag();
            if (IsOwner) {
                ProcessInput();
            } else {
                UpdatePos();
            }
        }
    }

    void UpdateNameTag() {
        nameText.text = playerName;  // TODO only update when needed?

        nameText.gameObject.transform.LookAt(Camera.main.transform.position);
        nameText.gameObject.transform.Rotate(Vector3.up, 180f);  // mirror
    }

    void ProcessInput() {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        var direction = new Vector3(horizontalInput, 0, verticalInput);

        var cameraDirection = Camera.main.transform.rotation;
        direction = cameraDirection * direction;
        direction.y = 0;  // no flying allowed!

        controller.Move(direction * Time.deltaTime * movementSpeed);

        var p = new PlayerPos();
        p.Position = controller.transform.position;
        p.Rotation = controller.transform.rotation;

        //playerPos.Value = p;
        UpdatePosServerRpc(p);
    }

    void UpdatePos() {
        transform.position = playerPos.Value.Position;
        transform.rotation = playerPos.Value.Rotation;
    }

    [ServerRpc]
    public void UpdatePosServerRpc(PlayerPos p) {
        playerPos.Value = p;
    }

}
