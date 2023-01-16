using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ToggleButtonArray : MonoBehaviour
{
    public List<GameObject> toggleButtonRoots;

    List<ToggleButton> buttons = new List<ToggleButton>();
    void Awake()
    {
        foreach (var root in toggleButtonRoots)
        {
            buttons.AddRange(root.GetComponentsInChildren<ToggleButton>());
        }

        foreach(var btn in buttons)
        {
            btn.OnClick += OnButtonClick;
        }
    }

    void OnButtonClick(ToggleButton toggledButton)
    {
        foreach(var btn in buttons)
        {
            if(btn != toggledButton)
            {
                btn.Toggled = false;
                btn.ApplyChanges();
            }
        }
    }

}
