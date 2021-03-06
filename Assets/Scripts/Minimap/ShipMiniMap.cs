using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;

public class ShipMiniMap : MonoBehaviour {
    // NOTE: Y is up in Unity!

    public static Vector3 CAL_POINT_A_WORLD = new Vector3(0, 0, 0);
    public static Vector3 CAL_POINT_A_CANVAS = new Vector3(-0.426f, 27.125f, 0);
    public static Vector3 CAL_POINT_B_WORLD = new Vector3(15, 0, -14);
    public static Vector3 CAL_POINT_B_CANVAS = new Vector3(36.307f, -7.15f, 0);

    public static Color m_AliveColor = new Color(0.4f, 1.0f, 0.4f);
    public static Color m_DeadColor = new Color(1.0f, 0.4f, 0.4f);

    [SerializeField]
    private List<RectTransform> m_PlayerIcons = new List<RectTransform>();
    private List<TMP_Text> m_PlayerNames = new List<TMP_Text>();

    public WinScreen m_WinScreen;

    [SerializeField]
    private TMP_Text m_AlertRed;
    [SerializeField]
    private TMP_Text m_AlertGreen;

    public void ShowAlert(string text, bool green, float duration_secs = 3f) {
        var t = green ? m_AlertGreen : m_AlertRed;
        StartCoroutine(ShowAlertCoroutine(t, text, duration_secs));
    }

    private IEnumerator ShowAlertCoroutine(TMP_Text text, string message, float duration) {
        string prev = text.text;
        text.text += message + "\n";
        yield return new WaitForSeconds(duration);
        text.text = "";
    }

    void Awake() {
        foreach (var icon in m_PlayerIcons) {
            // TODO!! handle re-joining players
            m_PlayerNames.Add(icon.GetComponentInChildren<TMP_Text>());
        }
    }

    void Update() {
        int i = 0;
        int dead = 0;
        foreach (var pplayer in PlayerManager.Instance.Players) {
            if (pplayer.PlayerName == PlayerAvatar.SPECTATOR_NAME) {
                continue;
            }
            var ava = pplayer.Avatar;
            if (i >= m_PlayerIcons.Count) {
                Debug.LogWarning("More players than minimap icons!");
                continue;
            }
            if (ava) {
                m_PlayerIcons[i].gameObject.SetActive(true);
                m_PlayerIcons[i].localPosition = WorldToCanvas(ava.transform.position);
                m_PlayerNames[i].text = pplayer.PlayerName;
                if (ava.m_Health.Value > 0) {
                    m_PlayerNames[i].color = m_AliveColor;
                }
                else {
                    m_PlayerNames[i].color = m_DeadColor;
                    dead++;
                }
                ++i;
            }
        }
        if (dead == i && i != 0) {
            m_WinScreen.SetEnabled(true, false);
        }
        while (i < m_PlayerIcons.Count) {
            m_PlayerIcons[i].gameObject.SetActive(false);
            ++i;
        }
    }

    public static Vector3 WorldToCanvas(Vector3 world_3d) {
        float t_x = (world_3d.x - CAL_POINT_A_WORLD.x) / (CAL_POINT_B_WORLD.x - CAL_POINT_A_WORLD.x);
        float x = Mathf.LerpUnclamped(CAL_POINT_A_CANVAS.x, CAL_POINT_B_CANVAS.x, t_x);
        float t_y = (world_3d.z - CAL_POINT_A_WORLD.z) / (CAL_POINT_B_WORLD.z - CAL_POINT_A_WORLD.z);
        float y = Mathf.LerpUnclamped(CAL_POINT_A_CANVAS.y, CAL_POINT_B_CANVAS.y, t_y);
        return new Vector3(x, y, 0);
    }
}
