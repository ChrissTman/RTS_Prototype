using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class SquadButton : MonoBehaviour
{
    [SerializeField] Image background;
    [SerializeField] RawImage icon;
    [SerializeField] Text text;

    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
    }

    public Action OnClick;
    public RectTransform RectTransform { get; private set; }
    public bool State {get; private set;}
    public void SetState(bool state)
    {
        State = state;
        background.enabled = state;
        text.enabled = state;
        icon.enabled = state;
    }

    public void SetText(string s)
    {
        text.text = s;
    }

    public void SetIcon(Sprite sprite)
    {
        icon.texture = sprite.texture;
    }

    public void OnClick_UI()
    {
        OnClick?.Invoke();
    }
}
