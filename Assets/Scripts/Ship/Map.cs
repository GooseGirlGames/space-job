using System.Collections;
using UnityEngine;

public enum Event {
    POWER_OUTAGE,
    COSMIC_HORROR,
    NONE,
}

public struct MapState {
    public Event ev;
    public float risk;
    // TODO Biomes
}

public class Map : MonoBehaviour {
    public Texture2D MapTexture;
    public Texture2D MinimapTexture;

    [SerializeField]
    private float m_MinimapZoom = 6f;

    void Start() {
        StartCoroutine(SpeedBoostCoroutine());
    }

    public MapState GetState(Vector2 pos) {
        MapState state = new MapState();
        if (pos.x < 0 || pos.y < 0 || pos.x > 1024 || pos.y > 1024) {
            state.ev = Event.COSMIC_HORROR;
            state.risk = 1;
        } else {
            Color color = MapTexture.GetPixel(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
            state.ev = color.g > 0.5 ? Event.POWER_OUTAGE : Event.NONE;
            state.risk = color.r;
        }
        return state;
    }

    public void UpdateMinimap(Vector2 shipPos) {
        Vector2 dims = new Vector2(MinimapTexture.width, MinimapTexture.height);
        for (int x = 0; x < MinimapTexture.width; ++x) {
            for (int y = 0; y < MinimapTexture.width; ++y) {
                Vector2 offset = new Vector2(x, y) - (0.5f * dims);
                float risk = GetState(shipPos + m_MinimapZoom * offset).risk;
                Color col = new Color(0.2f, risk, 0.2f);
                if (offset.magnitude <= 2) {
                    col.r = 0.6f;
                }
                MinimapTexture.SetPixel(x, y, col);
            }
        }
        MinimapTexture.Apply();
    }

    IEnumerator SpeedBoostCoroutine() {
        while (true) {
            UpdateMinimap(ShipManager.Instance.GetShipPosition());
            yield return new WaitForSeconds(5);
        }
    }
}
