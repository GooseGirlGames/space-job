using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ControllerInteractionButton : MonoBehaviour {
    private Button m_Button;
    public Button Button { get => m_Button; }

    private TMP_Text m_Text;

    void Awake() {
        m_Button = GetComponent<Button>();
        m_Text = GetComponentInChildren<TMP_Text>();
    }

    public void SetText(string text) {
        m_Text.text = text;
    }

    void OnGUI() {
        m_Text.enabled = m_Button.gameObject == EventSystem.current.currentSelectedGameObject;
    }
}
