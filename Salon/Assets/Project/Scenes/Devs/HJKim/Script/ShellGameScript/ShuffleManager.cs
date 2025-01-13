using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class ShuffleManager : MonoBehaviour
{
    //[SerializeField]
    //public GameObject spinner_Prefab;
    [SerializeField]
    private List<Cup> cups = new List<Cup>();
    [SerializeField]
    private float spinDuration = 1f;//회전 속도 
    private GameObject spinner;//빈껍데기 스피너

       
    private int cupCount;


    private SHELLDIFFICULTY shellDifficulty;

    private void Start()
    {//무적권 1번(가운데 컵)이 구슬 가지고있음.
        cups[1].hasBall = true;

        foreach (Cup cup in cups)
        {
            cup.gameObject.SetActive(false);
        }

        //TODO:난이도 설정 버튼 할당해야 함.
        shellDifficulty = SHELLDIFFICULTY.Easy;
        cupCount = (int)SHELLDIFFICULTY.Easy;

        //if (shellDifficulty == SHELLDIFFICULTY.Easy)
        //{
        //    cups[0].gameObject.SetActive(true);
        //    cups[1].gameObject.SetActive(true);
        //    cups[2].gameObject.SetActive(true);
        //}
        //else { }

        for (int i = 0; i < cupCount; i++)
        {
            cups[i].gameObject.SetActive(true);
        }

        CupShuffle();
    }

    private void CupShuffle()
    {//컵 두개뽑기 
        int firstCup = Random.Range(0,cupCount);
        int secondCup = Random.Range(0, cupCount);
        while (firstCup == secondCup)
        {
            firstCup = Random.Range(0, cupCount);
        }

        if (cups[firstCup].hasBall == true)
        {
            print("구슬컵이 포함되어있습니다.");
        }

        //컵 움직이기
        Vector3 spinnerPos = (cups[firstCup].transform.position + cups[secondCup].transform.position) / 2f;
        //프리팹 생성과 회전 초기화

        spinner = new GameObject("Spinner");
        spinner.transform.position = spinnerPos;

        //자식으로 설정
        cups[firstCup].transform.SetParent(spinner.transform);
        cups[secondCup].transform.SetParent(spinner.transform);

        //회전 설정

        //spinner.transform.rotation = Quaternion.Euler(0f, 180f, 0f);


    }
}


public enum SHELLDIFFICULTY
{
    Easy= 3,
    Nomal,
    Hard,
}
