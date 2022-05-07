using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class Room : NetworkBehaviour {
    public string Name;
    public List<Door> Doors = new List<Door>();

    private NetworkVariable<float> m_Oxygen = new NetworkVariable<float>(1.0f);
    public float Oxygen {
        get => m_Oxygen.Value;
        set => m_Oxygen.Value = value;
    }

    void Awake() {
        foreach (Door door in Doors) {
            door.SetRoom(this);
        }
    }

    private void OnTriggerEnter(Collider other) {
        var player = other.GetComponent<PlayerAvatar>();
        if (player != null) {
            player.CurrentRoom = this;
        }
    }

}
