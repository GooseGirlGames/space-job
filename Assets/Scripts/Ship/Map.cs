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
}
