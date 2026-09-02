using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public enum SkillAttackSlot
    {
        None = 0,

        Attack1 = 1,
        Attack2 = 2,
        Attack3 = 3,

        Power = 4,
        Skill = 5,

        Extra1 = 6,
        Extra2 = 7,
        Extra3 = 8,
        Extra4 = 9,
        Extra5 = 10
    }

    [CreateAssetMenu(menuName = "Relic/Data/Skill Attack Override Database")]
    public class SkillAttackOverrideDatabase : ScriptableObject
    {
        [SerializeField] private List<SkillAttackOverrideEntry> entries = new();

        private Dictionary<string, SkillAttackOverrideEntry> map;

        public void Initialize()
        {
            map = new Dictionary<string, SkillAttackOverrideEntry>();

            foreach (SkillAttackOverrideEntry entry in entries)
            {
                if (entry == null)
                    continue;

                string characterId = NormalizeId(entry.CharacterId);
                string skillId = NormalizeId(entry.SkillId);

                if (string.IsNullOrWhiteSpace(characterId) ||
                    string.IsNullOrWhiteSpace(skillId) ||
                    entry.AttackSlot == SkillAttackSlot.None)
                {
                    continue;
                }

                string key = MakeKey(characterId, skillId);

                if (map.ContainsKey(key))
                {
                    Debug.LogWarning(
                        $"[SkillAttackOverrideDatabase] Duplicate override: {characterId} / {skillId}");
                    continue;
                }

                map.Add(key, entry);
            }
        }

        public bool TryGetPresentationSlot(
            string characterId,
            string skillId,
            out SkillAttackSlot slot)
        {
            slot = SkillAttackSlot.None;

            if (!TryGetEntry(characterId, skillId, out SkillAttackOverrideEntry entry))
                return false;

            slot = entry.AttackSlot;
            return slot != SkillAttackSlot.None;
        }

        /// <summary>
        /// 2타 이후 사용할 랜덤 애니메이션 슬롯을 반환합니다.
        /// 직전 타격에서 사용한 슬롯은 가능한 경우 제외합니다.
        /// </summary>
        public bool TryGetRepeatPresentationSlot(
            string characterId,
            string skillId,
            SkillAttackSlot previousSlot,
            out SkillAttackSlot slot)
        {
            slot = SkillAttackSlot.None;

            if (!TryGetEntry(characterId, skillId, out SkillAttackOverrideEntry entry))
                return false;

            List<SkillAttackSlot> repeatSlots = entry.RepeatAttackSlots;

            if (repeatSlots == null || repeatSlots.Count == 0)
            {
                // 반복 애니가 지정되지 않은 기존 데이터는
                // 기존 AttackSlot을 그대로 사용합니다.
                slot = entry.AttackSlot;
                return slot != SkillAttackSlot.None;
            }

            List<SkillAttackSlot> validSlots = new();

            for (int i = 0; i < repeatSlots.Count; i++)
            {
                SkillAttackSlot candidate = repeatSlots[i];

                if (candidate == SkillAttackSlot.None)
                    continue;

                // 바로 직전 타격 애니메이션은 제외
                if (candidate == previousSlot)
                    continue;

                // 같은 슬롯이 리스트에 중복 등록되어 있어도 한 번만 후보에 추가
                if (!validSlots.Contains(candidate))
                    validSlots.Add(candidate);
            }

            if (validSlots.Count > 0)
            {
                slot = validSlots[UnityEngine.Random.Range(0, validSlots.Count)];
                return true;
            }

            // 후보가 하나뿐이고 그게 직전 애니인 경우 등
            // 제외한 뒤 아무것도 남지 않으면 반복 리스트에서 다시 선택
            validSlots.Clear();

            for (int i = 0; i < repeatSlots.Count; i++)
            {
                SkillAttackSlot candidate = repeatSlots[i];

                if (candidate == SkillAttackSlot.None)
                    continue;

                if (!validSlots.Contains(candidate))
                    validSlots.Add(candidate);
            }

            if (validSlots.Count > 0)
            {
                slot = validSlots[UnityEngine.Random.Range(0, validSlots.Count)];
                return true;
            }

            // 리스트에 None만 들어있는 경우
            slot = entry.AttackSlot;
            return slot != SkillAttackSlot.None;
        }

        public bool TryGetAttackSlot(
            string characterId,
            string skillId,
            out SkillAttackSlot attackSlot)
        {
            return TryGetPresentationSlot(characterId, skillId, out attackSlot);
        }

        public bool TryGetAttackIndex(
            string characterId,
            string skillId,
            out int attackIndex)
        {
            attackIndex = 0;

            if (!TryGetPresentationSlot(
                    characterId,
                    skillId,
                    out SkillAttackSlot slot))
            {
                return false;
            }

            if (slot < SkillAttackSlot.Attack1 ||
                slot > SkillAttackSlot.Attack3)
            {
                return false;
            }

            attackIndex = (int)slot;
            return true;
        }

        private bool TryGetEntry(
            string characterId,
            string skillId,
            out SkillAttackOverrideEntry entry)
        {
            entry = null;

            characterId = NormalizeId(characterId);
            skillId = NormalizeId(skillId);

            if (string.IsNullOrWhiteSpace(characterId) ||
                string.IsNullOrWhiteSpace(skillId))
            {
                return false;
            }

            if (map == null)
                Initialize();

            return map.TryGetValue(
                MakeKey(characterId, skillId),
                out entry);
        }

        private static string NormalizeId(string id)
        {
            return string.IsNullOrWhiteSpace(id)
                ? string.Empty
                : id.Trim();
        }

        private static string MakeKey(
            string characterId,
            string skillId)
        {
            return $"{characterId}\n{skillId}";
        }
    }

    [Serializable]
    public class SkillAttackOverrideEntry
    {
        public string CharacterId;

        public string SkillId;

        [Tooltip("첫 번째 타격에서 사용할 애니메이션")]
        public SkillAttackSlot AttackSlot = SkillAttackSlot.None;

        [Tooltip("2번째 타격부터 랜덤으로 사용할 애니메이션 목록")]
        public List<SkillAttackSlot> RepeatAttackSlots = new();
    }
}

#if UNITY_EDITOR
namespace Relic.Gameplay.Data
{
    [UnityEditor.CustomPropertyDrawer(typeof(SkillAttackOverrideEntry))]
    public class SkillAttackOverrideEntryDrawer : UnityEditor.PropertyDrawer
    {
        private const float Spacing = 2f;

        public override void OnGUI(
            Rect position,
            UnityEditor.SerializedProperty property,
            GUIContent label)
        {
            UnityEditor.SerializedProperty skillIdProperty =
                property.FindPropertyRelative("SkillId");

            string skillId =
                skillIdProperty != null
                    ? skillIdProperty.stringValue?.Trim()
                    : string.Empty;

            string displayName =
                string.IsNullOrEmpty(skillId)
                    ? label.text
                    : skillId;

            UnityEditor.EditorGUI.BeginProperty(
                position,
                label,
                property);

            float lineHeight =
                UnityEditor.EditorGUIUtility.singleLineHeight;

            Rect foldoutRect = new Rect(
                position.x,
                position.y,
                position.width,
                lineHeight);

            property.isExpanded =
                UnityEditor.EditorGUI.Foldout(
                    foldoutRect,
                    property.isExpanded,
                    new GUIContent(displayName),
                    true);

            if (property.isExpanded)
            {
                int previousIndent =
                    UnityEditor.EditorGUI.indentLevel;

                UnityEditor.EditorGUI.indentLevel =
                    previousIndent + 1;

                float y =
                    foldoutRect.yMax + Spacing;

                DrawProperty(
                    property,
                    "CharacterId",
                    position.x,
                    ref y,
                    position.width);

                DrawProperty(
                    property,
                    "SkillId",
                    position.x,
                    ref y,
                    position.width);

                DrawProperty(
                    property,
                    "AttackSlot",
                    position.x,
                    ref y,
                    position.width);

                DrawProperty(
                    property,
                    "RepeatAttackSlots",
                    position.x,
                    ref y,
                    position.width,
                    true);

                UnityEditor.EditorGUI.indentLevel =
                    previousIndent;
            }

            UnityEditor.EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(
            UnityEditor.SerializedProperty property,
            GUIContent label)
        {
            float lineHeight =
                UnityEditor.EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded)
                return lineHeight;

            float height = lineHeight + Spacing;

            height += GetHeight(property, "CharacterId");
            height += GetHeight(property, "SkillId");
            height += GetHeight(property, "AttackSlot");
            height += GetHeight(
                property,
                "RepeatAttackSlots",
                true);

            return height;
        }

        private static float GetHeight(
            UnityEditor.SerializedProperty parent,
            string propertyName,
            bool includeChildren = false)
        {
            UnityEditor.SerializedProperty child =
                parent.FindPropertyRelative(propertyName);

            if (child == null)
                return 0f;

            return UnityEditor.EditorGUI.GetPropertyHeight(
                       child,
                       includeChildren)
                   + Spacing;
        }

        private static void DrawProperty(
            UnityEditor.SerializedProperty parent,
            string propertyName,
            float x,
            ref float y,
            float width,
            bool includeChildren = false)
        {
            UnityEditor.SerializedProperty child =
                parent.FindPropertyRelative(propertyName);

            if (child == null)
                return;

            float height =
                UnityEditor.EditorGUI.GetPropertyHeight(
                    child,
                    includeChildren);

            Rect rect =
                new Rect(
                    x,
                    y,
                    width,
                    height);

            UnityEditor.EditorGUI.PropertyField(
                rect,
                child,
                includeChildren);

            y += height + Spacing;
        }
    }
}
#endif