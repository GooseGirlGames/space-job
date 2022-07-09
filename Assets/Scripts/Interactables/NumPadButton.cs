using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NumPadButton : InteractableBase
{
    public int Number;
    public bool IsPowerTerminal;
    public NumPad VendingNumPad;
    public PowerTerminal PowerTerminal;
    protected override void Interaction(){
        if(Number == 420){
            VendingNumPad.ClearInput();
        }
        else if(Number == 69){
            VendingNumPad.ClearInput();
        }
        else if(Number == 21){
            PowerTerminal.ClearInput();
        }
        else if(Number == 99){
            PowerTerminal.ClearInput();
        }
        else if(IsPowerTerminal){
            PowerTerminal.DisplayInputText(Number);
        }
        else{
            VendingNumPad.DisplayInputText(Number);
        }
    }
    public override float CooldownTime()
    {
        return 0.0f;
    }
    public override string FriendlyName() {
        return Number.ToString();
    }
}
