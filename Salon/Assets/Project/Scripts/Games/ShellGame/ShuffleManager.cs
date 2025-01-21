using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static ShellGameDiffi.Difficult;
using Salon.ShellGame;
public class ShuffleManager : MonoBehaviour
{

    [SerializeField]
    private List<Cup> cups = new List<Cup>();
    [Header("Controller")]
    [SerializeField]
    private float spinSpeed = 5;//회전 속도 
    [SerializeField]
    private float shuffleDuration = 5;


    private ShellGameUI uiManager;

    [Header("Anime")]
    //초반 애니메이션용 컵과 구슬
    public GameObject anime_Cup;
    public GameObject anime_Ball;
    private GameObject spinner;//빈껍데기 스피너
    [SerializeField]
    private Transform table_pos;

    private int cupCount;
    private bool isStart = false;
    private bool isCanSelect = false;
    private float cupDis;
    private float animaMoveSpeed = 5f;
    public Transform animeMovePoint;


    private void Start()
    {
        UIManager.Instance.CloseAllPanels();
        UIManager.Instance.OpenPanel(PanelType.ShellGame);
        uiManager = UIManager.Instance.GetComponentInChildren<ShellGameUI>();
        uiManager.Initialize(this);
    }


    private SHELLDIFFICULTY shellDifficulty;


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
            Time.deltaTime * spinSpeed / cupDis);
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
        yield return new WaitForSeconds(shuffleDuration);

        isStart = false;
        isCanSelect = true;
    }

    public void OnCupSelected(Cup cup)
    {
        if (isCanSelect == false)
        {
            return;
        }
        if (cup.hasBall == true)
        {
            print("승리");
        }
        else if (cup.hasBall == false)
        {
            print("패배");
        }
        isCanSelect = false;
    }

    private IEnumerator SetAnime()
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
        anime_Ball.gameObject.SetActive(true);

        StartCoroutine(SetAnime());
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
                animeMovePoint.position + new Vector3(0, 0.3f, 0),//너무 아래로 내려가니까 y축으로 좀 덜내려가도록 조정
                animaMoveSpeed * Time.deltaTime);
            yield return null;
        }
    }

}
