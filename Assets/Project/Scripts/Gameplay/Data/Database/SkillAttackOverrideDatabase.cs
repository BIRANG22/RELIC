using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public enum SkillAttackSlot
    {
        None = 0,

        // 기존 Unity 직렬화 값 보존을 위해 Attack1~3 값은 변경하지 않습니다.
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

        private Dictionary<string, SkillAttackSlot> map;

        public void Initialize()
        {
            map = new Dictionary<string, SkillAttackSlot>();

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

                map.Add(key, entry.AttackSlot);
            }
        }

        public bool TryGetPresentationSlot(
            string characterId,
            string skillId,
            out SkillAttackSlot slot)
        {
            slot = SkillAttackSlot.None;

            characterId = NormalizeId(characterId);
            skillId = NormalizeId(skillId);

            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(skillId))
                return false;

            if (map == null)
                Initialize();

            return map.TryGetValue(MakeKey(characterId, skillId), out slot);
        }

        // 기존 호출부 호환용.
        public bool TryGetAttackSlot(
            string characterId,
            string skillId,
            out SkillAttackSlot attackSlot)
        {
            return TryGetPresentationSlot(characterId, skillId, out attackSlot);
        }

        // 기존 호출부 호환용. Power/Skill 슬롯은 공격 인덱스가 아니므로 false를 반환합니다.
        public bool TryGetAttackIndex(string characterId, string skillId, out int attackIndex)
        {
            attackIndex = 0;

            if (!TryGetPresentationSlot(characterId, skillId, out SkillAttackSlot slot))
                return false;

            if (slot < SkillAttackSlot.Attack1 || slot > SkillAttackSlot.Attack3)
                return false;

            attackIndex = (int)slot;
            return true;
        }

        private static string NormalizeId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        }

        private static string MakeKey(string characterId, string skillId)
        {
            return $"{characterId}\n{skillId}";
        }
    }

    [Serializable]
    public class SkillAttackOverrideEntry
    {
        public string CharacterId;
        public string SkillId;
        public SkillAttackSlot AttackSlot = SkillAttackSlot.None;
    }
}

#if UNITY_EDITOR
namespace Relic.Gameplay.Data
{
    [UnityEditor.CustomPropertyDrawer(typeof(SkillAttackOverrideEntry))]
    public class SkillAttackOverrideEntryDrawer : UnityEditor.PropertyDrawer
    {
        private const float Spacing = 2f;

        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            UnityEditor.SerializedProperty skillIdProperty = property.FindPropertyRelative("SkillId");
            string skillId = skillIdProperty != null ? skillIdProperty.stringValue?.Trim() : string.Empty;
            string displayName = string.IsNullOrEmpty(skillId) ? label.text : skillId;

            UnityEditor.EditorGUI.BeginProperty(position, label, property);

            Rect foldoutRect = new Rect(
                position.x,
                position.y,
                position.width,
                UnityEditor.EditorGUIUtility.singleLineHeight);

            property.isExpanded = UnityEditor.EditorGUI.Foldout(
                foldoutRect,
                property.isExpanded,
                new GUIContent(displayName),
                true);

            if (property.isExpanded)
            {
                int previousIndent = UnityEditor.EditorGUI.indentLevel;
                UnityEditor.EditorGUI.indentLevel = previousIndent + 1;

                float lineHeight = UnityEditor.EditorGUIUtility.singleLineHeight;
                float y = foldoutRect.yMax + Spacing;

                DrawProperty(property, "CharacterId", position.x, ref y, position.width, lineHeight);
                DrawProperty(property, "SkillId", position.x, ref y, position.width, lineHeight);
                DrawProperty(property, "AttackSlot", position.x, ref y, position.width, lineHeight);

                UnityEditor.EditorGUI.indentLevel = previousIndent;
            }

            UnityEditor.EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
        {
            float lineHeight = UnityEditor.EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded)
                return lineHeight;

            return (lineHeight * 4f) + (Spacing * 3f);
        }

        private static void DrawProperty(
            UnityEditor.SerializedProperty parent,
            string propertyName,
            float x,
            ref float y,
            float width,
            float lineHeight)
        {
            UnityEditor.SerializedProperty child = parent.FindPropertyRelative(propertyName);
            if (child == null)
                return;

            Rect rect = new Rect(x, y, width, lineHeight);
            UnityEditor.EditorGUI.PropertyField(rect, child);
            y += lineHeight + Spacing;
        }
    }
}
#endif
