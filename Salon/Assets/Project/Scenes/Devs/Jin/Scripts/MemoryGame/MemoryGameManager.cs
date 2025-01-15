using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using UnityEngine;

public class MemoryGameManager : MonoBehaviour
{
    private const int CARDCOUNT = 14;

    public bool isCardFull { get; private set; } = false;

    [SerializeField]
    private Transform[] cardSpawnPos;

    private List<Card> tableCardList = new List<Card>();

    public Sprite[] cardsprite;

    [SerializeField]
    private Card cardPrefab;

    private int cardnum = 0;

    private List<Card> openCardList = new List<Card>();

    private void Start()
    {
        CardRandomSet();
    }

    private void CardRandomSet()
    {
        HashSet<int> randomCardSet = new HashSet<int>();
        while (randomCardSet.Count < CARDCOUNT)
        {
            randomCardSet.Add(Random.Range(0, cardSpawnPos.Length));
        }

        foreach (int i in randomCardSet)
        {
            Card card = Instantiate(cardPrefab, cardSpawnPos[i]);
            
            card.cardData = new InGameCard
            {
                 cardType = (CARDTYPE)cardnum,
                 cardSprite = cardsprite[cardnum],
            };
            card.Initialize(this);
            tableCardList.Add(card);
            cardnum++;
            if (cardnum > 6 ) cardnum = 0;
        }
    }

    public void CardOpen(Card card)
    {
        openCardList.Add(card);
        StartCoroutine(TurnRoutine(card));
        if(openCardList.Count >= 2)
        {
            isCardFull = true;
            StartCoroutine(CardCheckRoutine());
        }
 
    }
    private IEnumerator CardCheckRoutine()
    {
        yield return new WaitUntil(() => !openCardList[1].isTurning);

        yield return new WaitForSeconds(0.5f);

        //뽑은 2개의 카드가 같을경우
        //TODO:상대턴 내턴 만들어야함
        if (openCardList[0].cardData.cardType ==
              openCardList[1].cardData.cardType)
        {
            //TODO: 점수 만들어야함         
            print("카드가 같음!");
            openCardList.Clear();
            isCardFull = false;
        }
        //뽑은 2개의 카드가 다를경우
        else
        {
            print("카드가 다름");
            StartCoroutine(FailRoutine());            
        }
    }
    public IEnumerator FailRoutine()
    {
        yield return StartCoroutine(TurnRoutine(openCardList[1]));

        yield return StartCoroutine(TurnRoutine(openCardList[0]));

        openCardList.Clear();
        isCardFull = false;
    }

    public IEnumerator TurnRoutine(Card card)
    {
        float elapsedTime = 0f;
        card.cardOpen = !card.cardOpen;
        
        card.isTurning = true;

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime;

            card.transform.localRotation =
                Quaternion.Slerp(card.startQuaternion,
                card.targetQuaternion,
                elapsedTime *card.turnSpeed);
            yield return null;
        }
        Quaternion temp = card.startQuaternion;
        card.startQuaternion = card.targetQuaternion;
        card.targetQuaternion = temp;

        card.isTurning = false;
    }

}
