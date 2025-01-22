using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Salon.DartGame
{
    public class DartRoundPanel : MonoBehaviour, IPointerClickHandler
    {
        public TextMeshProUGUI gameStartTostText;

        public float WaitTime { get; set; } = 5f;

        public bool Gamestart { get; set; } = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            Gamestart = true;
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            Gamestart = false;
            StartCoroutine(GameStartRoutine());
        }

        private IEnumerator GameStartRoutine()
        {
            yield return new WaitForSeconds(WaitTime);
            Gamestart = true;
            gameObject.SetActive(false);
        }
    }
}