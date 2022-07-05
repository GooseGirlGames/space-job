using System.Collections.Generic;
using UnityEngine.EventSystems;
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


    void Awake() {
        m_Buttons = new List<Button>(m_InteractionCanvas.GetComponentsInChildren<Button>());
    }

    void Update() {
        string text = "SELECTED UI ELEMENT:\n";
        text += EventSystem.current.currentSelectedGameObject.name + "\n\n";
        text += "AVAILABLE INTERACTABLES:\n";
        foreach (var i in m_AvailableInteractables) {
            text += i.name + "\n";
        }
        m_DebugText.text = text;
    }

    void OnGUI() {
        int interactable_idx = 0;
        foreach (var button in m_Buttons) {
            var interactable = interactable_idx < m_AvailableInteractables.Count ? m_AvailableInteractables[interactable_idx] : null;
            if (interactable) {
                if (!button.gameObject.activeSelf) {
                    button.gameObject.SetActive(true);
                }
                DrawInteractionUI(interactable, button);
                if (interactable_idx == 0 && !EventSystem.current.currentSelectedGameObject) {  // TODO and selected object not visible
                    EventSystem.current.SetSelectedGameObject(button.gameObject);
                }
            } else {
                button.gameObject.SetActive(false);
            }
            ++interactable_idx;
        }
    }

    public void MarkInteractableAvailable(InteractableBase interactable) {
        m_AvailableInteractables.Add(interactable);
        interactable.OnDestroyCallback += () => {
            MarkInteractableUnavailable(interactable);
        };
    }

    public bool MarkInteractableUnavailable(InteractableBase interactable) {
        return m_AvailableInteractables.Remove(interactable);
    }

    private void DrawInteractionUI(InteractableBase interactable, Button button) {
        var cam = CameraBrain.Instance.OutputCamera;
        var pos_screen = cam.WorldToScreenPoint(interactable.transform.position);

        button.transform.position = pos_screen;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => {
            interactable.Invoke("Interaction", 0);  // irgh
        });

    }
}
