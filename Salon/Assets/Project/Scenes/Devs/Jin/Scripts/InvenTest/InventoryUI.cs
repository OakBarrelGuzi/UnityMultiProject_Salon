using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Salon.Inven
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField]
        private Button emojiInvenButton;
        [SerializeField]
        private Button animeInvenButton;
        [SerializeField]
        private GameObject emojiInven;
        [SerializeField]
        private GameObject animeInven;

        private void Awake()
        {
            emojiInvenButton.onClick.AddListener(OnEmojinButtonClick);
            animeInvenButton.onClick.AddListener(OnAnimeButtonClick);

            emojiInven.SetActive(false);
            animeInven.SetActive(false);
        }



        private void OnEmojinButtonClick()
        {
            if (emojiInven.gameObject.activeSelf == false)
            {
                emojiInven.SetActive(true);
                animeInven.SetActive(false);
            }
        }
        private void OnAnimeButtonClick()
        {
            if (animeInven.gameObject.activeSelf == false)
            {
                animeInven.SetActive(true);
                emojiInven.SetActive(false);
            }
        }
    }
}
