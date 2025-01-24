using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingPanel : Panel
{
    public Slider loadingSlider;

    public TextMeshProUGUI loadingText;
    private Canvas panelCanvas;

    public override void Open()
    {
        base.Open();
        panelCanvas = GetComponent<Canvas>();
        if (panelCanvas == null)
            panelCanvas = gameObject.AddComponent<Canvas>();

        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 9999;
        StartCoroutine(StartLoading());
    }

    public IEnumerator StartLoading()
    {
        float elapsedTime = 0f;
        float duration = 3f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            UpdateLoadingText(progress);
            UpdateLoadingSlider(progress);

            yield return null;
        }

        // 마지막에 정확히 100%로 설정
        UpdateLoadingText(1f);
        UpdateLoadingSlider(1f);

        Close();
    }

    public void UpdateLoadingText(float progress)
    {
        loadingText.text = $"{Mathf.Round(progress * 100)}%";
    }

    public void UpdateLoadingSlider(float progress)
    {
        loadingSlider.value = progress;
    }

    public override void Close()
    {
        StopAllCoroutines();
        base.Close();
    }

}
