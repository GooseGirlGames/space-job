using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GreenButton : MonoBehaviour {


    [SerializeField]
    private TMP_Text text;
    [SerializeField]
    private Image background;

    void Awake() {
        ChangeFontColorToGreen();
    }

    public void ChangeFontColorToBlack() {
        text.faceColor = Color.black;
        background.color = new Color(0.4f, 1.0f, 0.4f, 1.0f);
    }

    public void ChangeFontColorToGreen() {
        text.faceColor = new Color(0.4f, 1.0f, 0.4f, 1.0f);
        background.color = new Color(0f, 0f, 0f, 0.25f);
    }
}
