using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LocalizedText : MonoBehaviour
{
    private TMP_FontAsset defaultTMPFont;
    private Font defaultLegacyFont;

    [Tooltip("텍스트 데이터 설정\n- TextObject: 텍스트를 표시할 UI 오브젝트\n- textKey: CSV 파일의 Idx 열에 있는 키 값")]
    public LocalizedTextData[] data;

    void Start()
    {
        UpdateText();
    }

    public void UpdateText()
    {
        if (defaultTMPFont == null)
        {
            defaultTMPFont = ResourceManager.Instance.LoadResource<TMP_FontAsset>("Assets/Resources/Fonts/MainFontTmp.asset");
        }

        if (defaultLegacyFont == null)
        {
            defaultLegacyFont = ResourceManager.Instance.LoadResource<Font>("Assets/Resources/Fonts/MainFontLegacy.ttf");
        }

        foreach (LocalizedTextData textData in data)
        {
            string localizedText = UIManager.Instance.GetText(textData.textKey);

            var uiText = textData.TextObject.GetComponent<Text>();
            var tmpText = textData.TextObject.GetComponent<TextMeshProUGUI>();

            if (uiText != null)
            {
                uiText.text = localizedText;
                if (defaultLegacyFont != null)
                    uiText.font = defaultLegacyFont;
            }
            if (tmpText != null)
            {
                tmpText.text = localizedText;
                if (defaultTMPFont != null)
                    tmpText.font = defaultTMPFont;
            }
        }
    }
}

[System.Serializable]
public struct LocalizedTextData
{
    [Tooltip("텍스트를 표시할 UI 오브젝트 (Text 또는 TextMeshProUGUI 컴포넌트가 있어야 함)")]
    public GameObject TextObject;

    [Tooltip("CSV 파일의 Idx 열에 있는 키 값")]
    public string textKey;
}
