using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LogManager : MonoBehaviour
{
    private static LogManager instance;
    public static LogManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("LogManager");
                instance = go.AddComponent<LogManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        _ = ResourceManager.Instance;
        _ = Instance;
    }

    private GameObject logPanelPrefab;
    private LogPanel currentLogPanel;
    private Queue<(string message, string stackTrace)> errorQueue = new Queue<(string message, string stackTrace)>();
    private bool isShowingError = false;

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

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        Debug.Log("LogManager 초기화 시작");
        Application.logMessageReceived += HandleLog;

        logPanelPrefab = ResourceManager.Instance.LoadResource<GameObject>("Log/LogPanel");
        if (logPanelPrefab == null)
        {
            Debug.LogError("LogPanel 프리팹을 찾을 수 없습니다.");
        }
        else
        {
            Debug.Log("LogManager 초기화 완료");
        }
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            ShowLog(logString, stackTrace);
        }
    }

    public void ShowLog(string errorMessage, string stackTrace = "")
    {
        errorQueue.Enqueue((errorMessage, stackTrace));
        if (!isShowingError)
        {
            ShowNextError();
        }
    }

    private void ShowNextError()
    {
        if (errorQueue.Count == 0)
        {
            isShowingError = false;
            return;
        }

        isShowingError = true;
        var (message, stackTrace) = errorQueue.Dequeue();

        if (currentLogPanel == null)
        {
            if (logPanelPrefab != null)
            {
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    GameObject canvasObj = new GameObject("Canvas");
                    canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                    CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);

                    canvasObj.AddComponent<GraphicRaycaster>();

                    DontDestroyOnLoad(canvasObj);
                }

                GameObject logPanelObj = Instantiate(logPanelPrefab, canvas.transform);
                currentLogPanel = logPanelObj.GetComponent<LogPanel>();

                if (currentLogPanel == null)
                {
                    Debug.LogError("LogPanel 컴포넌트를 찾을 수 없습니다.");
                    return;
                }
            }
            else
            {
                Debug.LogError("LogPanel 프리팹이 로드되지 않았습니다.");
                return;
            }

        }

        currentLogPanel.ShowError(message, stackTrace, () =>
        {
            ShowNextError();
        });
    }

    public void ClearErrors()
    {
        errorQueue.Clear();
        if (currentLogPanel != null)
        {
            Destroy(currentLogPanel.gameObject);
            currentLogPanel = null;
        }
        isShowingError = false;
    }
}