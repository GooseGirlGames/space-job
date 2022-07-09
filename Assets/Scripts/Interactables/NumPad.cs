using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NumPad : TwoLevelInteractable
{
    public TMP_Text InputDisplay;
    private int m_InCounter = 0;
    public override void Start() {
        base.Start();
        ClearInput();   
    }
    public void DisplayInputText(int input_number){
        if(m_InCounter == 4){
            ClearInput();
        }
        InputDisplay.text += input_number;
        m_InCounter ++;
    }
    public void ClearInput(){
        InputDisplay.text = ">";
        m_InCounter = 0;
    }
    public override string FriendlyName() {
        return "Numpad";
    }
}
