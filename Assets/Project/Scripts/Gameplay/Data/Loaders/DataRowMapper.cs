using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;


/// <summary>
/// [Loaders] 스크립트. 역할/설정/변수 용도를 코드 주석으로 확인할 수 있도록 정리했습니다.
/// Unity 연결: MonoBehaviour 스크립트는 Scene/GameObject에 컴포넌트로 부착 후 Inspector 필드를 설정하세요.
/// 데이터 클래스는 엑셀 시트 컬럼과 필드명을 맞춰 DataBootstrap 로딩 파이프라인에서 자동 매핑됩니다.
/// </summary>
namespace Relic.Gameplay.Data
{
    /// <summary>
    /// DataRowMapper의 책임을 담당하는 클래스입니다. 파일 상단 주석의 연결/설정 지침을 참고하세요.
    /// </summary>
    public static class DataRowMapper
    {
        public static List<T> MapList<T>(IReadOnlyList<Dictionary<string, string>> rows) where T : new()
        {
            var list = new List<T>();
            if (rows == null)
                return list;

            foreach (var row in rows)
                list.Add(Map<T>(row));

            return list;
        }

        public static T Map<T>(Dictionary<string, string> row) where T : new()
        {
            var target = new T();
            if (row == null)
                return target;

            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (!TryGetValue(row, field.Name, out var raw))
                    continue;

                var converted = ConvertValue(field.FieldType, raw);
                if (converted != null || field.FieldType == typeof(string))
                    field.SetValue(target, converted);
            }

            return target;
        }

        private static bool TryGetValue(Dictionary<string, string> row, string key, out string value)
        {
            if (row.TryGetValue(key, out value))
                return true;

            var normalized = key.Replace("_", string.Empty);
            foreach (var pair in row)
            {
                var candidate = pair.Key.Replace("_", string.Empty).Replace(" ", string.Empty);
                if (string.Equals(candidate, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    value = pair.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static object ConvertValue(Type type, string raw)
        {
            if (type == typeof(string))
                return raw;
            if (type == typeof(int))
                return int.TryParse(raw, out var i) ? i : 0;
            if (type == typeof(float))
                return float.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var f) ? f : 0f;
            if (type == typeof(bool))
                return raw == "1" || bool.TryParse(raw, out var b) && b;
            if (type.IsEnum)
                return Enum.TryParse(type, raw, true, out var e) ? e : Activator.CreateInstance(type);

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var tokens = SplitTokens(raw);
                var array = Array.CreateInstance(elementType, tokens.Length);
                for (var i = 0; i < tokens.Length; i++)
                    array.SetValue(ConvertValue(elementType, tokens[i]), i);
                return array;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = type.GetGenericArguments()[0];
                var tokens = SplitTokens(raw);
                var list = (IList)Activator.CreateInstance(type);
                foreach (var token in tokens)
                    list.Add(ConvertValue(elementType, token));
                return list;
            }

            return null;
        }

        private static string[] SplitTokens(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();
            return raw.Split(new[] { ';', '|', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
        }
    }
}
