using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[AddComponentMenu("UI/Text Loader - 텍스트 자동 로드")]
[DisallowMultipleComponent]
public class TextLoader : MonoBehaviour
{
    [Header("사용설명서")]
    [TextArea(10, 10)]
    [SerializeField]
    private string usageDescription;

    private readonly string defaultUsageDescription = @"[TextLoader 컴포넌트 사용 방법]

1. 이 오브젝트는 UI 오브젝트의 최상위의 부모 오브젝트여야 합니다.

2. 하위에 있는 모든 Text/TextMeshPro 컴포넌트들의 텍스트를 자동으로 로드합니다.

3. 텍스트 ID는 각 텍스트 오브젝트의 이름을 기준으로 합니다.
   - 예시: 오브젝트 이름이 'Title_Text'면 이 ID로 텍스트를 찾습니다.

4. 폰트는 Resources 폴더의 지정된 경로에서 자동으로 로드됩니다.
   - TMP: Assets/Resources/Fonts/MainFontTmp.asset
   - Legacy: Assets/Resources/Fonts/MainFontLegacy.ttf";

    public TextLoader()
    {
        usageDescription = defaultUsageDescription;
    }

    [Header("폰트 설정")]
    private TMP_FontAsset defaultTMPFont;
    private Font defaultLegacyFont;

#if UNITY_EDITOR
    [MenuItem("GameObject/UI/Text Loader", false, 10)]
    private static void CreateTextLoader(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("Text_Loader");
        go.AddComponent<RectTransform>();
        go.AddComponent<TextLoader>();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

        Undo.RegisterCreatedObjectUndo(go, "Create Text Loader");
        Selection.activeObject = go;
    }

    private void OnValidate()
    {
        if (usageDescription != defaultUsageDescription)
        {
            usageDescription = defaultUsageDescription;
        }
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