using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ShuffleManager : MonoBehaviour
{
    [SerializeField]
    private List<Cup> cups = new List<Cup>();

    private SHELLDIFFICULTY shellDifficulty;

    private int cupCount;
    private void Start()
    {
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
    {
        int a = Random.Range(0,cupCount);
        int b = Random.Range(0, cupCount);
        while (a == b)
        {
            a = Random.Range(0, cupCount);
        }

        if (cups[a].hasBall == true)
        {
            print("어 여기있어");
        }


        cups[a].gameObject.SetActive(false);
        cups[b].gameObject.SetActive(false);
    }
}

public enum SHELLDIFFICULTY
{
    Easy= 3,
    Nomal,
    Hard,
}
