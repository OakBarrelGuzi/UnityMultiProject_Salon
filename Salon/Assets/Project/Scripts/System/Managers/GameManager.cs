using UnityEngine;

public class GameManager : MonoBehaviour
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

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManagers();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeManagers()
    {
        _ = LogManager.Instance;
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