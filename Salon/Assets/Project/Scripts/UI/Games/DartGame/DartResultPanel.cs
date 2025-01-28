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

        public Image recordImage;

        public TextMeshProUGUI newRecordText;

        public Sprite[] recordsprite;

        public ParticleSystem recordParticle;
        public void OnPointerClick(PointerEventData eventData)
        {
            gameObject.SetActive(false);
            UIManager.Instance.CloseAllPanels();
            ScenesManager.Instance.ChanageScene("LobbyScene");
        }

        public void Textset(int score)
        {
            recordParticle.gameObject.SetActive(true);
            recordParticle.Play();
            bestScoreText.text = score.ToString();
            scoreText.text = score.ToString();
            newRecordText.text = "NEW RECORD";
            recordImage.sprite = recordsprite[0];

        }
        public void Textset(int bestscore, int score)
        {
            recordParticle.gameObject.SetActive(false);
            bestScoreText.text = bestscore.ToString();
            scoreText.text = score.ToString();
            newRecordText.text = "";
            recordImage.sprite = recordsprite[1];
        }
    }
}
