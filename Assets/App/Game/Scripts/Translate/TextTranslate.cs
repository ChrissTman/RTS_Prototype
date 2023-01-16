using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TextTranslate : MonoBehaviour, ITranslatableUI
{
    [SerializeField] string translateTag;
    [SerializeField] Text textLabel;

    public void RefreshTranslation()
    {
        string translation = TranslateManager.GetTranslation(translateTag);

    }
}
