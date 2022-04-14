using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[System.Serializable]
public struct PlayerPos {
    public Vector3 Position;
    public Quaternion Rotation;
}

[RequireComponent(typeof(NetworkObject))]
public class PlayerControllerMP : NetworkBehaviour {

    private CharacterController controller;
    private float movementSpeed = 5f;
    private System.Guid id;
    private string playerName = "";

    private NetworkVariable<PlayerPos> playerPos
            = new NetworkVariable<PlayerPos>();

    void Start() {
        controller = GetComponent<CharacterController>();
        id = System.Guid.NewGuid();
        playerName = id.ToString();

        Debug.Log("Player " + playerName + " created!");
    }

    void Update() {
        if (IsClient) {
            if (IsOwner) {
                ProcessInput();
            } else {
                UpdatePos();
            }
        }
    }

    void ProcessInput() {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        var direction = new Vector3(horizontalInput, 0, verticalInput);

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
