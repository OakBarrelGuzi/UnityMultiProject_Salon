using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShuffleManager : MonoBehaviour
{
    //[SerializeField]
    //public GameObject spinner_Prefab;
    [SerializeField]
    private List<Cup> cups = new List<Cup>();
    [SerializeField]
    private float spinSpeed =5f;//회전 속도 
    [SerializeField]
    private float shuffleDuration = 5;
     
    private GameObject spinner;//빈껍데기 스피너
    [SerializeField]
    private Transform table_pos;

    private int cupCount;
    private bool isStart = false;
    private bool isCanSelect = false;


    private SHELLDIFFICULTY shellDifficulty;


    private void Update()
    {
        if (spinner != null)
        {
            CupShuffle();
        }
      else if (spinner ==null&& isStart==true)
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

        //if (cups[firstCup].hasBall == true)
        //{
        //    print("구슬컵이 포함되어있습니다.");
        //}

        //컵 움직이기
        Vector3 spinnerPos = (cups[firstCup].transform.position + cups[secondCup].transform.position) / 2f;

        //스피너 가운데에 생성
        spinner = new GameObject("Spinner");
        spinner.transform.position = spinnerPos;

        //자식으로 설정
        cups[firstCup].transform.SetParent(spinner.transform);
        cups[secondCup].transform.SetParent(spinner.transform);

        //회전 설정

        //spinner.transform.rotation = Quaternion.Euler(0f, 180f, 0f);


    }
    private void CupShuffle()
    {
        spinner.transform.rotation = Quaternion.Lerp
            (spinner.transform.rotation,
            Quaternion.Euler(0f, 180f, 0f),
            Time.deltaTime * spinSpeed);
        if (Quaternion.Angle(spinner.transform.rotation,
            Quaternion.Euler(0f, -180f, 0f))<0.05f){
            while (spinner.transform.childCount > 0)
            {
                spinner.transform.GetChild(0).SetParent(table_pos);
            }

            Destroy(spinner);
        }
    }

    private IEnumerator ShuffleStart()
    {
        yield return new WaitForSeconds(10);

        isStart=false;
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


    public void StartGame()
    {
        isStart = true;
        cups[1].hasBall = true;


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
        StartCoroutine(ShuffleStart());
    }

    public void SetDifficulty(SHELLDIFFICULTY difficulty)
    {
        shellDifficulty = difficulty;
        cupCount = (int)shellDifficulty;
    }
}


public enum SHELLDIFFICULTY
{
    Easy = 3,
    Nomal,
    Hard,
}