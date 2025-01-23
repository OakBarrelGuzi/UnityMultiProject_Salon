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
            UIManager.Instance.CloseAllPanels();
            ScenesManager.Instance.ChanageScene("LobbyScene");
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
