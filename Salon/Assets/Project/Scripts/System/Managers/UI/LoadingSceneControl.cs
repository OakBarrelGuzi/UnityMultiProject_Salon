using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneControl : MonoBehaviour
{
    public Image progressBar;
    public Image backgroundImage; // 배경 이미지를 변경할 UI 이미지
    public Sprite[] ImagaeSprite;
    static string nextScene;

    //LoadingSceneController.LoadScene("씬 이름") -> 로딩으로 넘어갈거면
    private void Start()
    {
        SetBackgroundImage();
        StartCoroutine(LoadingSceneProgress());
    }
    public static void LoadScene(string sceneName)
    {
        nextScene = sceneName;
        SceneManager.LoadScene("LoadingScene");
    }
    private void SetBackgroundImage()
    {
        if (backgroundImage != null)
        {
            //if (nextScene == "씬이름")
            //{
            //    backgroundImage.sprite = ImagaeSprite[0];
            //}
        }
    }
    public IEnumerator LoadingSceneProgress()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(nextScene);
        //리소스 로딩이 끝나기 전에 Scene Loading이 끝나면 로딩되지 않은 오브젝트 깨지는 현상 방지
        op.allowSceneActivation = false;

        float timer = 0f;
        while (!op.isDone)
        {
            yield return null;
            if (op.progress < 0.9f)
            {
                progressBar.fillAmount = op.progress;
            }
            else
            {
                timer += Time.unscaledDeltaTime;
                progressBar.fillAmount = Mathf.Lerp(0.9f, 1f, timer);
                if (progressBar.fillAmount >= 1f)
                {
                    op.allowSceneActivation = true;
                    yield break;
                }
            }

        }
    }
}
