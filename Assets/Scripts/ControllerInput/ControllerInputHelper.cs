using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;
using TMPro;

public class ControllerInputHelper : MonoBehaviour {
    [SerializeField]
    private TMP_Text m_DebugText;
    [SerializeField]
    private Canvas m_InteractionCanvas;

    private List<InteractableBase> m_AvailableInteractables = new List<InteractableBase>();
    private List<Button> m_Buttons = new List<Button>();


    void Awake() {
        m_Buttons = new List<Button>(m_InteractionCanvas.GetComponentsInChildren<Button>());
    }

    void Update() {
        string text = "AVAILABLE INTERACTABLES:\n";
        foreach (var i in m_AvailableInteractables) {
            text += i.name + "\n";
        }
        m_DebugText.text = text;
    }

    void OnGUI() {
        foreach (var button in m_Buttons) {
            button.transform.position = new Vector2(-1000, -1000);
            button.interactable = false;
            button.enabled = false;
        }
        int button_idx = 0;
        foreach (var interactable in m_AvailableInteractables) {
            DrawInteractionUI(interactable, m_Buttons[button_idx]);
            if (button_idx == 0 && !EventSystem.current.currentSelectedGameObject) {  // TODO and selected object not visible
                Debug.Log("selceted: " + m_Buttons[button_idx].gameObject);
                EventSystem.current.SetSelectedGameObject(m_Buttons[button_idx].gameObject);
            }
            if (++button_idx >= m_Buttons.Count) {
                break;
            }
        }
    }

    public void MarkInteractableAvailable(InteractableBase interactable) {
        m_AvailableInteractables.Add(interactable);
    }

    public bool MarkInteractableUnavailable(InteractableBase interactable) {
        return m_AvailableInteractables.Remove(interactable);
    }

    private void DrawInteractionUI(InteractableBase interactable, Button button) {
        var cam = CameraBrain.Instance.OutputCamera;
        var pos_screen = cam.WorldToScreenPoint(interactable.transform.position);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => {
            interactable.Invoke("Interaction", 0);  // irgh
        });

        button.transform.position = pos_screen;
        button.interactable = true;
        button.enabled = true;
    }
}