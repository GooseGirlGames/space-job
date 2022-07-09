using UnityEngine;

public abstract class RangedInteractableBase : InteractableBase {
    protected InteractionRange m_InteractionRange;
    protected bool m_IsInArea = false;

    public override void Start() {
        base.Start();

        m_InteractionRange = GetComponent<InteractionRange>();
        if (!m_InteractionRange) {
            m_InteractionRange = GetComponentInChildren<InteractionRange>();
        }
        if (m_InteractionRange) {
            m_InteractionRange.OnRangeTriggerEnter += OnRangeTriggerEnter;
            m_InteractionRange.OnRangeTriggerExit += OnRangeTriggerExit;
        } else {
            Debug.LogWarning("No interactionrange found for " + gameObject.name);
        }
    }

    protected override bool PlayerCanInteract() {
        return m_IsInArea && base.PlayerCanInteract();
    }

    public void OnRangeTriggerEnter(Collider other) {
        PlayerAvatar player = other.GetComponent<PlayerAvatar>();
        if(player != null && player.IsOwner){
            m_IsInArea = true;
            if (PlayerCanInteract()) {
                GameManager.Instance.ControllerInput.MarkInteractableAvailable(this);
            }
        }
    }

    // Called by InteractionRange
    public void OnRangeTriggerExit(Collider other) {
        PlayerAvatar player = other.GetComponent<PlayerAvatar>();
        if(player != null && player.IsOwner){
            m_IsInArea = false;
            GameManager.Instance.ControllerInput.MarkInteractableUnavailable(this);
        }
    }

}
