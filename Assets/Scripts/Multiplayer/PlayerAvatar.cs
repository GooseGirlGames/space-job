using UnityEngine;
using Unity.Netcode;
using TMPro;

[System.Serializable]
public struct PlayerPos {
    public Vector3 Position;
    public Quaternion Rotation;
}

[RequireComponent(typeof(NetworkObject))]
public class PlayerAvatar : NetworkBehaviour {

    private NetworkVariable<PlayerPos> m_playerPos
            = new NetworkVariable<PlayerPos>();
    private NetworkVariable<NetworkObjectReference> m_primaryItem
            = new NetworkVariable<NetworkObjectReference>();
    private NetworkVariable<NetworkObjectReference> m_secondaryItem
            = new NetworkVariable<NetworkObjectReference>();

    public enum Slot {
        PRIMARY, SECONDARY
    };

    [ServerRpc]
    public void UpdatePosServerRpc(PlayerPos p) {
        m_playerPos.Value = p;
    }


    private CharacterController m_controller;
    private float m_movementSpeed = 5f;
    private bool m_isGrounded = false;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public const float GRAVITY = -10f;  //in case of zero gravity this need to change
    private PersistentPlayer m_localPlayer;
    private Vector3 m_Velocity;

    public TMP_Text nameText;

    public void Start() {
        m_controller = GetComponent<CharacterController>();

        foreach (var player in FindObjectsOfType<PersistentPlayer>()) {
            if (player.OwnerClientId == OwnerClientId) {
                m_localPlayer = player;
                break;
            }
        }
        name = m_localPlayer.PlayerName;
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
    public void PerformGroundCheck() {
        m_isGrounded = Physics.CheckSphere(groundCheck.position,
                GroundCheck.GROUND_CHECK_RADIUS,
                groundLayer
        );
    }
    void UpdateNameTag() {
        nameText.text = m_localPlayer.PlayerName;  // TODO only update when needed?
        nameText.gameObject.transform.LookAt(Camera.main.transform.position);
        nameText.gameObject.transform.Rotate(Vector3.up, 180f);  // mirror
    }

    void ProcessInput() {
        if (Input.GetKeyDown(KeyCode.Q)) {
            DropItem(Slot.PRIMARY);
        }

        PerformGroundCheck();
        if (m_isGrounded && m_Velocity.y < 0) {
            m_Velocity.y = -2f;
        }

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        var direction = new Vector3(horizontalInput, 0, verticalInput);

        var cameraDirection = Camera.main.transform.rotation;
        direction = Vector3.Normalize(cameraDirection * direction);
        direction.y = 0;  // no flying allowed!

        m_controller.Move(direction * Time.deltaTime * m_movementSpeed);
        m_controller.transform.LookAt(m_controller.transform.position + direction);

        m_Velocity.y += GRAVITY * Time.deltaTime;
        m_controller.Move(m_Velocity * Time.deltaTime);

        var p = new PlayerPos();
        p.Position = m_controller.transform.position;
        p.Rotation = m_controller.transform.rotation;

        UpdatePosServerRpc(p);
    }

    void UpdatePos() {
        transform.position = m_playerPos.Value.Position;
        transform.rotation = m_playerPos.Value.Rotation;
    }

    private NetworkObject GetInventoryItem(Slot slot) {
        NetworkObjectReference reference;
        if (slot == Slot.PRIMARY) {
            reference = m_primaryItem.Value;
        } else {
            reference = m_secondaryItem.Value;
        }

        NetworkObject o;
        if (reference.TryGet(out o)) {
            return o;
        }
        return null;
    }

    public bool HasInventorySpace(Slot slot) {
        return GetInventoryItem(slot) == null;
    }

    public bool HasInventorySpace() {
        return HasInventorySpace(Slot.PRIMARY) || HasInventorySpace(Slot.SECONDARY);
    }

    public void AddToInventory(NetworkObject item) {
        if (HasInventorySpace(Slot.PRIMARY)) {
            AddToInventory(Slot.PRIMARY, item);
        } else if (HasInventorySpace(Slot.SECONDARY)) {
            AddToInventory(Slot.SECONDARY, item);
        }
    }

    public void AddToInventory(Slot slot, NetworkObject item) {
        if (slot == Slot.PRIMARY) {
            m_primaryItem.Value = item;
        } else {
            m_secondaryItem.Value = item;
        }
    }

    public void DropItem(Slot slot) {
        NetworkObjectReference item;
        if (slot == Slot.PRIMARY) {
            item = m_primaryItem.Value;
        } else {
            item = m_secondaryItem.Value;
        }

        NetworkObject o;
        if (!item.TryGet(out o)) {
            Debug.Log("No item here.");
        }

        Cup cup = o.GetComponentInChildren<Cup>();
        cup.DropServerRpc(transform.position);

        if (slot == Slot.PRIMARY) {
            m_primaryItem = new NetworkVariable<NetworkObjectReference>();
        } else {
            m_secondaryItem = new NetworkVariable<NetworkObjectReference>();
        }
    }
}
