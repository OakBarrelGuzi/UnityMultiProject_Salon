using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

namespace Salon.DartGame
{

    public class DartResultPanel : MonoBehaviour, IPointerClickHandler
    {
        public TextMeshProUGUI bestScoreText;
        public TextMeshProUGUI scoreText;

        public void OnPointerClick(PointerEventData eventData)
        {
            ScenesManager.Instance.ChanageScene("LobbyScene");
            gameObject.SetActive(false);
        }

        public void Textset(int score)
        {
            bestScoreText.text = score.ToString();
            scoreText.text = score.ToString();

        }
        public void Textset(int bestscore, int score)
        {
            bestScoreText.text = bestscore.ToString();
            scoreText.text = scoreText.ToString();
        }
    }
}
