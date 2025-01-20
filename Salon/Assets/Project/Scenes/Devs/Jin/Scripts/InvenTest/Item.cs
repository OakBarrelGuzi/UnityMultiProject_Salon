using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Salon.Inven
{
    public class Item : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private ItemData ItemData;
        public ItemData itemData => ItemData;

        public Image image;

        public Sprite itemImage;

        public RectTransform rectTransform { get; private set; }

        public Canvas canvas { get; private set; }
        private Transform parent;
        private int siblingIndex;

        public Vector2 originalPosition { get; private set; }
        
        //테스트용
        //private void Start()
        //{
        //    rectTransform = GetComponent<RectTransform>();
        //    canvas = GetComponentInParent<Canvas>();
        //    if (this.ItemData == null)
        //    {
        //        ItemData data = new ItemData()
        //        {
        //            itemCost = Random.Range(0, 100),
        //            itemName = "안녕",
        //            itemType = itemType.Emoji,
        //        };
        //        this.ItemData = data;
        //    }
        //    if (itemImage != null)
        //    {
        //        image.sprite = itemImage;
        //    }
        //}


        public virtual void Initialize(ItemData itemData)
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();

            ItemData data = new ItemData()
            {
                itemCost = itemData.itemCost,
                itemName = itemData.itemName,
                itemType = itemData.itemType,
            };
            this.ItemData = data;

            if (itemImage != null)
            {
                image.sprite = itemImage;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            parent = transform.parent;
            siblingIndex = transform.GetSiblingIndex();
            originalPosition = rectTransform.anchoredPosition;
            transform.SetParent(canvas.transform);

            image.raycastTarget = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 localPoint;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out localPoint
            );
            rectTransform.position = canvas.transform.TransformPoint(localPoint);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {

            transform.SetParent(parent);
            transform.SetSiblingIndex(siblingIndex);
            rectTransform.anchoredPosition = originalPosition;

            image.raycastTarget = true;
        }
    }
}