using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ViewsDialogPanelScript : MenuPanelScript
{
    public Button DebugViewButton;
    void Start()
    {
        UpdateDevView();
    }

    public void UpdateDevView(){
        bool validDevMode = Manager.CurrentDevMode != DevMode.None;
        DebugViewButton.gameObject.SetActive(validDevMode);
    }
}
