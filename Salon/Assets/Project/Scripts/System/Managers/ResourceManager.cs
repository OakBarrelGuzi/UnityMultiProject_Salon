using UnityEngine;
using System.Collections.Generic;

public class ResourceManager : DataManager
{
    private static ResourceManager instance;
    private Dictionary<string, Object> cachedResources = new Dictionary<string, Object>();

    public static ResourceManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("ResourceManager");
                instance = go.AddComponent<ResourceManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
            Destroy(gameObject);
    }

    public override void LoadData()
    {

    }

    public override string ProcessPath(string fullPath)
    {
        string resourcePath = fullPath;
        int resourcesIndex = fullPath.IndexOf("Resources/");

        if (resourcesIndex != -1)
            resourcePath = fullPath.Substring(resourcesIndex + 10);

        int extensionIndex = resourcePath.LastIndexOf('.');
        if (extensionIndex != -1)
            resourcePath = resourcePath.Substring(0, extensionIndex);

        return resourcePath;
    }

    public T LoadResource<T>(string fullPath) where T : Object
    {
        string processedPath = ProcessPath(fullPath);

        if (cachedResources.TryGetValue(processedPath, out Object cachedResource))
        {
            return cachedResource as T;
        }
        T resource = Resources.Load<T>(processedPath);
        if (resource == null)
        {
            Debug.LogError($"리소스를 찾을 수 없습니다. 경로: {fullPath}");
            return null;
        }

        cachedResources[processedPath] = resource;
        return resource;
    }

    public void ClearCache()
    {
        cachedResources.Clear();
        Resources.UnloadUnusedAssets();
    }
}