using UnityEngine;
using UnityEngine.UI;

public class WinScreen : MonoBehaviour {
    private Button[] m_Buttons;
    private Canvas m_Canvas;

    void Start() {
        m_Canvas = GetComponent<Canvas>();
        m_Buttons = GetComponentsInChildren<Button>(includeInactive: true);
    }

    public void OnButtonPress() {
        ShipManager.Instance.StartNewGameServerRpc();
    }

    public void SetEnabled(bool enabled) {
        foreach (var button in m_Buttons) {
            button.gameObject.SetActive(enabled);
        }
        m_Canvas.enabled = enabled;
    }
}
