using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Salon.ShellGame
{
    public class ShellGameManager : MonoBehaviour
    {


        [SerializeField]
        private List<Cup> cups = new List<Cup>();
        [Header("Controller")]
        [SerializeField]
        private float spinSpeed = 5;//ȸ�� �ӵ� 
        public Dictionary<SHELLDIFFICULTY, int> maxBetting { get; private set; } = new Dictionary<SHELLDIFFICULTY, int>();
        public Dictionary<SHELLDIFFICULTY, float> shuffleSpeed { get; private set; } = new Dictionary<SHELLDIFFICULTY, float>();
        public Dictionary<SHELLDIFFICULTY, float> plusShuffleSpeed { get; private set; } = new Dictionary<SHELLDIFFICULTY, float>();
        public Dictionary<SHELLDIFFICULTY, float> shuffleDuration { get; private set; } = new Dictionary<SHELLDIFFICULTY, float>();

        private ShellGameUI uiManager;

        private SHELLDIFFICULTY shellDifficulty;

        [Header("Anime")]
        //�ʹ� �ִϸ��̼ǿ� �Ű� ����
        public GameObject anime_Cup;
        public GameObject anime_Ball;
        private GameObject spinner;//�󲮵��� ���ǳ�
        [SerializeField]
        private Transform table_pos;


        public int round { get; private set; } = 1;
        private int cupCount;
        private bool isStart = false;
        private bool isCanSelect = false;
        private float cupDis;
        private float animaMoveSpeed = 5f;
        public Transform animeMovePoint;
        [SerializeField]
        private float speed = 0f;
        [SerializeField]
        private float roundSpeed = 0f;
        //TODO:���� ������ �Ҵ�
        [SerializeField]
        private float Duration = 0f;
        public int myGold { get; private set; }

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

            shuffleDuration[SHELLDIFFICULTY.Easy] = 5f +Duration;
            shuffleDuration[SHELLDIFFICULTY.Normal] = 7f + Duration;
            shuffleDuration[SHELLDIFFICULTY.Hard] = 10f + Duration;

        }
        private void Start()
        {
            UIManager.Instance.CloseAllPanels();
            UIManager.Instance.OpenPanel(PanelType.ShellGame);
            uiManager = UIManager.Instance.GetComponentInChildren<ShellGameUI>();

            uiManager.Initialize(this);

            uiManager.gameInfo_Panel.round_Text.text = $"ROUND {round}";
            uiManager.clear_Panel.clearRound_Text.text = $"ROUND{round}";
        }

        private void Update()
        {
            if (spinner != null)
            {
                CupShuffle();
            }
            else if (spinner == null && isStart == true)
            {
                SpawnSpinner();
            }
        }
        private void SpawnSpinner()
        {//�� �ΰ��̱� 
            int firstCup = Random.Range(0, cupCount);
            int secondCup = Random.Range(0, cupCount);
            while (firstCup == secondCup)
            {
                firstCup = Random.Range(0, cupCount);
            }
            //�� �����̱�
            Vector3 spinnerPos = (cups[firstCup].transform.position + cups[secondCup].transform.position) / 2f;
            cupDis = Vector3.Distance(cups[firstCup].transform.position, cups[secondCup].transform.position);
            cupDis = Mathf.Min(cupDis, 5f);
            //���ǳ� ����� ����
            spinner = new GameObject("Spinner");
            spinner.transform.position = spinnerPos;
            //�ڽ����� ����
            cups[firstCup].transform.SetParent(spinner.transform);
            cups[secondCup].transform.SetParent(spinner.transform);
        }
        private void CupShuffle()
        {
            spinner.transform.rotation = Quaternion.Lerp
                (spinner.transform.rotation,
                Quaternion.Euler(0f, 180f, 0f),
                Time.deltaTime * (shuffleSpeed[shellDifficulty] + (round * plusShuffleSpeed[shellDifficulty])) / cupDis);
            if (Quaternion.Angle(spinner.transform.rotation,
                Quaternion.Euler(0f, -180f, 0f)) < 0.05f)
            {
                while (spinner.transform.childCount > 0)
                {
                    spinner.transform.GetChild(0).SetParent(table_pos);
                }

                Destroy(spinner);
            }
        }

        private IEnumerator ShuffleStart()
        {
            yield return new WaitForSeconds(shuffleDuration[shellDifficulty]);

            isStart = false;
            isCanSelect = true;
            float time=5f;
            while (time>=0 && isCanSelect==true )
            {
                time -= Time.deltaTime;
                uiManager.gameInfo_Panel.timer_Text.text = ((int)time).ToString();
                yield return null;
            }
            if(time <= 0)
            {
                //TODO: �ð��ʰ� �й�
                print("Wls�� ������ �й�");
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

            StartCoroutine(CheckCup(cup));

            //if (cup.hasBall == true)
            //{
            //    uiManager.clear_Panel.gameObject.SetActive(true);
            //}
            //else if (cup.hasBall == false)
            //{
            //    uiManager.gameOver_Panel.gameObject.SetActive(true);
            //}
            //isCanSelect = false;
        }

        private IEnumerator SetAnime()
        {

            foreach (Cup cup in cups)
            {
                cup.Initialize(this);
                cup.gameObject.SetActive(false);
            }

            //���̵��� ������ŭ �� �ѱ�
            for (int i = 0; i < cupCount; i++)
            {
                cups[i].gameObject.SetActive(true);
            }
            cups[1].hasBall = true;//������ �ִ����� 3��°��(�߾�)

            print("����! �ų���������~~~~");
            yield return new WaitForSeconds(1.5f);
            print("������! �� �ٳ�����~~");
            StartCoroutine(uiManager.PlayCount());
            yield return new WaitForSeconds(4.5f);
            print("���� �� �ٳ��������ϱ� ���� �����Ұ�~~~~");
            cups[1].gameObject.SetActive(true);
            isStart = true;
            anime_Ball.gameObject.SetActive(false);
            anime_Cup.gameObject.SetActive(false);
            StartCoroutine(ShuffleStart());
            yield return new WaitForSeconds(5f);
        }

        public void StartGame()//���Ⱑ ���� �����ϴ� ���� �ִ� ������
        {
            anime_Cup.gameObject.SetActive(true);
            anime_Ball.gameObject.SetActive(true);

            StartCoroutine(SetAnime());
            StartCoroutine(StartAnime());
        }



        public void SetDifficulty(SHELLDIFFICULTY difficulty)
        {
            shellDifficulty = difficulty;
            cupCount = (int)shellDifficulty;
        }

        private IEnumerator StartAnime() // �����̶� ���� �������� �ʹ� �ִϸ��̼� 


        {//������ ���ؼ� �ϴ��� ����� ������
            cups[1].gameObject.SetActive(false);
            anime_Cup.gameObject.SetActive(true);
            anime_Ball.gameObject.SetActive(true);
            while (Vector3.Distance(anime_Ball.transform.position, animeMovePoint.position) > 0.01f)
            {
                anime_Ball.transform.position = Vector3.Lerp(
                    anime_Ball.transform.position,
                    animeMovePoint.position,
                    animaMoveSpeed * Time.deltaTime);
                yield return null;
            }
            while (Vector3.Distance(anime_Cup.transform.position,
                animeMovePoint.position + new Vector3(0, 0.3f, 0)) > 0.01f)
            {
                anime_Cup.transform.position = Vector3.Lerp(
                    anime_Cup.transform.position,
                    animeMovePoint.position + new Vector3(0, 0.3f, 0),//�ʹ� �Ʒ��� �������ϱ� y������ �� ������������ ����
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
            yield return new WaitForSeconds(0.5f);//�ſö���°� ���

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
        }
    }
}