using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TextLoader : MonoBehaviour
{
    private TMP_FontAsset defaultTMPFont;
    private Font defaultLegacyFont;

#if UNITY_EDITOR
    [MenuItem("GameObject/UI/Text Loader", false, 10)]
    private static void CreateTextLoader(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("Text Loader");
        go.AddComponent<TextLoader>();
        
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        
        Undo.RegisterCreatedObjectUndo(go, "Create Text Loader");
        Selection.activeObject = go;
    }
#endif

    void Start()
    {
        LoadTexts();
    }

    public void LoadTexts()
    {
        if (defaultTMPFont == null)
        {
            defaultTMPFont = ResourceManager.Instance.LoadResource<TMP_FontAsset>("Assets/Resources/Fonts/MainFontTmp.asset");
        }

        if (defaultLegacyFont == null)
        {
            defaultLegacyFont = ResourceManager.Instance.LoadResource<Font>("Assets/Resources/Fonts/MainFontLegacy.ttf");
        }

        Text[] uiTexts = GetComponentsInChildren<Text>(true);
        TextMeshProUGUI[] tmpTexts = GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (Text uiText in uiTexts)
        {
            string localizedText = UIManager.Instance.GetText(uiText.gameObject.name);
            uiText.text = localizedText;

            if (defaultLegacyFont != null)
                uiText.font = defaultLegacyFont;
        }

        foreach (TextMeshProUGUI tmpText in tmpTexts)
        {
            string localizedText = UIManager.Instance.GetText(tmpText.gameObject.name);
            tmpText.text = localizedText;

            if (defaultTMPFont != null)
                tmpText.font = defaultTMPFont;
        }
    }

    public void UpdateTexts()
    {
        LoadTexts();
    }
}