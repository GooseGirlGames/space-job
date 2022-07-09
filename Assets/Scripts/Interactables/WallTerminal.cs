using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WallTerminal : TwoLevelInteractable
{
    private Animator m_Visor;
    public TMP_Text StateDisplay;
    public override void Start() {
        base.Start();
        m_Visor = GetComponent<Animator>();
        if(m_InteractionRange)
        m_InteractionRange.OnRangeTriggerEnter += OnVisorTriggerEnter;
        m_InteractionRange.OnRangeTriggerExit += OnVisorTriggerExit;
        StateDisplay.text = ">closed";
        StateDisplay.color = new Color32(156, 156, 156, 255);
    }

    public void OnVisorTriggerEnter(Collider other){
        m_Visor.SetBool("active", true);
    }
    public void OnVisorTriggerExit(Collider other){
        m_Visor.SetBool("active", false);
    }

    public void DisplayDoorState(bool should_close){
        if(should_close){
            StateDisplay.text = ">close";
            StateDisplay.color = new Color32(156, 156, 156, 255);
        }
        else{
            StateDisplay.text = ">open";
            StateDisplay.color = new Color32(33, 169, 33, 255);
        }
    }
    public override string FriendlyName() {
        return "Door Terminal";
    }
}
