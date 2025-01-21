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
    private float spinSpeed = 5;//ȸ�� �ӵ� 
    [SerializeField]
    private float shuffleDuration = 5;


    private ShellGameUI uiManager;

    [Header("Anime")]
    //�ʹ� �ִϸ��̼ǿ� �Ű� ����
    public GameObject anime_Cup;
    public GameObject anime_Ball;
    private GameObject spinner;//�󲮵��� ���ǳ�
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
            print("�¸�");
        }
        else if (cup.hasBall == false)
        {
            print("�й�");
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

}
