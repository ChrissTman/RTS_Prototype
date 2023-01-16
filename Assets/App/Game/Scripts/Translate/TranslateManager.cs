using System;
using System.Collections;
using System.Collections.Generic;

public class TranslateManager
{
    public static string GetTranslation(string translateTag)
    {
        return "<n/a>";
    }
}

public interface ITranslatableUI
{
    void RefreshTranslation();
}