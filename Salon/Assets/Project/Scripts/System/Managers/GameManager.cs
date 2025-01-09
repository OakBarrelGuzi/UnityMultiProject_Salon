using UnityEngine;
using Salon.Interfaces;
using Salon.Firebase;

public class GameManager : MonoBehaviour, IInitializable
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("GameManager");
                instance = go.AddComponent<GameManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    public bool IsInitialized { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        _ = Instance;
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void Initialize()
    {
        InitializeManagers();
        IsInitialized = true;
    }

    private void InitializeManagers()
    {
        _ = FirebaseManager.Instance;
        _ = UIManager.Instance;
        _ = ResourceManager.Instance;
        _ = CSVManager.Instance;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("GameObject/Managers/Game Manager", false, 10)]
    private static void CreateGameManager(UnityEditor.MenuCommand menuCommand)
    {
        GameObject go = new GameObject("GameManager");
        go.AddComponent<GameManager>();

        UnityEditor.GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Game Manager");
        UnityEditor.Selection.activeObject = go;
    }
#endif
}