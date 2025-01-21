using Salon.Firebase;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Salon.DartGame
{
    public class DartGame : MonoBehaviour
    {
        private const float TURNTIME = 10f;
        private const float BREATHTIME = 3f;
        private const int MAXROUND = 5;
        private const int MAXTURN = 3;

        private int round = 1;
        private int turn = 1;
        private int totalScore = 0;

        private float turnTime = TURNTIME;
        private float breathTime = BREATHTIME;

        private bool isWaitInput = false;
        private bool isWaitBreath = false;
        private bool isWaitShootButton = false;

        private bool isRandomMoving = false;

        private bool isMove = true;
        private bool isMoving = false;

        private bool ranPosArrival = false;

        private Vector3 randomTargetPosition;

        private List<TextMeshProUGUI> scoreTextList = new List<TextMeshProUGUI>();

        private List<Arrow> darts = new List<Arrow>();

        private int[] roundScore = new int[MAXROUND];

        [SerializeField, Header("조이스틱 조준원 스피드")]
        private float joystickMoveSpeed = 1f;
        [SerializeField, Header("랜덤 조준원 스피드")]
        private float randomMoveSpeed = 0.9f;
        [SerializeField, Header("각 턴사이 딜레이시간")]
        private float turnWait = 2f;
        [SerializeField, Header("에임이 줄어드는 시간")]
        private float aimingDelay = 0.8f;
        [SerializeField, Header("에임이 줄어드는 틱 딜레이")]
        private float aimingRoutineDelay = 0.03f;
        [SerializeField, Header("에임이 틱당 줄어드는 비율")]
        private float aimingShrinkScale = 0.96f;

        private DartGameUI gameUi;

        [SerializeField, Header("")]
        private Dart dart;

        [SerializeField]
        private AimingRing aimingRingPrefab;

        private AimingRing targetAiming;

        [SerializeField]
        private Arrow arrowPrefab;

        private JoyStick joyStick;

        private bool isGameStarted;

        private void Start()
        {
            StartCoroutine(InitializeRoutine());
        }

        public IEnumerator InitializeRoutine()
        {
            yield return null;
            UIManager.Instance.CloseAllPanels();
            UIManager.Instance.OpenPanel(PanelType.Dart);
            DartGameUI panel = UIManager.Instance.GetComponentInChildren<DartGameUI>();
            panel.gameStartReady.gameObject.SetActive(true);
            panel.gameStartReady.gameStartTostText.text = "GAME START";
            yield return new WaitUntil(() => panel.gameStartReady.Gamestart == true);
            StartGame();
        }

        private void StartGame()
        {
            gameUi = UIManager.Instance.GetComponentInChildren<DartGameUI>();
            if (gameUi != null)
            {
                joyStick = gameUi.joyStick;
            }
            if (scoreTextList.Any())
            {
                foreach (TextMeshProUGUI text in scoreTextList)
                {
                    if (text != null)
                    {
                        Destroy(text.gameObject);
                    }
                }
                scoreTextList.Clear();
            }
            for (int i = 0; i < MAXROUND; i++)
            {
                TextMeshProUGUI scoretext = Instantiate(gameUi.scoreTextPrefab, gameUi.scoreTextField);
                scoreTextList.Add(scoretext);
                scoretext.text = $"R{i + 1}";
            }
            round = 1;
            turn = 1;
            totalScore = 0;
            dart.roundscores = 0;
            gameUi.recentScoreText.text = " ";
            NextTurnset();
            StartCoroutine(GameRoutine());
            isGameStarted = true;
        }

        public void NextTurnset()
        {
            turnTime = TURNTIME;
            breathTime = BREATHTIME;

            isWaitInput = false;
            isWaitBreath = false;
            isWaitShootButton = false;

            isRandomMoving = false;

            isMove = true;
            isMoving = false;
            ranPosArrival = false;

        }

        private void Update()
        {
            if (isGameStarted)
            {
                AimingMove();
                RandomMove();

                DartGameTimeDecay();

                DartGameUiUpdate();
            }
        }
        private void DartGameUiUpdate()
        {
            gameUi.turnTimeSlider.value = turnTime / TURNTIME;
            gameUi.turnTimeText.text = $"{(int)turnTime} / {TURNTIME}";
            gameUi.breathTimeSlider.value = breathTime / BREATHTIME;

            gameUi.totalScoreText.text = $"Total Score : {totalScore}";
        }

        private void DartGameTimeDecay()
        {
            if (isMoving == false && isWaitBreath == false) turnTime -= Time.deltaTime;
            else if (isWaitBreath == false) { breathTime -= Time.deltaTime; }

            if (breathTime <= 0f && isWaitBreath == false)
            {
                ShootButtonClick();
            }
            if (turnTime <= 0f && isWaitBreath == false)
            {
                ShootButtonClick();
            }
        }

        public void AimingMove()
        {
            if (isMove == false) return;
            if (targetAiming == null) return;

            Vector3 moveDirection = GetJoystickDirection();
            if (moveDirection != Vector3.zero)
            {
                MoveTarget(moveDirection, joystickMoveSpeed);
            }
        }

        private void RandomMove()
        {
            if (isRandomMoving == false) return;
            if (targetAiming == null) return;

            if (0.01f > Vector3.Distance(randomTargetPosition, targetAiming.transform.position))
            {
                ranPosArrival = true;
                return;
            }

            Vector3 moveDirection = (randomTargetPosition - targetAiming.transform.position).normalized;
            MoveTarget(moveDirection, randomMoveSpeed);
        }

        private Vector3 GetJoystickDirection()
        {
            Vector2 direction = joyStick.GetDirection();
            float magnitude = joyStick.GetMagnitude();

            if (magnitude < 1f)
            {
                isMoving = false;
                return Vector3.zero;
            }
            if (isWaitInput == false) isWaitInput = true;
            isMoving = true;

            return new Vector3(direction.x, direction.y, 0);
        }

        private void MoveTarget(Vector3 direction, float speed)
        {
            Vector3 newPosition = targetAiming.transform.position + direction * Time.deltaTime * speed;
            newPosition = ClampPositionToDartboard(newPosition);
            targetAiming.transform.position = newPosition;
        }

        private Vector3 ClampPositionToDartboard(Vector3 position)
        {
            float distanceFromCenter = Vector3.Distance(position, dart.center.position);
            float maxAllowedDistance = Vector3.Distance(dart.center.position, dart.outerLine.position)
                - Vector3.Distance(targetAiming.transform.position, targetAiming.outerLine.position);

            if (distanceFromCenter > maxAllowedDistance)
            {
                Vector3 directionFromCenter = (position - dart.center.position).normalized;
                position = dart.center.position + directionFromCenter * maxAllowedDistance;
            }

            return new Vector3(position.x, position.y, targetAiming.transform.position.z);
        }

        public void SpawnAimingRing()
        {
            AimingRing aiming = Instantiate(aimingRingPrefab);
            targetAiming = aiming;

            float dartDistance = Vector3.Magnitude(dart.center.position - dart.outerLine.position);
            float aminingDistance = Vector3.Magnitude(aiming.transform.position - aiming.outerLine.position);

            //print(dartDistance);
            //print(aminingDistance);

            float maxSpawnDistance = dartDistance - aminingDistance;

            //print(maxSpawnDistance);
            Vector2 ranPos = Random.insideUnitCircle;

            ranPos = ranPos * maxSpawnDistance;

            Vector3 spawnPosition = dart.center.position + new Vector3(ranPos.x, ranPos.y, -0.01f);

            aiming.transform.position = spawnPosition;

        }

        public void SpawnArrow()
        {
            float distance = Vector3.Distance(targetAiming.transform.position, targetAiming.innerLine.transform.position);

            Vector2 ran = Random.insideUnitCircle * distance;

            Vector3 arrowPoint = targetAiming.transform.position + new Vector3(ran.x, ran.y, 0);

            arrowPoint.z = dart.transform.position.z - 0.02f;

            Arrow arrow = Instantiate(arrowPrefab, dart.shootPoint.transform.position, Quaternion.identity);
            darts.Add(arrow);
            arrow.SetTargetPosition(arrowPoint);
        }

        private void SetRandomTargetPosition()
        {
            float dartDistance = Vector3.Distance(dart.center.position, dart.outerLine.position);
            float aimingDistance = Vector3.Distance(targetAiming.transform.position, targetAiming.outerLine.position);
            float maxDistance = dartDistance - aimingDistance;

            maxDistance = maxDistance * (1f - turnTime / 20f);

            Vector2 randomDirection = Random.insideUnitCircle;
            Vector3 randomPosition = dart.center.position +
                                   new Vector3(randomDirection.x, randomDirection.y, -0.01f) * maxDistance;

            randomPosition.z = targetAiming.transform.position.z;

            randomTargetPosition = randomPosition;
        }

        public void ShootButtonClick()
        {
            print("ShootButtonȣ��");
            isRandomMoving = false;
            isMove = false;
            isWaitInput = true;
            isWaitBreath = true;
            isMoving = false;

            gameUi.shootButton.onClick?.RemoveListener(ShootButtonClick);
            if (targetAiming != null)
            {
                StartCoroutine(reduceAimingRoutine());
            }
        }

        public void ShootDartArrow()
        {
            print("ShootDartArrowȣ��");
            isWaitShootButton = true;
            gameUi.shootButton.onClick?.RemoveListener(ShootDartArrow);
            SpawnArrow();

            Destroy(targetAiming.gameObject);
            targetAiming = null;
        }

        public void AddScores()
        {

            int dartScore = dart.roundscores;
            roundScore[round - 1] += dartScore;
            scoreTextList[round - 1].text = $"R{round}    {roundScore[round - 1]}";

            totalScore += dartScore;
            gameUi.recentScoreText.text = $"+ {dartScore}";
            dart.roundscores = 0;

            RoundManagement();
        }

        //������ �� ���� ���� ó�� �� ���� ����Ŭ ������ ���� �ʱ�ȭ 
        public void RoundManagement()
        {
            turn++;
            if (turn > MAXTURN)
            {
                turn = 1;
                round++;
                foreach (Arrow dart in darts)
                {
                    if (dart != null)
                    {
                        Destroy(dart.gameObject);
                    }
                }
                darts.Clear();
                if (round > MAXROUND)
                {

                    GameEnd();
                    return;
                }
                StartCoroutine(RoundSet());
                return;
            }
            StartCoroutine(Gameing());
        }

        private IEnumerator RoundSet()
        {
            gameUi.gameStartReady.gameObject.SetActive(true);
            gameUi.gameStartReady.gameStartTostText.text = $"{round}nd ROUND";
            yield return new WaitUntil(() => gameUi.gameStartReady.Gamestart == true);
            NextTurnset();
            StartCoroutine(GameRoutine());
        }

        private IEnumerator Gameing()
        {
            yield return new WaitForSeconds(turnWait);
            NextTurnset();
            StartCoroutine(GameRoutine());
        }

        //TODO: �߰��߰� �̺�Ʈ�� ��ǵ� �ֱ�
        private IEnumerator GameRoutine()
        {
            SpawnAimingRing();
            StartCoroutine(RingRandomMoveRoutine());
            gameUi.shootButton.onClick.AddListener(ShootButtonClick);
            yield return new WaitUntil(() => isWaitInput == true);

            yield return new WaitUntil(() => isWaitBreath == true);
            gameUi.shootButton.onClick?.RemoveListener(ShootButtonClick);
            gameUi.shootButton.onClick.AddListener(ShootDartArrow);

            yield return new WaitUntil(() => isWaitShootButton == true);
            gameUi.shootButton.onClick?.RemoveListener(ShootDartArrow);

            yield return new WaitForSeconds(arrowPrefab.duration + 0.5f);
            AddScores();
        }

        private IEnumerator reduceAimingRoutine()
        {
            float time = 0f;
            while (!isWaitShootButton && time <= aimingDelay)
            {
                targetAiming.transform.localScale = targetAiming.transform.localScale * aimingShrinkScale;
                yield return new WaitForSeconds(aimingRoutineDelay);
                time += aimingRoutineDelay;
            }
            if (!isWaitShootButton && time >= aimingDelay)
            {
                gameUi.shootButton.onClick?.RemoveListener(ShootDartArrow);
                targetAiming.transform.localScale = targetAiming.startScale;
                yield return new WaitForSeconds(0.2f);
                if (targetAiming != null) { ShootDartArrow(); }
            }
        }

        //TODO: �����ϸ� ��ġ �ٲ�� ����
        private IEnumerator RingRandomMoveRoutine()
        {
            while (!isWaitBreath)
            {
                isRandomMoving = !isWaitBreath;
                if (isRandomMoving)
                {
                    SetRandomTargetPosition();

                    yield return new WaitUntil(() => ranPosArrival == true);
                    ranPosArrival = false;
                }
                yield return null;
            }
            isRandomMoving = false;
        }

        private async void GameEnd()
        {
            int myTopSocre = 0;

            var UserUID = FirebaseManager.Instance.CurrentUserUID;

            var currentUserRef = FirebaseManager.Instance.DbReference.Child("Users").Child(UserUID).Child("bestDartScore");

            try
            {
                var snapshot = await currentUserRef.GetValueAsync();
                if (snapshot.Exists)
                {
                    myTopSocre = int.Parse(snapshot.Value.ToString());
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

            if (myTopSocre < totalScore)
            {
                await currentUserRef.SetValueAsync(totalScore);
                gameUi.dartResultPanel.gameObject.SetActive(true);
                gameUi.dartResultPanel.Textset(totalScore);
            }
            else
            {
                ScenesManager.Instance.ChanageScene("LobbyScene");
            }

        }
    }
}