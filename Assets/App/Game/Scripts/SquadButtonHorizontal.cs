using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquadButtonHorizontal : MonoBehaviour
{
    [SerializeField] RectTransform target;

    SquadButton[] buttons;
    public void Initialize(Slot<SquadButton>[] buttonSlots, bool applyParent = true)
    {
        buttons = new SquadButton[buttonSlots.Length];
        
        for (int i = 0; i < buttonSlots.Length; i++)
        {
            var elem = buttonSlots[i].Element;

            if(applyParent)
                elem.transform.SetParent(target);

            elem.RectTransform.pivot = Vector2.zero;

            elem.SetState(false);
            buttons[i] = elem;
        }
    }

    public void UpdateOrder()
    {
        float maxWidth = target.rect.width;
        float maxHeight = target.rect.height;
        int enabledElemenets = 0;

        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            if (btn.State)
                enabledElemenets++;
        }

        for (int i = 0, x = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            var elemetWidth = maxWidth / enabledElemenets;
            if (btn.State)
            {
                var trans = btn.RectTransform;
                
                var size = btn.RectTransform.sizeDelta;
                var pos = btn.RectTransform.anchoredPosition;

                size.x = elemetWidth;
                size.y = maxHeight;

                pos.x = x * elemetWidth;
                pos.y = 0;

                trans.sizeDelta = size;
                trans.anchoredPosition = pos;
                
                x++;
            }
        }
    }
}
