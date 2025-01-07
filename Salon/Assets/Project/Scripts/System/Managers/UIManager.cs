using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    private static UIManager instance;
    [SerializeField] private DataManager.DataFile[] LanguageFile;
    private Dictionary<string, string> processedData = new Dictionary<string, string>();

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

        InitializeLanguageSystem();
    }

    private void InitializeLanguageSystem()
    {
        foreach (var langFile in LanguageFile)
        {
            CSVManager.Instance.LoadAdditionalCSV(langFile.path);
            LoadLanguageData(langFile.path);
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
        foreach (LocalizedText text in FindObjectsOfType<LocalizedText>())
            text.UpdateText();
    }

    public void ReloadLanguageData()
    {
        foreach (var langFile in LanguageFile)
            LoadLanguageData(langFile.path);
        UpdateAllLocalizedTexts();
    }
}