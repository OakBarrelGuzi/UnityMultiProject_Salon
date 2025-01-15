using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShellGameUIManager : MonoBehaviour
{
    private ShuffleManager shuffleManager;

    [SerializeField]
    private Button[] difficulty_Button;

    [Header("Panel Setting")]
    public GameObject difficult_Panel;
    public GameObject betting_Panel;
    public GameObject gameInfo_Panel;
    public GameObject clear_Panel;
    public GameObject gameOver_Panel;

    [Header("Button Setting")]
    public Button start_button;


    private void Start()
    {
        difficult_Panel.SetActive(true);
        betting_Panel.SetActive(false);

        shuffleManager = FindObjectOfType<ShuffleManager>();

        // 난이도 버튼 설정
        difficulty_Button[0].onClick.AddListener(() => {
            shuffleManager.SetDifficulty(SHELLDIFFICULTY.Easy);
            ShowBettingUI();
        });

        difficulty_Button[1].onClick.AddListener(() => {
            shuffleManager.SetDifficulty(SHELLDIFFICULTY.Nomal);
            ShowBettingUI();
        });

        difficulty_Button[2].onClick.AddListener(() => {
            shuffleManager.SetDifficulty(SHELLDIFFICULTY.Hard);
            ShowBettingUI();
        });

        // 시작 버튼 설정
        start_button.onClick.AddListener(() => {
            betting_Panel.SetActive(false);  // 배팅 UI 숨기기
            gameInfo_Panel.SetActive(true);//라운드가 적혀있음
            shuffleManager.StartGame();      // 게임 시작
        });
    }

    private void ShowBettingUI()
    {
        difficult_Panel.SetActive(false);
        betting_Panel.SetActive(true);

    }


}
