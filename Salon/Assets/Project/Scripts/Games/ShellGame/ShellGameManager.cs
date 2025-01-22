using Firebase.Database;
using Salon.Firebase;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Salon.ShellGame
{
    public class ShellGameManager : MonoBehaviour
    {


        [SerializeField]
        private List<Cup> cups = new List<Cup>();
        [Header("Controller")]
        [SerializeField]
        private float spinSpeed = 5;//회전 속도 
        public Dictionary<SHELLDIFFICULTY, int> maxBetting { get; private set; } = new Dictionary<SHELLDIFFICULTY, int>();
        public Dictionary<SHELLDIFFICULTY, float> shuffleSpeed { get; private set; } = new Dictionary<SHELLDIFFICULTY, float>();
        public Dictionary<SHELLDIFFICULTY, float> plusShuffleSpeed { get; private set; } = new Dictionary<SHELLDIFFICULTY, float>();
        public Dictionary<SHELLDIFFICULTY, float> shuffleDuration { get; private set; } = new Dictionary<SHELLDIFFICULTY, float>();

        private ShellGameUI uiManager;

        private SHELLDIFFICULTY shellDifficulty;

        [Header("Anime")]
        //초반 애니메이션용 컵과 구슬
        public GameObject anime_Cup;
        public GameObject anime_Ball;

        private Vector3 anime_Cup_Pos; 
        private Vector3 anime_Ball_Pos;

        private GameObject spinner;//빈껍데기 스피너
        [SerializeField]
        private Transform table_pos;


        public int round { get; private set; } = 1;
        public int TextRound { get; private set; } = 1;
        private int cupCount;
        private bool isStart = false;
        private bool isCanSelect = false;
        private bool isTurning = false;
        private float cupDis;
        private float animaMoveSpeed = 5f;
        public Transform animeMovePoint;
        [SerializeField]
        private float speed = 0f;
        [SerializeField]
        private float roundSpeed = 0f;
        [SerializeField]
        private float Duration = 0f;

        public string UserUID { get; private set; }

        public DatabaseReference currentUserRef { get; private set; }

        //TODO:서버 소지금 할당
        public int myGold { get; private set; }
        //TODO: 투두
        public int BettingGOld { get; private set; }

        private void Awake()
        {
            maxBetting[SHELLDIFFICULTY.Easy] = 10;
            maxBetting[SHELLDIFFICULTY.Normal] = 50;
            maxBetting[SHELLDIFFICULTY.Hard] = 100;

            shuffleSpeed[SHELLDIFFICULTY.Easy] = 4.5f + speed;
            shuffleSpeed[SHELLDIFFICULTY.Normal] = 6f + speed;
            shuffleSpeed[SHELLDIFFICULTY.Hard] = 6f + speed;

            plusShuffleSpeed[SHELLDIFFICULTY.Easy] = 0.5f + roundSpeed;
            plusShuffleSpeed[SHELLDIFFICULTY.Normal] = 1f + roundSpeed;
            plusShuffleSpeed[SHELLDIFFICULTY.Hard] = 1.5f + roundSpeed;

            shuffleDuration[SHELLDIFFICULTY.Easy] = 5f + Duration;
            shuffleDuration[SHELLDIFFICULTY.Normal] = 7f + Duration;
            shuffleDuration[SHELLDIFFICULTY.Hard] = 10f + Duration;

            anime_Cup_Pos = anime_Cup.transform.position;
            anime_Ball_Pos = anime_Ball.transform.position;
        }
        private async void Start()
        {
            UIManager.Instance.CloseAllPanels();
            UIManager.Instance.OpenPanel(PanelType.ShellGame);
            uiManager = UIManager.Instance.GetComponentInChildren<ShellGameUI>();

            uiManager.Initialize(this);

            uiManager.gameInfo_Panel.round_Text.text = $"ROUND {TextRound}";
            uiManager.clear_Panel.clearRound_Text.text = $"ROUND{TextRound}";

            uiManager.clear_Panel.go_Button.onClick.AddListener(() =>
            {
                NextRound();
                uiManager.clear_Panel.gameObject.SetActive(false);
            });

            UserUID = FirebaseManager.Instance.CurrentUserUID;


            currentUserRef = FirebaseManager.Instance.DbReference.Child("Users").Child(UserUID).Child("Gold");

            try
            {
                var snapshot = await currentUserRef.GetValueAsync();
                if (snapshot.Exists)
                {
                    myGold = int.Parse(snapshot.Value.ToString());
                    print(myGold);
                }
                else
                {
                    Debug.Log("점수 데이터가 없습니다");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"점수 가져오기 실패: {e.Message}");
            }
       
        }

        private void Update()
        {
            if (spinner != null)
            {
                isTurning = true;
                CupShuffle();
            }
            else if (spinner == null && isStart == true)
            {
                SpawnSpinner();
            }
        }
        private void SpawnSpinner()
        {//컵 두개뽑기 
            int firstCup = Random.Range(0, cupCount);
            int secondCup = Random.Range(0, cupCount);
            while (firstCup == secondCup)
            {
                firstCup = Random.Range(0, cupCount);
            }
            //컵 움직이기
            Vector3 spinnerPos = (cups[firstCup].transform.position + cups[secondCup].transform.position) / 2f;
            cupDis = Vector3.Distance(cups[firstCup].transform.position, cups[secondCup].transform.position);
            cupDis = Mathf.Min(cupDis, 5f);
            //스피너 가운데에 생성
            spinner = new GameObject("Spinner");
            spinner.transform.position = spinnerPos;
            //자식으로 설정
            cups[firstCup].transform.SetParent(spinner.transform);
            cups[secondCup].transform.SetParent(spinner.transform);
        }
        private void CupShuffle()
        {
            spinner.transform.rotation = Quaternion.Lerp
                (spinner.transform.rotation,
                Quaternion.Euler(0f, 180f, 0f),
                Time.deltaTime * (shuffleSpeed[shellDifficulty] + (round * plusShuffleSpeed[shellDifficulty])) / cupDis);
            print(shuffleSpeed[shellDifficulty] + (round * plusShuffleSpeed[shellDifficulty]));
            if (Quaternion.Angle(spinner.transform.rotation,
                Quaternion.Euler(0f, -180f, 0f)) < 0.05f)
            {
                while (spinner.transform.childCount > 0)
                {
                    spinner.transform.GetChild(0).SetParent(table_pos);
                }

                Destroy(spinner);
                isTurning = false;
            }
        }

        private IEnumerator ShuffleStart()
        {
            yield return new WaitForSeconds(shuffleDuration[shellDifficulty]);

            isStart = false;

            yield return new WaitUntil(() => isTurning == false);

            isCanSelect = true;

            float time = 5f;
            while (time >= 0 && isCanSelect == true)
            {
                time -= Time.deltaTime;
                uiManager.gameInfo_Panel.timer_Text.text = ((int)time).ToString();
                yield return null;
            }
            if (time <= 0)
            {
                //TODO: 시간초과 패배
                print("현J님 개같이 패배");
                isCanSelect = false;
                uiManager.gameOver_Panel.gameObject.SetActive(true);

            }


        }

        public void OnCupSelected(Cup cup)
        {
            if (isCanSelect == false)
            {
                return;
            }
            isCanSelect = false;

            StartCoroutine(CheckCup(cup));

        }

        public void SetCup()
        {

            foreach (Cup cup in cups)
            {
                cup.Initialize(this);
                cup.gameObject.SetActive(false);
            }

            //난이도에 따른만큼 컵 켜기
            for (int i = 0; i < cupCount; i++)
            {
                cups[i].gameObject.SetActive(true);
            }
            cups[1].hasBall = true;//구슬이 있는컵은 3번째컵(중앙)

        }
        private IEnumerator setAnime()
        {
            print("멈춰! 컵내려오는중~~~~");
            yield return new WaitForSeconds(1.5f);
            print("움직여! 컵 다내려옴~~");
            StartCoroutine(uiManager.PlayCount());
            yield return new WaitForSeconds(4.5f);
            print("이제 컵 다내려왔으니까 게임 시작할게~~~~");
            cups[1].gameObject.SetActive(true);
            isStart = true;
            anime_Ball.gameObject.SetActive(false);
            anime_Cup.gameObject.SetActive(false);
            StartCoroutine(ShuffleStart());
            yield return new WaitForSeconds(5f);
        }
        public void StartGame()//여기가 게임 시작하는 초입 애니 들어가야함
        {
            anime_Cup.gameObject.SetActive(true);
            anime_Cup.transform.position = anime_Cup_Pos;
            anime_Ball.gameObject.SetActive(true);
            anime_Ball.transform.position = anime_Ball_Pos;

            StartCoroutine(setAnime());
            StartCoroutine(StartAnime());
        }



        public void SetDifficulty(SHELLDIFFICULTY difficulty)
        {
            shellDifficulty = difficulty;
            cupCount = (int)shellDifficulty;
        }

        private IEnumerator StartAnime() // 구슬이랑 컵이 내려가는 초반 애니메이션 


        {//연출을 위해서 일단은 가운데꺼 꺼놓기
            cups[1].gameObject.SetActive(false);
            anime_Cup.gameObject.SetActive(true);
            anime_Ball.gameObject.SetActive(true);
            while (Vector3.Distance(anime_Ball.transform.position,
                cups[1].transform.position + new Vector3(0, -0.3f, 0)) > 0.01f)
            {
                anime_Ball.transform.position = Vector3.Lerp(
                    anime_Ball.transform.position,
                    cups[1].transform.position + new Vector3(0, -0.3f, 0),
                    animaMoveSpeed * Time.deltaTime);
                yield return null;
            }
            while (Vector3.Distance(anime_Cup.transform.position,
                cups[1].transform.position) > 0.01f)
            {
                anime_Cup.transform.position = Vector3.Lerp(
                    anime_Cup.transform.position,
                    cups[1].transform.position,//너무 아래로 내려가니까 y축으로 좀 덜내려가도록 조정
                    animaMoveSpeed * Time.deltaTime);
                yield return null;
            }
        }

        private IEnumerator CheckCup(Cup cup)
        {
            if (cup.hasBall == true)
            {
                anime_Ball.gameObject.SetActive(true);
                Vector3 ballPos = cup.transform.position;
                ballPos.y -= 0.5f;
                anime_Ball.transform.position = ballPos;
            }
            Vector3 checkBall = cup.transform.position;
            checkBall.y += 2f;

            while (Vector3.Distance(cup.transform.position, checkBall) > 0.01f)
            {
                cup.transform.position = Vector3.Lerp(
                   cup.transform.position,
                   checkBall,
                  5.5f * Time.deltaTime);

                yield return null;

            }
            yield return new WaitForSeconds(0.5f);//컵올라오는거 대기

            if (cup.hasBall == true)
            {
                uiManager.clear_Panel.gameObject.SetActive(true);
                uiManager.gameOver_Panel.gameObject.SetActive(false);
            }
            else if (cup.hasBall == false)
            {
                uiManager.gameOver_Panel.gameObject.SetActive(true);
                uiManager.clear_Panel.gameObject.SetActive(false);
            }
            isCanSelect = false;
            checkBall.y -= 2f;
            while (Vector3.Distance(cup.transform.position,checkBall) > 0.01f)
            {
                cup.transform.position = Vector3.Lerp(
                  cup.transform.position,
                   checkBall,
                  5.5f * Time.deltaTime);

                yield return null;
            }

        }

        public void NextRound()
        {
            TextRound++;
            round++;
            if (round > 10)
            {
                round = 10;
            }
            isStart = false;
            isCanSelect = false;
                    
            uiManager.gameInfo_Panel.round_Text.text = $"ROUND {TextRound}";
            uiManager.clear_Panel.clearRound_Text.text = $"ROUND{TextRound} Clear";
            uiManager.gameInfo_Panel.timer_Text.text = "5";


            StartGame();
        }

        private async void MyGoldLoad()
        {

        }

    }
}