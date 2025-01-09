using UnityEngine;
using System.Collections.Generic;
using Salon.Interfaces;
public class DataManager : MonoBehaviour, IInitializable
{
    [System.Serializable]
    public struct DataFile
    {
        public string path;
    }

    protected List<DataFile> dataFiles = new List<DataFile>();
    protected Dictionary<string, List<string[]>> csvDataSets = new Dictionary<string, List<string[]>>();

    public bool IsInitialized { get; private set; }

    public virtual void Initialize() { }

    public virtual void LoadData() { }
    public virtual string ProcessPath(string fullPath)
    {
        string resourcePath = fullPath;
        int resourcesIndex = fullPath.IndexOf("Resources/");

        if (resourcesIndex != -1)
            resourcePath = fullPath.Substring(resourcesIndex + 10);

        return resourcePath;
    }

    public List<string[]> GetDataSet(string path)
    {
        string processedPath = ProcessPath(path);
        if (csvDataSets.ContainsKey(processedPath))
            return csvDataSets[processedPath];
        return null;
    }

    public void ClearDataSet(string path)
    {
        string processedPath = ProcessPath(path);
        if (csvDataSets.ContainsKey(processedPath))
            csvDataSets.Remove(processedPath);
    }

    public void ClearAllData()
    {
        csvDataSets.Clear();
        dataFiles.Clear();
    }
}