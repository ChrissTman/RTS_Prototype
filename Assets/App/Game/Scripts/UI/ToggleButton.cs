using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : MonoBehaviour
{
    [Tooltip("Set to null to ignore it.")][SerializeField] Image image;
    [Tooltip("Set to null to ignore it.")][SerializeField] Text text;

    [SerializeField] Color toggledColor;
    [SerializeField] Color normalColor;

    [SerializeField] string textToggled;
    [SerializeField] string textNormal;

    [SerializeField] bool initializeInverted;

    public Action<ToggleButton> OnClick;
    public bool Toggled { get; set; }

    private void Awake()
    {
        if (initializeInverted)
            Toggled = true;

        ApplyChanges();
    }

    public void Click()
    {
        Toggled = !Toggled;

        ApplyChanges();

        OnClick?.Invoke(this);
    }

    public void ApplyChanges()
    {
        if (image != null)
            image.color = Toggled ? toggledColor : normalColor;
        if (text != null)
            text.text = Toggled ? textToggled : textNormal;
    }
}
