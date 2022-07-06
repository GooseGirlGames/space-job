using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class Util {
    public static Vector3 DivideElementwise(Vector3 a, Vector3 b) {
        var result = a;
        result.x /= b.x;
        result.y /= b.y;
        result.z /= b.z;
        return result;
    }

    public static Vector2 RandomVec2(float min, float max) {
        return new Vector2(
            Random.Range(min, max),
            Random.Range(min, max)
        );
    }



    public static float Frac(float a) {
        return a - (int)a;
    }

    public static bool NetworkObjectReferenceIsEmpty(NetworkObjectReference r) {
        NetworkObject _;
        return !r.TryGet(out _);
    }

    public static T RandomChoice<T>(List<T> list) {
        if (list.Count < 0) {
            Debug.LogError("RandomChoice called for an empty list of " + typeof(T) + ".  This will crash, goodbye.");
        }

        int idx = UnityEngine.Random.Range(0, list.Count);
        return list[idx];
    }

    public static T RandomChoice<T>(T[] elements) where T: UnityEngine.Object {
        int i = UnityEngine.Random.Range(0, elements.Length);
        return elements[i];
    }

    public static bool PlayerIsPressingMoveButton(InputActionReference action) {
        // meh
        //
        var move = action.action.ReadValue<Vector2>();
        return Mathf.Abs(move.x) > 0.05f || Mathf.Abs(move.y) > 0.05f;
    }

    public static bool PlayerIsPressingMoveButton() {
        return Mathf.Abs(Input.GetAxis("Horizontal")) > 0.5f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.5f;
    }

    public static DroppableInteractable GetDroppableInteractable(NetworkObjectReference reference) {
        NetworkObject o;
        if (reference.TryGet(out o)) {
            return GetDroppableInteractable(o);
        }
        return null;
    }

    public static DroppableInteractable GetDroppableInteractable(NetworkObject network_object) {
        return network_object?.GetComponentInChildren<DroppableInteractable>();
    }

    public static T TryGet<T>(NetworkObjectReference reference) where T: MonoBehaviour  {
        NetworkObject o;
        if (reference.TryGet(out o)) {
            T t = o.GetComponent<T>();
            if (t == null)
                t = o.GetComponentInChildren<T>();

            if (t != null)
                return t;
        }

        return null;
    }

    public static void BakeLighting() {
#if UNITY_EDITOR
        foreach (var scene in new string[]{"ShipScene"}) {
            EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
            Lightmapping.lightingSettings.lightmapMaxSize = 1024;
            Lightmapping.Bake();
        }
#endif
    }
}
