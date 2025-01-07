using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    private static UIManager instance;
    [SerializeField] private DataManager.DataFile[] LanguageFile;
    private Dictionary<string, string> processedData = new Dictionary<string, string>();
    private string DefaultLangFile = "Assets/Resources/Langtable/LangTable.CSV";

    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("UIManager");
                instance = go.AddComponent<UIManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (LanguageFile == null)
            LanguageFile = new DataManager.DataFile[0];

        LoadDefaultLanguageSystem();
        InitializeLanguageSystem();
    }

    private void LoadDefaultLanguageSystem()
    {
        if (string.IsNullOrEmpty(DefaultLangFile))
        {
            Debug.LogWarning("기본 언어 파일 경로가 설정되지 않았습니다.");
            return;
        }

        if (CSVManager.Instance.LoadAdditionalCSV(DefaultLangFile))
        {
            LoadLanguageData(DefaultLangFile);
        }
        else
        {
            Debug.LogError($"기본 언어 파일을 로드하는데 실패했습니다: {DefaultLangFile}");
        }
    }

    private void InitializeLanguageSystem()
    {
        if (LanguageFile == null || LanguageFile.Length == 0)
        {
            return;
        }

        foreach (var langFile in LanguageFile)
        {
            if (langFile.path == null)
            {
                Debug.LogWarning("언어 파일 경로가 null입니다.");
                continue;
            }

            if (CSVManager.Instance.LoadAdditionalCSV(langFile.path))
            {
                LoadLanguageData(langFile.path);
            }
            else
            {
                Debug.LogError($"언어 파일을 로드하는데 실패했습니다: {langFile.path}");
            }
        }
    }

    private void LoadLanguageData(string path)
    {
        List<string[]> csvData = CSVManager.Instance.GetDataSet(path);
        if (csvData == null || csvData.Count < 3)
        {
            Debug.LogError($"언어 데이터를 불러오는데 실패했습니다: {path}");
            return;
        }

        for (int i = 2; i < csvData.Count; i++)
        {
            string[] row = csvData[i];
            if (row.Length >= 3 && !string.IsNullOrEmpty(row[0]))
            {
                processedData[row[0]] = row[2];
            }
        }
    }

    public string GetText(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("텍스트 키가 null이거나 비어있습니다.");
            return "Missing Key";
        }

        if (processedData.TryGetValue(key, out string value))
        {
            return value;
        }
        Debug.LogWarning($"키를 찾을 수 없습니다: {key}");
        return "Missing Text";
    }

    public string GetText(string fileName, string key)
    {
        return GetText(key);
    }

    public void UpdateAllLocalizedTexts()
    {
        var texts = FindObjectsOfType<LocalizedText>();
        if (texts == null || texts.Length == 0)
        {
            Debug.Log("업데이트할 LocalizedText가 없습니다.");
            return;
        }

        foreach (LocalizedText text in texts)
        {
            if (text != null)
                text.UpdateText();
        }
    }

    public void ReloadLanguageData()
    {
        processedData.Clear();
        LoadDefaultLanguageSystem();

        if (LanguageFile != null)
        {
            foreach (var langFile in LanguageFile)
            {
                if (langFile.path != null)
                    LoadLanguageData(langFile.path);
            }
        }

        UpdateAllLocalizedTexts();
    }
}