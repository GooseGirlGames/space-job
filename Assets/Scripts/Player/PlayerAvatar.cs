using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct PlayerPos : INetworkSerializeByMemcpy {
    public PlayerPos(Vector3 _Position, Quaternion _Rotation){
        Position = _Position;
        Rotation = _Rotation;
    }
    public Vector3 Position;
    public Quaternion Rotation;
}

[RequireComponent(typeof(NetworkObject))]
public class PlayerAvatar : NetworkBehaviour {

    /* === GENERAL STUFF === */
    private PersistentPlayer m_LocalPlayer;
    private Renderer m_PlayerMesh;
    private PlayerRagdoll m_Ragdoll;
    private Coroutine m_DeathReviveCoroutine = null;
    public NetworkVariable<bool> Spawned =
            new NetworkVariable<bool>(false, default, NetworkVariableWritePermission.Owner);

    /* === MOVEMENT & ROOMS === */
    public const float GRAVITY = -10f;  //in case of zero gravity this need to change
    public Transform groundCheck;
    public LayerMask groundLayer;
    private NetworkVariable<PlayerPos> m_PlayerPos
            = new NetworkVariable<PlayerPos>();
    private List<int> m_MovementLocks = new List<int>();
    private Coroutine m_SpeedBoostCoroutine = null;
    private CharacterController m_Controller;
    private float m_MovementSpeed = 3f;
    private bool m_IsGrounded = false;
    private float m_FallVelocity;

    private Room m_CurrentRoom = null;
    public Room CurrentRoom {
        get => m_CurrentRoom;
        set {
            m_CurrentRoom = value;
        }
    }

    /* === INVENTORY === */
    public enum Slot { PRIMARY, SECONDARY };

    [Tooltip("Point in space from which items are dropped.")]
    public Transform dropPoint;
    [Tooltip("Location where the item is displayed in the primaty slot.  Should be the player's left hand.")]
    public Transform PrimaryItemDisplay;
    [Tooltip("Location where the item is displayed in the secondary slot.  Should be the player's back.")]
    public Transform SecondaryItemDisplay;

    private NetworkVariable<NetworkObjectReference> m_PrimaryItem
            = new NetworkVariable<NetworkObjectReference>(default, default, NetworkVariableWritePermission.Owner);
    private NetworkVariable<NetworkObjectReference> m_SecondaryItem
            = new NetworkVariable<NetworkObjectReference>(default, default, NetworkVariableWritePermission.Owner);


    /* === LOBBY & CHARACTER SELECT === */
    [SerializeField]
    private GameObject m_CharacterList;
    public GameObject isready;
    public GameObject notready;
    public CharacterSelect chara_select;
    private NetworkVariable<int> m_ActiveCharacter
            = new NetworkVariable<int>(3, default, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> ready
            = new NetworkVariable<bool>(default, default, NetworkVariableWritePermission.Owner);


    /* === HEALTH, OXYGEN & RELATED UI === */
    public NetworkVariable<float> m_Health
            = new NetworkVariable<float>(1f, default, NetworkVariableWritePermission.Owner);
    private bool m_HasDied = false;  // used to avoid triggering death sequence multiple times
    [SerializeField]
    private HealthBar m_HealthBar;
    [SerializeField]
    private HealthBar m_OxygenBar;  // TODO remove, this is just temporary
    private float m_LungOxygen = 1f;

    /* === ANIMATION === */
    private PlayerAnimationController m_AnimationController;
    public PlayerAnimationController AnimationController { get => m_AnimationController; }
    public NetworkVariable<float> HorizontalSpeed
            = new NetworkVariable<float>(0, default, NetworkVariableWritePermission.Owner);

    /* === OTHER VISUALS (NAMETAG, MATERIALS, CAMERA, ...) === */
    public TMP_Text nameText;
    public Material normalMaterial;
    public Material transparentMaterial;
    public Transform CameraLookAt;

    /* === AUDIO === */
    [SerializeField]
    private AudioClip[] dropCupSounds;
    [SerializeField]
    private AudioClip[] dropMetalObjectSounds;
    [SerializeField]
    private AudioClip[] m_DeathSounds;
    [SerializeField]
    private AudioClip[] m_FallSounds;
    [SerializeField]
    private AudioClip[] m_DamageSounds;
    private float m_LastDamageSound = 0f;
    private readonly float DAMAGE_SOUND_COOLDOWN = 0.7f;  // seconds
    private AudioSource audioSource;


    /* ================== LIFECYCLE METHODS ================== */

    private void Awake() {
        audioSource = GetComponent<AudioSource>();
        m_Ragdoll = GetComponent<PlayerRagdoll>();
    }

    public void Start() {
        if (CameraLookAt == null) {
            CameraLookAt = transform;
        }
        m_Controller = GetComponent<CharacterController>();
        var renderers = GetComponentsInChildren<Renderer>(includeInactive: false);
        foreach (var renderer in renderers) {
            if (renderer.gameObject.CompareTag("PlayerMeshRenderer")) {
                m_PlayerMesh = renderer;
            }
        }

        if (m_PlayerMesh == null) {
            Debug.LogError("No Player renderer found for " + name + ". "
                    + "Did you forget to tag the renderer as 'PlayerMeshRenderer'?");
        }

        if( SceneManager.GetActiveScene().name != "Lobby"){
            isready.SetActive(false);
            notready.SetActive(false);
        }

        m_HealthBar = GetComponentInChildren<HealthBar>();
        m_AnimationController = GetComponent<PlayerAnimationController>();
        if(m_AnimationController = null){
            Debug.Log("animation contraller created to early");
        }
        SetupRagdoll();
    }

    public void Setup() {
        // Setup for all players
        foreach (var player in FindObjectsOfType<PersistentPlayer>()) {
            if (player.OwnerClientId == OwnerClientId) {
                m_LocalPlayer = player;
                break;
            }
        }
        if (m_LocalPlayer == null) {
            Debug.LogWarning("Player object not found for '" + name + "'");
            return;
        }

        name = m_LocalPlayer.PlayerName;
        nameText.text = m_LocalPlayer.PlayerName;

        Debug.Log("Found persistentplayer object " + m_LocalPlayer.name + " for avatar " + name + " Owner? " + IsOwner);

        // Setup for local player
        if (IsOwner) {
            CameraSwap.UpdateLookAt(this);
        }

        // more setup
        OnPrimaryItemChanged(m_PrimaryItem.Value, m_PrimaryItem.Value);
        OnSecondaryItemChanged(m_SecondaryItem.Value, m_SecondaryItem.Value);
    }

    void Update() {
        if (IsClient) {
            if (IsOwner) {
                ProcessInput();
                OxygenRegulation(Time.deltaTime);
            } else {
                UpdatePos();
            }
            UpdateNameTag();
            UpdateHealthBar();
        }
        if(SceneManager.GetActiveScene().name == "Lobby") {
            if(ready.Value) {
                isready.SetActive(true);
                notready.SetActive(false);
            }
            if(!ready.Value) {
                isready.SetActive(false);
                notready.SetActive(true);
            }
        }
        else {
            isready.SetActive(false);
            notready.SetActive(false);
        }
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        m_PrimaryItem.OnValueChanged += OnPrimaryItemChanged;
        m_SecondaryItem.OnValueChanged += OnSecondaryItemChanged;
        m_ActiveCharacter.OnValueChanged += OnCharacterChanged;
        HorizontalSpeed.OnValueChanged += (float _, float curr) => {
            if (!m_AnimationController) {
                m_AnimationController = GetComponent<PlayerAnimationController>();
                if (!m_AnimationController) {
                    Debug.LogError("Speed changed for " + name + " but no Animator attached.");
                    return;
                }
            }
            m_AnimationController.OnSpeedChange(curr);
        };
        m_Health.OnValueChanged += OnHealthChanged;

        Debug.Log("[PlayerAvatar/OnNetworkSpawn] PlayerAvatar spanwed " + name + " owned by " + OwnerClientId);
        Setup();
    }

    public override void OnNetworkDespawn(){
        base.OnNetworkDespawn();
        m_PrimaryItem.OnValueChanged -= OnPrimaryItemChanged;
        m_SecondaryItem.OnValueChanged -= OnSecondaryItemChanged;
        m_ActiveCharacter.OnValueChanged -= OnCharacterChanged;
        m_Health.OnValueChanged -= OnHealthChanged;
    }

    void ProcessInput() {
        PerformGroundCheck();
        if (m_IsGrounded && m_FallVelocity < 0) {
            m_FallVelocity = -2f;
        }

        if (Input.GetKeyDown(KeyCode.R) && m_Health.Value <= 0) {
            Revive(transform.position, transform.rotation);
        }

        if (MovementLocked) {
            return;
        }

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        var direction = new Vector3(horizontalInput, 0, verticalInput);
        if (direction.magnitude > 1) {
            direction = Vector3.Normalize(direction);
        }

        Quaternion cameraDirection = CameraBrain.Instance.ActiveCameraTransform.rotation;
        direction = cameraDirection * direction;
        direction.y = 0;  // no flying allowed!

        var horizontalVelocity = direction * Time.deltaTime * m_MovementSpeed;
        m_Controller.Move(horizontalVelocity);
        HorizontalSpeed.Value = direction.magnitude;

        m_Controller.transform.LookAt(m_Controller.transform.position + direction);

        m_FallVelocity += GRAVITY * Time.deltaTime;
        m_Controller.Move(new Vector3(0, m_FallVelocity, 0) * Time.deltaTime);

        var p = new PlayerPos();
        p.Position = m_Controller.transform.position;
        p.Rotation = m_Controller.transform.rotation;

        if (Input.GetKeyDown(KeyCode.Q)) {
            if (!HasInventorySpace(Slot.PRIMARY)) {
                StartCoroutine(WaitForGround(0.5f));
            }
        }

        if (Input.GetKey(KeyCode.Alpha9)){
            if(CurrentRoom.RoomOxygen < 1f){
                CurrentRoom.RoomOxygen = CurrentRoom.RoomOxygen + 0.1f;
            }
        }

        if (Input.GetKey(KeyCode.Alpha0)){
            if(CurrentRoom.RoomOxygen > 0f){
                CurrentRoom.RoomOxygen = CurrentRoom.RoomOxygen - 0.1f;
            }
        }

        if (Input.mouseScrollDelta.y != 0) {
            SwapInventorySlots();
        }

        if (Input.GetKeyDown(KeyCode.Mouse1)) {
            if (!HasInventorySpace(Slot.PRIMARY)) {
                var item = GetInventoryItem(Slot.PRIMARY).GetComponentInChildren<InteractableBase>();
                if (item != null && item.LastUse + item.CooldownTime() < Time.fixedTime) {
                    PlayerAnimation anim = item.SelfInteraction(this);
                    AnimationController.TriggerAnimation(anim);
                    item.LastUse = Time.fixedTime;
                }
            }
        }

        UpdatePosServerRpc(p);
    }

    void UpdatePos() {
        transform.position = m_PlayerPos.Value.Position;
        transform.rotation = m_PlayerPos.Value.Rotation;
    }

    private void UpdateNameTag() {
        nameText.gameObject.transform.rotation = CameraBrain.Instance.ActiveCameraTransform.rotation;
        isready.transform.rotation = CameraBrain.Instance.ActiveCameraTransform.rotation;
        notready.transform.rotation = CameraBrain.Instance.ActiveCameraTransform.rotation;
        float distanceToCamera = Vector3.Distance(CameraBrain.Instance.ActiveCameraTransform.position, transform.position);
        Vector3 scale = new Vector3(distanceToCamera, distanceToCamera, distanceToCamera);
        nameText.gameObject.transform.localScale = scale * 0.002f;
        isready.transform.localScale = scale * 0.002f;
        notready.transform.localScale = scale * 0.002f;
    }

    private void UpdateHealthBar() {
        m_HealthBar.UpdateHealthBar(m_Health.Value);
        m_OxygenBar.UpdateHealthBar(IsOwner ? m_LungOxygen : 0f);
    }


    /* ================== MOVEMENT ================== */

    [ServerRpc]
    public void UpdatePosServerRpc(PlayerPos p) {
        m_PlayerPos.Value = p;
    }

    public void PerformGroundCheck() {
        m_IsGrounded = Physics.CheckSphere(groundCheck.position,
                GroundCheck.GROUND_CHECK_RADIUS,
                groundLayer
        );
    }

    public void Teleport(Vector3 pos, Quaternion rot) {
        bool prev_controller_enabled = m_Controller.enabled;
        m_Controller.enabled = false;
        transform.SetPositionAndRotation(pos, rot);
        m_Controller.enabled = prev_controller_enabled;
    }

    public void Teleport(Transform target) {
        Teleport(target.position, target.rotation);
    }

    [ClientRpc]
    public void TeleportClientRpc(PlayerPos target) {
        Teleport(target.Position, target.Rotation);
    }

    [ClientRpc]
    public void SetSpawnedClientRpc() {
        if (IsOwner) {
            Spawned.Value = true;
        }
    }

    public void LockMovement(int locker) {
        if (!m_MovementLocks.Contains(locker)) {
            m_MovementLocks.Add(locker);
        }
    }
    public void ReleaseMovementLock(int locker) {
        if (m_MovementLocks.Contains(locker)) {
            m_MovementLocks.Remove(locker);
        }
    }
    public bool MovementLocked {
        get => m_MovementLocks.Count > 0;
    }

    public void SpeedBoost() {
        if (m_SpeedBoostCoroutine == null) {
            m_SpeedBoostCoroutine = StartCoroutine(SpeedBoostCoroutine());
        }
    }
    IEnumerator SpeedBoostCoroutine() {
        float speed_prev = m_MovementSpeed;
        m_MovementSpeed *= 3f;
        yield return new WaitForSeconds(4);
        m_MovementSpeed = speed_prev;
        m_SpeedBoostCoroutine = null;
    }


    /* ================== CHARACTER SELECT ================== */

    // might not be called initially, i think.  not that that's super bad or anything, just worth keeping in mind
    public void OnCharacterChanged(int previous, int current) {
        m_CharacterList.transform.GetChild(previous).gameObject.SetActive(false);
        m_CharacterList.transform.GetChild(current).gameObject.SetActive(true);
        m_PlayerMesh = GetComponentInChildren<MeshRenderer>(includeInactive: false);
        SetupRagdoll();
    }

    public void SetActiveCharacter(int characterIndex) {
        m_ActiveCharacter.Value = characterIndex;
    }


    /* ================== AUDIO ================== */

    private AudioClip GetRandomCupDropClip() {
        return dropCupSounds[UnityEngine.Random.Range(0, dropCupSounds.Length)];
    }

    private AudioClip GetRandomMetalObjectDropClip() {
        return dropMetalObjectSounds[UnityEngine.Random.Range(0, dropMetalObjectSounds.Length)];
    }

    private AudioClip GetRandomDamageClip() {
        return Util.RandomChoice<AudioClip>(m_DamageSounds);
    }

    private AudioClip GetRandomDeathClip() {
        return Util.RandomChoice<AudioClip>(m_DeathSounds);
    }

    private AudioClip GetRandomFallClip() {
        return Util.RandomChoice<AudioClip>(m_FallSounds);
    }

    IEnumerator WaitForGround(float time){
        var item = GetInventoryItem(Slot.PRIMARY).tag;
        DropItem(Slot.PRIMARY);
        yield return new WaitForSeconds(time);
        if(item == "Cup"){
            AudioClip sound = GetRandomCupDropClip();
            audioSource.PlayOneShot(sound);
            Debug.Log("Cup Drop");
        }
        else{
            AudioClip sound = GetRandomMetalObjectDropClip();
            audioSource.PlayOneShot(sound);
            Debug.Log("Metal Object Drop");
        }
    }


    /* ================== HEALTH & OXYGEN ================== */

    public void OxygenRegulation(float delta_time){
        if (m_CurrentRoom == null) return;

        float breathing_rate = 0.6f * delta_time;
        float oxygen = ((m_LungOxygen - breathing_rate + ((1.1f * breathing_rate) * m_CurrentRoom.RoomOxygen)));

        m_LungOxygen = Mathf.Clamp(oxygen, 0f, 1f);

        if (m_LungOxygen < 0.2f) {
            float dps = 0.08f;
            TakeDamage(dps * delta_time);
        }
    }

    public void TakeDamage(float damage) {
        m_Health.Value = Mathf.Clamp(m_Health.Value - damage, 0f, 1f);
        if (!m_HasDied && m_LastDamageSound + DAMAGE_SOUND_COOLDOWN < Time.fixedTime) {
            audioSource.PlayOneShot(GetRandomDamageClip());
            m_LastDamageSound = Time.fixedTime;
        }
    }

    public void Revive(Vector3 position, Quaternion rotation) {
        Debug.Log(name + " revived");
        Teleport(position, rotation);
        m_HasDied = false;
        m_Health.Value = 1f;
        SetPlayerAlive(alive: true);
    }

    public void SetPlayerAlive(bool alive) {
        if (m_DeathReviveCoroutine == null) {
            audioSource.PlayOneShot(GetRandomDeathClip());
            audioSource.PlayOneShot(GetRandomFallClip());
            m_DeathReviveCoroutine = StartCoroutine(SetPlayerAliveCoroutine(alive, alive ? 0.0f : 2.0f));
        }
    }

    public IEnumerator SetPlayerAliveCoroutine(bool alive, float delay) {
        if (IsOwner) {
            if (alive) {
                ReleaseMovementLock(GetHashCode());
            } else {
                LockMovement(GetHashCode());
                DropAllItems();
            }
        }

        if (!alive) {
            m_Ragdoll.SetRagdollEnabled(true);
        }

        yield return new WaitForSeconds(delay);
        Canvas canvas = GetComponentInChildren<Canvas>();
        canvas.enabled = alive;
        m_CharacterList.SetActive(alive);
        if (!alive) {
            m_Ragdoll.SetRagdollEnabled(false);
        }

        if (IsOwner && !alive) {
            var net_ref = new NetworkObjectReference(NetworkObject);
            PlayerManager.Instance.SpawnRevivalFloppyServerRpc(net_ref, transform.position);
        }

        m_DeathReviveCoroutine = null;
    }

    private void OnHealthChanged(float health_prev, float health) {
        if (health <= 0 && !m_HasDied) {
            m_HasDied = true;
            SetPlayerAlive(alive: false);
        }
        if (health > 0 && health_prev <= 0 && m_HasDied) {
            m_HasDied = false;
            SetPlayerAlive(alive: true);
        }

        if (health < health_prev && !IsOwner) {  // played immediately for owner in TakeDamage
            if (m_LastDamageSound + DAMAGE_SOUND_COOLDOWN < Time.fixedTime) {
                audioSource.PlayOneShot(GetRandomDamageClip());
                m_LastDamageSound = Time.fixedTime;
            }
        }
    }


    private void SetupRagdoll() {
        m_Ragdoll.Setup(this, m_CharacterList);
        m_Ragdoll.SetRagdollEnabled(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReviveServerRpc(Vector3 position, Quaternion rotation) {
        ReviveClientRpc(position, rotation);
    }

    [ClientRpc]
    public void ReviveClientRpc(Vector3 position, Quaternion rotation) {
        if (IsOwner) {
            Revive(position, rotation);
        }
    }

    /* ================== HIDE PLAYER ================== */

    public void HidePlayer(bool on){
        if (!m_PlayerMesh) return; // too early
        if(on) {
            m_PlayerMesh.sharedMaterial = transparentMaterial;
        }
        else{
            m_PlayerMesh.sharedMaterial = normalMaterial;
        }
    }


    /* ================== INVENTORY ================== */

    private void ShowInInventory(Transform hand, NetworkObject item) {
        MeshRenderer itemRend = item.GetComponentInChildren<MeshRenderer>();
        MeshRenderer handRend = hand.GetComponent<MeshRenderer>();
        handRend.materials = itemRend.materials;
        handRend.GetComponent<MeshFilter>().mesh = itemRend.GetComponent<MeshFilter>().mesh;
        itemRend.enabled = false;

        DroppableInteractable interactable = item.GetComponentInChildren<DroppableInteractable>();

        bool primary = hand == PrimaryItemDisplay;
        var handRendPos = primary ? interactable.PrimaryPos : interactable.SecondaryPos;
        var handRendRot = primary ? interactable.PrimaryRot : interactable.SecondaryRot;
        handRend.transform.localPosition = handRendPos;
        Quaternion rot = Quaternion.identity;
        rot.eulerAngles = handRendRot;
        handRend.transform.localRotation = rot;

        // Make overall world scale of object in hand match the item's scale.
        float itemscale = SceneManager.GetActiveScene().name == "SampleScene" ? 0.4f: 1.0f;
        handRend.transform.localScale = Vector3.Scale(transform.localScale, item.transform.lossyScale) * itemscale;

        handRend.enabled = true;
    }
    private void HideInventorySlot(Transform hand) {
            MeshRenderer handRend = hand.GetComponent<MeshRenderer>();
            handRend.enabled = false;
    }

    // Called for both local and other players
    public void OnPrimaryItemChanged(NetworkObjectReference prev, NetworkObjectReference current) {
        if (Util.NetworkObjectReferenceIsEmpty(current)) {  // dropped item
            HideInventorySlot(PrimaryItemDisplay);
        } else {  // picked up item
            ShowInInventory(PrimaryItemDisplay, current);
        }
    }
    // Called for both local and other players
    public void OnSecondaryItemChanged(NetworkObjectReference prev, NetworkObjectReference current) {
        if (Util.NetworkObjectReferenceIsEmpty(current)) {  // dropped item
            HideInventorySlot(SecondaryItemDisplay);
        } else {  // picked up item
            ShowInInventory(SecondaryItemDisplay, current);
        }
    }

    public NetworkObject GetInventoryItem(Slot slot) {
        NetworkObjectReference reference;
        if (slot == Slot.PRIMARY) {
            reference = m_PrimaryItem.Value;
        } else {
            reference = m_SecondaryItem.Value;
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
            m_PrimaryItem.Value = item;
            ShowInInventory(PrimaryItemDisplay, item);  // optional, client-sided
        } else {
            m_SecondaryItem.Value = item;
            ShowInInventory(SecondaryItemDisplay, item);  // optional, client-sided
        }
        var interactable = item.GetComponentInChildren<DroppableInteractable>();
        if (!interactable) {
            Debug.LogError("Picked up " + item.name + " but no DroppableInteractable component found on it.");
        }
        interactable.InvokeOnPickup(this);
        InvokeOnPickupServerRpc(new NetworkObjectReference(item));
    }

    public void DropItem(Slot slot) {
        NetworkObjectReference item;
        if (slot == Slot.PRIMARY) {
            item = m_PrimaryItem.Value;
            HideInventorySlot(PrimaryItemDisplay);
        } else {
            item = m_SecondaryItem.Value;
            HideInventorySlot(SecondaryItemDisplay);
        }

        NetworkObject o;
        if (!item.TryGet(out o)) {
            Debug.Log("No item here.");
        }

        DroppableInteractable cup = o.GetComponentInChildren<DroppableInteractable>();
        cup.DropServerRpc(dropPoint.position);

        if (slot == Slot.PRIMARY) {
            m_PrimaryItem.Value = new NetworkObjectReference();
        } else {
            m_SecondaryItem.Value = new NetworkObjectReference();
        }

        AnimationController.TriggerAnimation(PlayerAnimation.INTERACT);
        cup.InvokeOnDrop(this);
        InvokeOnDropServerRpc(item);
    }

    public void DropAllItems() {
        if (!HasInventorySpace(Slot.PRIMARY)) {
            DropItem(Slot.PRIMARY);
        }
        if (!HasInventorySpace(Slot.SECONDARY)) {
            DropItem(Slot.SECONDARY);
        }
    }

    public void SwapInventorySlots() {
        NetworkObjectReference tmp = m_PrimaryItem.Value;
        m_PrimaryItem.Value = m_SecondaryItem.Value;
        m_SecondaryItem.Value = tmp;

        // ShowInInventory **should** be called automatically, because the networkvariable changed
        // TODO figure out if this is delayed or immediate

        // invoke events locally....
        GetInventoryItem(Slot.PRIMARY)
                ?.GetComponentInChildren<DroppableInteractable>()
                ?.InvokeOnSlotChange(this, Slot.PRIMARY);
        GetInventoryItem(Slot.SECONDARY)
                ?.GetComponentInChildren<DroppableInteractable>()
                ?.InvokeOnSlotChange(this, Slot.SECONDARY);
        // ...and for all other players
        InvokeOnSlotChangedServerRpc(m_PrimaryItem.Value, Slot.PRIMARY);
        InvokeOnSlotChangedServerRpc(m_SecondaryItem.Value, Slot.SECONDARY);
    }

    [ServerRpc]
    private void InvokeOnPickupServerRpc(NetworkObjectReference reference) {
        InvokeOnPickupClientRpc(reference);
    }

    [ClientRpc]
    private void InvokeOnPickupClientRpc(NetworkObjectReference reference) {
        if (IsOwner) {
            // OnPickup was already invoked purely client-side.
            return;
        }
        DroppableInteractable interactable = Util.GetDroppableInteractable(reference);
        interactable.InvokeOnPickup(this);
    }

    [ServerRpc]
    private void InvokeOnDropServerRpc(NetworkObjectReference reference) {
        InvokeOnDropClientRpc(reference);
    }

    [ClientRpc]
    private void InvokeOnDropClientRpc(NetworkObjectReference reference) {
        if (IsOwner) {
            // Ondrop was already invoked purely client-side.
            return;
        }
        DroppableInteractable interactable = Util.GetDroppableInteractable(reference);
        interactable.InvokeOnDrop(this);
    }

    [ServerRpc]
    private void InvokeOnSlotChangedServerRpc(NetworkObjectReference reference, Slot slot) {
        InvokeOnSlotChangedClientRpc(reference, slot);
    }

    [ClientRpc]
    private void InvokeOnSlotChangedClientRpc(NetworkObjectReference reference, Slot slot) {
        if (IsOwner) { return; }  // already invoked
        DroppableInteractable interactable = Util.GetDroppableInteractable(reference);
        interactable.InvokeOnSlotChange(this, slot);
    }

    public static bool IsHolding<T>() where T: DroppableInteractable {
        var primary_item = PlayerManager.Instance.LocalPlayer.Avatar.GetInventoryItem(PlayerAvatar.Slot.PRIMARY);
        return primary_item != null && primary_item.GetComponentInChildren<T>() != null;
    }
}
