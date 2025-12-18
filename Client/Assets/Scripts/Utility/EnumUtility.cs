using System;

namespace Assets.Scripts.Utility
{
    public static class EnumUtility
    {
        private static class EnumCache<T> where T : struct, Enum
        {
            public static readonly T[] Values = (T[])Enum.GetValues(typeof(T));
            public static readonly int Length = Values.Length;
        }

        /// <summary>
        /// Enum의 요소개수를 리턴합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static int Count<T>() where T : struct, Enum
        {
            return EnumCache<T>.Length;
        }

        /// <summary>
        /// int 값을 Enum으로 변환합니다.
        /// </summary>
        public static T IntToEnum<T>(int value) where T : struct, Enum
        {
            int length = EnumCache<T>.Length;

            if (length <= 0)
            {
                return default;
            }

            if (value < 0)
            {
                value = 0;
            }
            else if (value >= length)
            {
                value = length - 1;
            }

            return EnumCache<T>.Values[value];
        }

        /// <summary>
        /// Enum 값을 주어진 델타만큼 이동시킵니다.
        /// 순회가 필요할 경우 다음 함수를 사용하시오
        /// ShiftWrap(T, int)
        /// </summary>
        public static T Shift<T>(T current, int delta) where T : struct, Enum
        {
            int index = Convert.ToInt32(current);
            int nextIndex = index + delta;

            return IntToEnum<T>(nextIndex);
        }

        /// <summary>
        /// Enum 요소를 순회할 경우 사용하는 함수
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="current"></param>
        /// <param name="delta"></param>
        /// <returns></returns>
        public static T ShiftWrap<T>(T current, int delta) where T : struct, Enum
        {
            int count = EnumCache<T>.Length;

            if (count <= 0)
            {
                return default;
            }

            int index = Convert.ToInt32(current);
            int next = ((index + delta) % count + count) % count;

            return EnumCache<T>.Values[next];
        }

        /// <summary>
        /// 제공받은 Enum의 인덱스를 반환합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int EnumToInt<T>(T value) where T : struct, Enum
        {
            return Convert.ToInt32(value);
        }

        /// <summary>
        /// 문자열에 해당하는 Enum을 조회하여 있으면 반환합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryParseEnum<T>(string name, out T result) where T : struct, Enum
        {
            result = default;

            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return Enum.TryParse(name, true, out result);
        }

        /// <summary>
        /// Enum의 요소값 문자열로 반환합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToString<T>(T value) where T : struct, Enum
        {
            return Enum.GetName(typeof(T), value);
        }
    }
}
