using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//TODO: rename to Unit UI indicator
public class UnitUI : MonoBehaviour
{
    public Text Text;
    public Image HealthIndicator;
    public Image background;

    public UIAutoSize UIAutoSize;
    
    public bool State;
    public bool Highlight;

    public void SetText(string text)
    {
        Text.text = text;
    }

    public void SetHealthIndicator(float scale)
    {
        scale = Mathf.Clamp01(scale);

        HealthIndicator.transform.localScale = new Vector3(scale, 1, 1);
    }

    public void SetHighlight(bool state)
    {
        if(Highlight != state)
        {
            Highlight = state;
            var a = background.color.a;
            background.color = state ? new Color(1,1,1,a): new Color(0,0,0, a);
            
        }
    }

    public void Activate()
    {
        State = true;
        Text.enabled = true;
        HealthIndicator.enabled = true;
        background.enabled = true;

        UIAutoSize.UpdateSize();
    }
    public void Deactivate()
    {
        //transform.position = Vector2.one * -500;

        State = false;
        Text.enabled = false;
        HealthIndicator.enabled = false;
        background.enabled = false;
    }
}
