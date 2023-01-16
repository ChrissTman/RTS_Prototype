using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAutoSize : MonoBehaviour
{
    [SerializeField] float widthRatio;
    [SerializeField] float heightRatioToWidthRatio;
    RectTransform trans;
    void Awake()
    {
        trans = GetComponent<RectTransform>();
        if(trans == null)
        {
            Debug.LogError("UIAutoSize can't work without RectTransform");
            Destroy(this);
        }

        UpdateSize();
    }

    //TODO: remove
    //private void Update()
    //{
    //    UpdateSize();
    //}

    public void UpdateSize()
    {
        var deltaSize = trans.sizeDelta;

        deltaSize.x = Screen.width  / widthRatio;
        deltaSize.y = deltaSize.x / heightRatioToWidthRatio;

        trans.sizeDelta = deltaSize;
    }
}
