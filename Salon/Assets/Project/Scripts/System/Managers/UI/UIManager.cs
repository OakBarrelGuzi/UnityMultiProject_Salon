using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Salon.Interfaces;
using Salon.Firebase;
using System;
using Salon.System;
using System.Threading.Tasks;
using Firebase.Database;

public class UIManager : Singleton<UIManager>, IInitializable
{
    private DataManager.DataFile[] LanguageFile;
    private Dictionary<string, string> processedData = new Dictionary<string, string>();
    private string DefaultLangFile = "Assets/Resources/Langtable/LangTable.CSV";
    private List<Panel> panels = new List<Panel>();
    [SerializeField] private List<Panel> panelsPrefabs = new List<Panel>();
    public bool IsInitialized { get; private set; }
    public bool isTesting;

    public async void Start()
    {
        OpenPanel(PanelType.Loading);
        Initialize();
        if (!isTesting)
        {
            await InitializeAsync();
        }
    }

    private async Task InitializeAsync()
    {
        while (!FirebaseManager.Instance.IsInitialized)
        {
            await Task.Delay(100);
        }

        try
        {
            await Task.Delay(2000);

            if (FirebaseManager.Instance.CurrentUserUID != null)
            {
                // 현재 로그인된 사용자의 상태 확인
                DatabaseReference userRef = FirebaseManager.Instance.DbReference.Child("Users")
                    .Child(FirebaseManager.Instance.CurrentUserUID)
                    .Child("Status");

                DataSnapshot snapshot = await userRef.GetValueAsync();

                if (snapshot.Exists)
                {
                    int status = Convert.ToInt32(snapshot.Value);
                    if (status == 1) // UserStatus.Online = 1
                    {
                        FirebaseManager.Instance.SignOut();
                        LogManager.Instance.ShowLog("다른 기기에서 로그인된 상태여서 로그아웃되었습니다.");
                        OpenPanel(PanelType.MainDisplay);
                        OpenPanel(PanelType.SignIn);
                    }
                    else
                    {
                        OpenPanel(PanelType.MainDisplay);
                    }
                }
                else
                {
                    OpenPanel(PanelType.MainDisplay);
                    OpenPanel(PanelType.SignIn);
                }
            }
            else
            {
                OpenPanel(PanelType.MainDisplay);
                OpenPanel(PanelType.SignIn);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UIManager] 초기화 중 오류 발생: {ex.Message}");
            OpenPanel(PanelType.SignIn);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (panels.FirstOrDefault(p => p.panelType == PanelType.Option) != null)
            {
                if (panels.FirstOrDefault(p => p.panelType == PanelType.Option).isOpen)
                    ClosePanel(PanelType.Option);
                else
                    OpenPanel(PanelType.Option);
            }
            else
            {
                OpenPanel(PanelType.Option);
            }
        }
    }

    public void Initialize()
    {
        if (LanguageFile == null)
            LanguageFile = new DataManager.DataFile[0];

        LoadDefaultLanguageSystem();
        InitializeLanguageSystem();

        IsInitialized = true;
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
            return;
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

    public void OpenPanel(PanelType panelType)
    {
        if (panels.FirstOrDefault(p => p.panelType == panelType) == null)
        {
            var newPanel = GetPanel(panelType);
            panels.Add(newPanel);
            newPanel.Open();
            newPanel.transform.SetAsLastSibling();
        }
        else
        {
            var existingPanel = panels.FirstOrDefault(p => p.panelType == panelType);
            if (!existingPanel.isOpen)
            {
                existingPanel.Open();
                existingPanel.transform.SetAsLastSibling();
            }
        }
    }

    public void ClosePanel(PanelType panelType)
    {
        var panel = panels.FirstOrDefault(p => p.panelType == panelType);
        if (panel == null)
        {
            LogManager.Instance.ShowLog($"패널 {panelType}이 존재하지 않습니다.");
            return;
        }
        if (panel.isOpen)
        {
            panel.Close();
        }
    }

    private Panel GetPanel(PanelType panelType)
    {
        var prefab = panelsPrefabs.FirstOrDefault(p => p.panelType == panelType);
        if (prefab == null)
        {
            Debug.LogError($"[UIManager] {panelType} 프리팹을 찾을 수 없습니다.");
            return null;
        }

        return Instantiate(prefab, transform);
    }

    public void CloseAllPanels()
    {
        Debug.Log("[UIManager] CloseAllPanels 호출");
        foreach (Panel panel in panels)
        {
            panel.Close();
        }
        Debug.Log("[UIManager] CloseAllPanels 완료");
    }

    public Panel GetPanelByType(PanelType panelType)
    {
        return panels.FirstOrDefault(p => p.panelType == panelType);
    }

    public T GetUI<T>() where T : Panel
    {
        return panels.FirstOrDefault(p => p is T) as T;
    }
}