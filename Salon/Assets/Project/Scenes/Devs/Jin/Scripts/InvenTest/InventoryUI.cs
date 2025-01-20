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
        private Button invenExitButton;
        [SerializeField]
        private GameObject emojiInven;
        [SerializeField]
        private GameObject animeInven;

        [SerializeField]
        private GameObject emojiPopupPanel;
        [SerializeField]
        private GameObject animePopupPanel;
        private void Awake()
        {
            invenExitButton.onClick.AddListener(()=>gameObject.SetActive(false));
            emojiInvenButton.onClick.AddListener(() => 
            {
                emojiInven.SetActive(true);
                emojiPopupPanel.SetActive(true);
                animeInven.SetActive(false);
                animePopupPanel.SetActive(false);
            });
            animeInvenButton.onClick.AddListener(() =>
            {
                animeInven.SetActive(true);
                animePopupPanel.SetActive(true);
                emojiInven.SetActive(false);
                emojiPopupPanel.SetActive(false);
            } );

            emojiInven.SetActive(true);
            emojiPopupPanel.SetActive(true );
            animeInven.SetActive(false);
            animePopupPanel.SetActive(false);
        }
    }
}
