using Unity.Netcode;
using UnityEngine;
using System;
using UnityEngine.InputSystem;

public abstract class InteractableBase : NetworkBehaviour {

    public enum Mode { SINGLE_CLICK, HOLD_DOWN};
    protected Mode m_Mode = Mode.SINGLE_CLICK;  // set to HOLD_DOWN in Awake if needed.

    public const float DEFAULT_INTERACT_COOLDOWN_TIME = 1.0f;

    protected LayerMask m_HighlightedLayer;
    protected LayerMask m_DefaultLayer;
    protected Renderer m_Renderer;
    public bool NeedsPower = false;
    public PlayerAnimation TriggeredAnimation = PlayerAnimation.INTERACT;
    public float LastUse = 0;
    public bool CanInteract = true;
    public event Action OnDestroyCallback;

    /* === INPUT === */
    private InputActionReference m_InteractAction;

    // Called when item is held in hand and right mouse button pressed
    // Returns animation to play upon interaction
    public virtual PlayerAnimation SelfInteraction(PlayerAvatar avatar) {
        return PlayerAnimation.NONE;  // do nothing by default
    }

    protected abstract void Interaction();
    public virtual bool PlayerCanInteract() {
        return CanInteract && PlayerManager.Instance.LocalPlayer.Avatar.m_Health.Value > 0;
    }

    // Relevant only for HOLD_DOWN mode.  Called when mouse button is lifted.
    protected virtual void StopInteraction() {}

    // Override to set a different cooldown.
    // Also applied for SelfInteraction, over in PlayerAvatar.
    public virtual float CooldownTime() {
        return DEFAULT_INTERACT_COOLDOWN_TIME;
    }

    // Name of the interactable, as displayed in UI
    public virtual string FriendlyName() {
        return name;
    }

    public virtual void Start() {
        m_HighlightedLayer = LayerMask.NameToLayer("Highlighted");
        m_Renderer = GetComponent<Renderer>();
        if (!m_Renderer) {
            m_Renderer = GetComponentInChildren<Renderer>();
        }
        if (!m_Renderer) {
            m_Renderer = GetComponentInParent<Renderer>();
        }

        if (m_Renderer) {
            m_DefaultLayer = m_Renderer.gameObject.layer;
        } else {
            Debug.LogWarning("No renderer found for " + gameObject.name);
        }
        m_InteractAction = GameManager.Instance.InteractAction;
    }

    public override void OnDestroy() {
        OnDestroyCallback?.Invoke();
        base.OnDestroy();
    }

    public void SetHighlight(bool highlighted) {
        m_Renderer.gameObject.layer = highlighted ? m_HighlightedLayer : m_DefaultLayer;
    }


    protected void OnMouseOver() {
        SetHighlight(PlayerCanInteract());

        if (PlayerCanInteract()) {
            GameManager.Instance.ControllerInput.InteractableSelectedWithMouse = this;
        }

        if (PlayerCanInteract() && (!NeedsPower || ShipManager.Instance.HasPower)) {
            bool playAnim = false;
            if (m_InteractAction.action.WasPressedThisFrame() && LastUse + CooldownTime() < Time.fixedTime) {
                Interaction();
                playAnim = true;
                LastUse = Time.fixedTime;
            } else if (m_Mode == Mode.HOLD_DOWN && m_InteractAction.action.WasReleasedThisFrame()){
                StopInteraction();
                playAnim = true;
            }

            if (playAnim) {
                PlayerManager.Instance.LocalPlayer.Avatar.AnimationController.TriggerAnimation(TriggeredAnimation);
            }
        }
    }
    protected void OnMouseExit() {
        SetHighlight(false);
        StopInteraction();
        if (GameManager.Instance.ControllerInput.InteractableSelectedWithMouse == this) {
            GameManager.Instance.ControllerInput.InteractableSelectedWithMouse = null;
        }
    }

    public virtual void Update() {
    }

}
