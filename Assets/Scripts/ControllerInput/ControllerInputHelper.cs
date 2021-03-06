using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class ControllerInputHelper : MonoBehaviour {

    [SerializeField]
    private TMP_Text m_DebugText;
    [SerializeField]
    private Canvas m_InteractionCanvas;

    private List<InteractableBase> m_AvailableInteractables = new List<InteractableBase>();
    private List<Button> m_Buttons = new List<Button>();

    public InteractableBase InteractableSelectedWithMouse;

    public InputActionReference
            MoveActionUI,
            CancelActionUI;

    void Awake() {
        m_Buttons = new List<Button>(m_InteractionCanvas.GetComponentsInChildren<Button>());
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.F12)) {
            m_DebugText.gameObject.SetActive(!m_DebugText.IsActive());
        }
        if (!m_DebugText.IsActive()) {
            return;
        }
        string text = "";
        text += "SHIP SPEED:  " + ShipManager.Instance?.GetShipSpeed();
        text += "\nSHIP ANGVEL:  " + ShipManager.Instance?.GetShipAngleSpeed();
        var pos = ShipManager.Instance.GetShipPosition();
        text += "\nSHIP POS:  " + pos.x + ", " + pos.y;
        text += "\nDISTANCE TO GOAL:  " + ShipManager.Instance?.GetDistantToWin();
        text += "\nRISK LEVEL:  " + ShipManager.Instance?.GetComponent<EventManager>().CurrentRisk();
        text += "\nDICE:  " + ShipManager.Instance?.GetComponent<EventManager>().DiceRollDebugInfo;
        text += "\n\nSELECTED UI ELEMENT:\n";
        text += EventSystem.current.currentSelectedGameObject + "\n\n";
        text += "AVAILABLE INTERACTABLES (" + m_AvailableInteractables.Count + ", " + m_AvailableInteractables.GetHashCode() + "):\n";
        foreach (var i in m_AvailableInteractables) {
            text += i.FriendlyName() + "\n";
        }
        var map = GameManager.Instance.Input.currentActionMap;
        text += "\n\nACTION MAP:  " + map;
        var scheme = GameManager.Instance.Input.currentControlScheme;
        text += "\n\nCONTROLE SCHEME:  " + scheme;
        var players = PlayerManager.Instance?.Players;
        text += "\n\nPLAYERS REGISTERED:  n=" + players.Count + "(";
        foreach (var p in players) {
            text += p.PlayerName + "; ";
        }
        text += ")";

        m_DebugText.text = text;
    }

    void OnGUI() {
        if (SceneManager.GetActiveScene().name == "MainMenu") {
            return;
        }

        if (PlayerManager.Instance?.LocalPlayer?.Avatar?.m_Health?.Value <= 0f) {
            foreach (var button in m_Buttons) {
                button.gameObject.SetActive(false);
            }
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }

        int interactable_idx = 0;
        foreach (var button in m_Buttons) {
            var interactable = interactable_idx < m_AvailableInteractables.Count ? m_AvailableInteractables[interactable_idx] : null;
            if (interactable && interactable.PlayerCanInteract()) {
                if (!button.gameObject.activeSelf) {
                    button.gameObject.SetActive(true);
                }
                DrawInteractionUI(interactable, button);
                if (Util.UsingGamepad() && interactable_idx == 0 &&
                        (!EventSystem.current.currentSelectedGameObject
                         || !EventSystem.current.currentSelectedGameObject.activeSelf)) {  // TODO??? and selected object not visible
                    EventSystem.current.SetSelectedGameObject(button.gameObject);
                }

                if (Util.UsingGamepad()) {
                    bool selected = EventSystem.current.currentSelectedGameObject.gameObject == button.gameObject;
                    interactable?.SetHighlight(selected && interactable.PlayerCanInteract());
                } else {
                    if (InteractableSelectedWithMouse != null &&
                            interactable.gameObject == InteractableSelectedWithMouse.gameObject) {
                        EventSystem.current.SetSelectedGameObject(button.gameObject);
                    }
                }
            } else {
                if (Util.UsingGamepad()) {
                    interactable?.SetHighlight(false);
                }
                button.gameObject.SetActive(false);
            }

            if (!Util.UsingGamepad() && InteractableSelectedWithMouse == null) {
                EventSystem.current.SetSelectedGameObject(null);
            }

            ++interactable_idx;
        }
    }

    public void MarkInteractableAvailable(InteractableBase interactable) {
        if (m_AvailableInteractables.Contains(interactable)) {
            return;
        }
        m_AvailableInteractables.Add(interactable);
        interactable.OnDestroyCallback += () => {
            MarkInteractableUnavailable(interactable);
        };
    }

    public bool MarkInteractableUnavailable(InteractableBase interactable) {
        if (Util.UsingGamepad()) {
            interactable.SetHighlight(false);
        }
        return m_AvailableInteractables.Remove(interactable);
    }

    private void DrawInteractionUI(InteractableBase interactable, Button button) {
        var pos_world = new Vector3();
        var colliders = interactable.GetComponents<Collider>();
        if (colliders.Length > 0) {
            foreach (var collider in colliders) {
                pos_world += collider.bounds.center;
            }
            pos_world /= colliders.Length;
        } else {
            pos_world = interactable.transform.position;
        }

        var cam = CameraBrain.Instance.OutputCamera;
        var pos_screen = cam.WorldToScreenPoint(pos_world);

        button.transform.position = pos_screen;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => {
                if (interactable.m_Mode == InteractableBase.Mode.SINGLE_CLICK
                        && (!interactable.NeedsPower || ShipManager.Instance.HasPower)) {
                    interactable.Invoke("Interaction", 0);  // irgh
                }
        });

        var interactbutton = button.GetComponent<ControllerInteractionButton>();
        interactbutton.SetText(interactable.FriendlyName());
    }

    public void ClearAvailableInteractables() {
        m_AvailableInteractables.Clear();
    }
}
