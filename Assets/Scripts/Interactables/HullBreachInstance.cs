using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class HullBreachInstance : RangedInteractableBase {

    private Room m_Room;
    private Transform m_SpawnLocation;
    private EventParameters.HullBreachSize m_Size = EventParameters.HullBreachSize.SMALL;
    private bool m_PlayerIsHoldingPlate = false;  // server-only!
    private Coroutine m_GrowCoroutine;
    private PlayerAnimation m_DefaultAnim;
    private bool m_LocalPlayerHoldingPlate = false;

    private readonly float LARGE_HULLBREACH_SCALE = 3.2f;

    public const float BASE_DRAIN = 0.4f;
    public const float GROW_INTERVAL_SECS = 45f;

    public void Awake() {
        m_DefaultAnim = TriggeredAnimation;
    }

    public override string FriendlyName() {
        string size = "";
        if (m_Size == EventParameters.HullBreachSize.LARGE) {
            size = "Large ";
        }
        return size + "Hull Breach";
    }

    public float DrainFactor() {
        return BASE_DRAIN * (m_Size == EventParameters.HullBreachSize.LARGE ? 1.5f : 1f);
    }

    public override void OnDestroy() {
        var ava = PlayerManager.Instance.LocalPlayer.Avatar;
        if (ava) {
            ava.AnimationController.SetBool(PlayerAminationBool.HOLDING_PLATE, false);
            if (m_LocalPlayerHoldingPlate) {
                var plate = Util.GetDroppableInteractable(ava.GetInventoryItem(PlayerAvatar.Slot.PRIMARY)).GetComponent<MetalPlate>();
                if (plate != null) {
                    plate.DespawnServerRpc();
                    // plate.StickToWallServerRpc(transform.position, transform.rotation);
                }
            }
            ava.ReleaseMovementLock(GetHashCode());
        }
        base.OnDestroy();
    }

    public override float CooldownTime() {
        return 2.0f;
    }

    protected override void Interaction() {
        if (m_Size == EventParameters.HullBreachSize.SMALL) {
            InteractionSmall();
        } else {
            InteractionLarge();
        }
    }

    private void InteractionSmall() {
        if (PlayerAvatar.IsHolding<BreachBeGone>()) {
            var ava = PlayerManager.Instance.LocalPlayer.Avatar;
            ava.AnimationController.TriggerAnimation(PlayerAnimation.SPRAY);
            Debug.Log("Avatar: " + ava + " ItemSolot: "+ ava.GetInventoryItem(PlayerAvatar.Slot.PRIMARY).GetComponent<BreachBeGone>());
            ava.GetInventoryItem(PlayerAvatar.Slot.PRIMARY).GetComponent<BreachBeGone>().DespawnServerRpc();
            ResolvedServerRpc();
        }
    }

    private void InteractionLarge() {
        TriggeredAnimation = m_DefaultAnim;
        if (PlayerAvatar.IsHolding<MetalPlate>()) {
            TriggeredAnimation = PlayerAnimation.NONE;
            StartCoroutine(HoldPlate());
        } else if (PlayerAvatar.IsHolding<PipeWrench>()) {  // TODO add welder
            AttemptWeldPlateServerRpc();
        }
    }

    private IEnumerator HoldPlate() {
        var ava = PlayerManager.Instance.LocalPlayer.Avatar;
        m_LocalPlayerHoldingPlate = true;
        ava.AnimationController.SetBool(PlayerAminationBool.HOLDING_PLATE, true);
        ava.LockMovement(GetHashCode());
        SetIsHoldingServerRpc(true);
        yield return new WaitForSeconds(1.5f);
        ava.AnimationController.SetBool(PlayerAminationBool.HOLDING_PLATE, false);
        yield return new WaitForSeconds(0.5f);
        ava.ReleaseMovementLock(GetHashCode());
        SetIsHoldingServerRpc(false);
        m_LocalPlayerHoldingPlate = false;
    }

    // called by room
    public void Setup(Room room, Transform spawn) {
        m_Room = room;
        name = "Hullbreach (" + room.name + ")";
        m_SpawnLocation = spawn;
        if (IsServer) {
            m_GrowCoroutine = StartCoroutine(Grow());
        }
    }

    private IEnumerator Grow() {
        Debug.Log("Hullbrach " + name + " spawned.");
        yield return new WaitForSeconds(GROW_INTERVAL_SECS + Random.Range(-5f, 5f));
        Debug.Log("Hullbrach " + name + " expanded.");
        GrowHoleClientRpc(EventParameters.HullBreachSize.LARGE);
    }

    [ClientRpc]
    public void GrowHoleClientRpc(EventParameters.HullBreachSize new_size) {
        m_Size = new_size;
        if (new_size == EventParameters.HullBreachSize.LARGE) {
            transform.localScale = LARGE_HULLBREACH_SCALE * transform.localScale;
        }
    }

    [ServerRpc(RequireOwnership=false)]
    public void ResolvedServerRpc() {
        Debug.Log("Hull Breach fixed in " + m_Room);
        StopAllCoroutines();
        m_Room.HullBreachResolved(this, m_SpawnLocation);
        GetComponent<NetworkObject>().Despawn(destroy: true);  // breach do be gone
    }

    [ServerRpc(RequireOwnership=false)]
    public void SetIsHoldingServerRpc(bool holding) {
        m_PlayerIsHoldingPlate = holding;
    }

    [ServerRpc(RequireOwnership=false)]
    public void AttemptWeldPlateServerRpc() {
        if (m_PlayerIsHoldingPlate) {
            ResolvedServerRpc();
        }
    }

    public override void Update() {
        base.Update();
#if !DISABLE_DEBUG_KEYS
        if (Input.GetKeyDown(KeyCode.M)) {
            AttemptWeldPlateServerRpc();
        }
#endif
    }
}
