using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class PlayerControllerMP : NetworkBehaviour {

    private CharacterController controller;
    private float movementSpeed = 5f;
    private System.Guid id;
    private string playerName = "";

    void Start() {
        controller = GetComponent<CharacterController>();
        id = System.Guid.NewGuid();
        playerName = id.ToString();

        Debug.Log("Player " + playerName + " created!");
    }

    void Update() {
        if (IsClient) {
            ClientUpdate();
        }
    }

    void ClientUpdate() {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        var direction = new Vector3(horizontalInput, 0, verticalInput);

        controller.Move(direction * Time.deltaTime * movementSpeed);
    }
}
