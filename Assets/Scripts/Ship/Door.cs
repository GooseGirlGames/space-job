using UnityEngine;
using Unity.Netcode;

public class Door : NetworkBehaviour {
    private Room m_ConnectedRoomA = null;
    private Room m_ConnectedRoomB = null;
    private NetworkVariable<bool> m_Open = new NetworkVariable<bool>(true);


    public void SetRoom(Room room) {
        if (m_ConnectedRoomA == null) {
            m_ConnectedRoomA = room;
        } else if (m_ConnectedRoomB == null) {
            m_ConnectedRoomB = room;
        } else {
            Debug.Log("Door's full of rooms :(");
        }
    }
    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
        
    }
}
