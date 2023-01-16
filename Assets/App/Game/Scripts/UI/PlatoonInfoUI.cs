using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlatoonInfoUI : MonoBehaviour
{
    [SerializeField] Button mainBtn;
    [SerializeField] Image image;
    [SerializeField] Text name;
    
    public void SetData(Action onClick, string text, Sprite icon)
    {
        name.text = text;

        image.sprite = icon;

        mainBtn.onClick.RemoveAllListeners();
        mainBtn.onClick.AddListener(() => onClick());
    }
}
