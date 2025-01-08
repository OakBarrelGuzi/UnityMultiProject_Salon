using UnityEngine;
using System.Collections.Generic;

public class GameDataManager : MonoBehaviour
{
    private static GameDataManager instance;
    [SerializeField] private DataManager.DataFile[] GameDataFiles;

    private Dictionary<string, float> gameValues = new Dictionary<string, float>();

    public static GameDataManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("GameDataManager");
                instance = go.AddComponent<GameDataManager>();
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

        InitializeGameData();
    }

    private void InitializeGameData()
    {
        foreach (var dataFile in GameDataFiles)
        {
            CSVManager.Instance.LoadAdditionalCSV(dataFile.path);
            LoadGameData(dataFile.path);
        }
    }

    private void LoadGameData(string path)
    {
        List<string[]> csvData = CSVManager.Instance.GetDataSet(path);
        if (csvData == null || csvData.Count < 3)
        {
            Debug.LogError($"게임 데이터를 불러오는데 실패했습니다: {path}");
            return;
        }

        string tableName = System.IO.Path.GetFileNameWithoutExtension(path);

        for (int i = 2; i < csvData.Count; i++)
        {
            string[] row = csvData[i];
            if (row.Length >= 3 && !string.IsNullOrEmpty(row[0]))
            {
                string key = $"{tableName}_{row[0]}";

                if (float.TryParse(row[2], out float value))
                {
                    gameValues[key] = value;
                }
            }
        }
    }

    public float GetValue(string tableName, string key, float defaultValue = 0f)
    {
        string fullKey = $"{tableName}_{key}";
        if (gameValues.TryGetValue(fullKey, out float value))
        {
            return value;
        }
        Debug.LogWarning($"데이터를 찾을 수 없습니다: 테이블={tableName}, 키={key}");
        return defaultValue;
    }

    public void ReloadGameData()
    {
        gameValues.Clear();
        foreach (var dataFile in GameDataFiles)
        {
            LoadGameData(dataFile.path);
        }
    }
}
