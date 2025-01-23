using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Salon.Firebase.Database;
using TMPro;

public class Item : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI Components")]
    public Image itemIcon;

    public ItemData itemData;

    private RectTransform rectTransform;
    private Canvas canvas;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalPosition;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public virtual void Initialize(ItemData data)
    {
        // 아이템 데이터 복사
        itemData = new ItemData()
        {
            itemName = data.itemName,
            itemType = data.itemType,
            itemCost = data.itemCost,
        };

        // 아이템 아이콘 설정
        itemIcon.sprite = ItemManager.Instance.GetItemSprite(itemData.itemName);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalPosition = rectTransform.anchoredPosition;

        // 드래그 중인 아이템을 캔버스의 최상위로 이동
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();

        // 드래그 중인 아이템의 레이캐스트 차단
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out Vector2 localPoint))
        {
            rectTransform.position = canvas.transform.TransformPoint(localPoint);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 원래 위치로 복귀
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
        rectTransform.anchoredPosition = originalPosition;

        // 레이캐스트 차단 해제
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
    }
}