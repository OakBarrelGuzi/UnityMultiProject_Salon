using UnityEngine;

namespace Salon.Firebase.Database
{
    public static class NetworkPositionCompressor
    {
        // 안전한 ASCII 범위 사용 (33 ~ 126, 총 94개 문자)
        private const char ASCII_START = '!';  // 33
        private const char ASCII_END = '~';    // 126
        private const int ASCII_RANGE = 94;    // 사용 가능한 문자 수
        private const float POSITION_RANGE = 1000f; // ±500 범위

        public static string CompressVector3(Vector3 position, Vector3 direction, bool isMoving)
        {
            char[] compressed = new char[5]; // positionX(2) + positionZ(2) + direction&moving(1)

            // 위치 압축 (각 좌표를 2개의 문자로 표현, 94*94=8836 단계)
            CompressCoordinate(position.x, out compressed[0], out compressed[1]);
            CompressCoordinate(position.z, out compressed[2], out compressed[3]);

            // 방향과 이동 상태를 하나의 문자로 압축
            // 방향은 8방향(0~7)으로 양자화하고, isMoving은 1비트 사용
            int directionIndex = QuantizeDirection(direction);
            compressed[4] = (char)(ASCII_START + (directionIndex << 1) + (isMoving ? 1 : 0));

            return new string(compressed);
        }

        public static (Vector3 position, Vector3 direction, bool isMoving) DecompressToVectors(string compressed)
        {
            if (string.IsNullOrEmpty(compressed) || compressed.Length < 5)
                return (Vector3.zero, Vector3.forward, false);

            // 위치 복원
            float x = DecompressCoordinate(compressed[0], compressed[1]);
            float z = DecompressCoordinate(compressed[2], compressed[3]);

            // 방향과 이동 상태 복원
            int combinedValue = compressed[4] - ASCII_START;
            int directionIndex = combinedValue >> 1;
            bool isMoving = (combinedValue & 1) == 1;

            Vector3 direction = DeQuantizeDirection(directionIndex);

            return (new Vector3(x, 0, z), direction, isMoving);
        }

        private static void CompressCoordinate(float value, out char high, out char low)
        {
            // -POSITION_RANGE ~ POSITION_RANGE를 0 ~ (ASCII_RANGE * ASCII_RANGE - 1)로 매핑
            float normalized = Mathf.InverseLerp(-POSITION_RANGE, POSITION_RANGE, value);
            int intValue = Mathf.RoundToInt(normalized * (ASCII_RANGE * ASCII_RANGE - 1));

            // 상위/하위 문자로 분할
            high = (char)(ASCII_START + (intValue / ASCII_RANGE));
            low = (char)(ASCII_START + (intValue % ASCII_RANGE));
        }

        private static float DecompressCoordinate(char high, char low)
        {
            // 두 문자를 하나의 값으로 결합
            int highValue = high - ASCII_START;
            int lowValue = low - ASCII_START;
            int combined = (highValue * ASCII_RANGE) + lowValue;

            // 정규화된 값으로 변환 후 실제 좌표로 복원
            float normalized = combined / (float)(ASCII_RANGE * ASCII_RANGE - 1);
            return Mathf.Lerp(-POSITION_RANGE, POSITION_RANGE, normalized);
        }

        private static int QuantizeDirection(Vector3 direction)
        {
            // 방향을 8방향으로 양자화
            float angle = Mathf.Atan2(direction.z, direction.x);
            float normalized = (angle + Mathf.PI) / (2f * Mathf.PI);
            return Mathf.RoundToInt(normalized * 7f);
        }

        private static Vector3 DeQuantizeDirection(int index)
        {
            // 8방향 인덱스를 Vector3로 변환 (방향 반전)
            float angle = (index / 7f) * 2f * Mathf.PI;
            return new Vector3(-Mathf.Cos(angle), 0, -Mathf.Sin(angle));
        }
    }
}