using UnityEngine;
using Unity.Netcode;

public class Door : NetworkBehaviour {
    private Room m_ConnectedRoomA = null;
    private Room m_ConnectedRoomB = null;
    private NetworkVariable<bool> m_Open = new NetworkVariable<bool>(true);
    private Collider m_PhysicalDoor;
    private MeshRenderer m_PsychologicalDoor;


    public void SetRoom(Room room) {
        if (m_ConnectedRoomA == null) {
            m_ConnectedRoomA = room;
        } else if (m_ConnectedRoomB == null) {
            m_ConnectedRoomB = room;
        } else {
            Debug.Log("Door's full of rooms :(");
        }
    }

    public override void OnNetworkSpawn(){
        m_Open.OnValueChanged += OnDoorStateChange;
    }
    public override void OnNetworkDespawn(){
        m_Open.OnValueChanged -= OnDoorStateChange;
    }

    public void OnDoorStateChange(bool previous, bool current){
        m_PsychologicalDoor.enabled = current;
        m_PhysicalDoor.enabled = current;

    }

    // Start is called before the first frame update
    void Start() {
        m_PhysicalDoor = GetComponent<Collider>();
        m_PsychologicalDoor = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update() {
        
    }
}
