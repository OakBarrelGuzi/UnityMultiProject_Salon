using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace Salon.DartGame
{
    public class Dart : MonoBehaviour
    {
        public int roundscores { get; set; } = 0;

        [Header("다트판 중앙")]
        public Transform center;

        [Header("오브젝트 아웃라인")]
        public Transform outerLine;

        [SerializeField, Header("점수 배율계산 위치")]
        private ScroeSetPoint scroeMultiplyPoint = new ScroeSetPoint();

        public Transform shootPoint;

        //중앙에서 부터 충돌위치 각도 힘 계산함수
        public void SetShootPoint(Vector3 Point)
        {
            float arrowDistance = Vector3.Magnitude(center.position - Point);

            float arrowAngle = Mathf.Atan2(Point.y - center.position.y, Point.x - center.position.x) * Mathf.Rad2Deg;

            //print(Point);
            //print(arrowDistance);
            //print(arrowAngle);

            int scores = GetScoreFromAngle(arrowAngle);

            scores = GetScoreMultiplier(arrowDistance, scores);

            print(scores);

            roundscores += scores;
        }

        //각도 기준으로 점수계산
        public int GetScoreFromAngle(float arrowAngle)
        {
            int[] scores = new int[]
            {
            6, 13, 4, 18, 1, 20, 5, 12, 9, 14,
            11, 8, 16, 7, 19, 3, 17, 2, 15, 10
            };

            arrowAngle += 9f;
            if (arrowAngle < 0)
            {
                arrowAngle += 360f;
            }

            int section = Mathf.FloorToInt(arrowAngle / 18f);
            return scores[section];
        }

        //중앙에서 거리기준으로 추가점수 계산
        public int GetScoreMultiplier(float arrowDistance, int scores)
        {
            print($"다트 : {arrowDistance}");
            if (arrowDistance < GetScoreDistance(scroeMultiplyPoint.FiftyPoint))
            {
                return 50;
            }
            if (arrowDistance < GetScoreDistance(scroeMultiplyPoint.TwentyfivePoint))
            {
                return 25;
            }
            if (arrowDistance > GetScoreDistance(scroeMultiplyPoint.ThreetimesPoint1)
                && arrowDistance < GetScoreDistance(scroeMultiplyPoint.ThreetimesPoint2))
            {
                return scores * 3;
            }
            if (arrowDistance > GetScoreDistance(scroeMultiplyPoint.TwotimesPoint1)
                && arrowDistance < GetScoreDistance(scroeMultiplyPoint.TwotimesPoint2))
            {
                return scores * 2;
            }
            if (arrowDistance > GetScoreDistance(scroeMultiplyPoint.TwotimesPoint2))
            {
                return 0;
            }

            return scores;
        }

        public float GetScoreDistance(Transform Point)
        {
            float Distance = Vector3.Magnitude(center.position - Point.position);

            print(Distance);
            return Distance;
        }
    }

    [Serializable]
    public struct ScroeSetPoint
    {
        public Transform FiftyPoint;
        public Transform TwentyfivePoint;
        public Transform ThreetimesPoint1;
        public Transform ThreetimesPoint2;
        public Transform TwotimesPoint1;
        public Transform TwotimesPoint2;
    }
}
