using UnityEngine;
using UnityEngine.UI;

public class InteractableExitCanvas : MonoBehaviour {

    private Canvas m_Canvas;
    private Button[] m_Buttons;

    public static InteractableExitCanvas Instance;

    void Awake() {
        m_Canvas = GetComponent<Canvas>();
        m_Buttons = GetComponentsInChildren<Button>(includeInactive: true);
        Instance = this;
    }

    public void SetVisible(bool visible) {
        m_Canvas.enabled = visible;
        foreach (var button in m_Buttons) {
            button.gameObject.SetActive(visible);
        }
    }

    public void OnButtonPress() {
        TwoLevelInteractable.TryExitLocalPlayerFromActive();
    }
}
