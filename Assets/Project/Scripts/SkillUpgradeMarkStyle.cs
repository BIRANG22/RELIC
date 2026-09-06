using UnityEngine;
using UnityEngine.UI;

namespace Relic.Gameplay.Data
{
    /// <summary>
    /// 모든 강화 스킬 UI가 공통으로 사용하는 + 마크 설정입니다.
    /// Bootstrap 씬의 UIManager 자식 GameObject 하나에 붙여 두면 모든 스킬 UI가 같은 설정을 사용합니다.
    /// </summary>
    public sealed class SkillUpgradeMarkStyle : MonoBehaviour
    {
        private const string MarkObjectName = "UpgradeMark";

        private static SkillUpgradeMarkStyle instance;
        private static bool missingInstanceWarningShown;

        [Header("강화 스킬 + 마크")]
        [SerializeField] private Sprite markSprite;
        [SerializeField, Range(0.05f, 1f)] private float markSizeRatio = 0.35f;
        [SerializeField] private Color markColor = Color.white;

        public static SkillUpgradeMarkStyle Shared
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SkillUpgradeMarkStyle>(FindObjectsInactive.Include);
                }

                if (instance == null && !missingInstanceWarningShown)
                {
                    missingInstanceWarningShown = true;
                    Debug.LogWarning(
                        "[SkillUpgradeMarkStyle] 공용 강화 마크 설정을 찾을 수 없습니다. " +
                        "Bootstrap 씬의 GameObject 하나에 SkillUpgradeMarkStyle을 붙여 주세요.");
                }

                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            missingInstanceWarningShown = false;
            // UIManager의 자식으로 사용하므로 여기서는 DontDestroyOnLoad를 직접 호출하지 않습니다.
            // 부모 UIManager가 유지되면 이 설정 오브젝트도 함께 유지됩니다.
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public static void ApplyShared(Image iconImage, string skillId)
        {
            if (iconImage == null)
                return;

            SkillUpgradeMarkStyle style = Shared;
            if (style != null)
            {
                style.Apply(iconImage, skillId);
                return;
            }

            SetExistingMarkActive(iconImage, false);
        }

        /// <summary>
        /// 이미 조회된 스킬 데이터를 사용해 강화 마크를 적용합니다.
        /// UI가 실제로 표시 중인 SkillMasterData.Level을 직접 사용하므로 가장 확실한 경로입니다.
        /// </summary>
        public static void ApplyShared(Image iconImage, SkillMasterData skillData)
        {
            if (iconImage == null)
                return;

            SkillUpgradeMarkStyle style = Shared;
            if (style != null)
            {
                style.Apply(iconImage, skillData);
                return;
            }

            SetExistingMarkActive(iconImage, false);
        }

        public void Apply(Image iconImage, string skillId)
        {
            if (iconImage == null)
                return;

            bool isUpgraded = IsLevelTwoSkill(skillId);
            ApplyInternal(iconImage, isUpgraded);
        }

        public void Apply(Image iconImage, SkillMasterData skillData)
        {
            if (iconImage == null)
                return;

            bool isUpgraded = skillData != null && skillData.Level >= 2;
            ApplyInternal(iconImage, isUpgraded);
        }

        private void ApplyInternal(Image iconImage, bool isUpgraded)
        {
            Image markImage = ResolveOrCreateMark(iconImage, isUpgraded);
            if (markImage == null)
                return;

            bool canShow = isUpgraded && markSprite != null;
            markImage.gameObject.SetActive(canShow);

            if (!canShow)
                return;

            markImage.sprite = markSprite;
            markImage.color = markColor;
            markImage.raycastTarget = false;

            RectTransform rect = markImage.rectTransform;
            float iconSize = GetIconReferenceSize(iconImage.rectTransform);
            float markSize = iconSize * Mathf.Clamp(markSizeRatio, 0.05f, 1f);

            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.sizeDelta = new Vector2(markSize, markSize);
            rect.anchoredPosition = Vector2.zero;
            markImage.transform.SetAsLastSibling();
        }

        private static float GetIconReferenceSize(RectTransform iconRect)
        {
            if (iconRect == null)
                return 28f;

            float width = Mathf.Abs(iconRect.rect.width);
            float height = Mathf.Abs(iconRect.rect.height);
            float referenceSize = Mathf.Min(width, height);

            if (referenceSize > 0.01f)
                return referenceSize;

            width = Mathf.Abs(iconRect.sizeDelta.x);
            height = Mathf.Abs(iconRect.sizeDelta.y);
            referenceSize = Mathf.Min(width, height);

            return referenceSize > 0.01f ? referenceSize : 28f;
        }

        private static bool IsLevelTwoSkill(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
                return false;

            skillId = skillId.Trim();

            if (DataManager.Instance != null &&
                DataManager.Instance.SkillDatabase != null &&
                DataManager.Instance.SkillDatabase.TryGet(skillId, out SkillMasterData skillData) &&
                skillData != null)
            {
                return skillData.Level >= 2;
            }

            // 데이터베이스가 아직 준비되지 않은 아주 이른 시점에는 기존 ID 규칙을 보조 판정으로 사용합니다.
            return SkillRarityUtility.IsUpgradeSkillVariant(skillId);
        }

        private Image ResolveOrCreateMark(Image iconImage, bool createIfMissing)
        {
            Transform markTransform = iconImage.transform.Find(MarkObjectName);
            Image markImage = markTransform != null ? markTransform.GetComponent<Image>() : null;

            if (markImage == null && createIfMissing && markSprite != null)
            {
                GameObject markObject = new GameObject(
                    MarkObjectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

                RectTransform rect = markObject.GetComponent<RectTransform>();
                rect.SetParent(iconImage.transform, false);
                rect.anchorMin = Vector2.one;
                rect.anchorMax = Vector2.one;
                rect.pivot = Vector2.one;

                markImage = markObject.GetComponent<Image>();
            }

            if (markImage != null)
                markImage.raycastTarget = false;

            return markImage;
        }

        private static void SetExistingMarkActive(Image iconImage, bool active)
        {
            Transform markTransform = iconImage.transform.Find(MarkObjectName);
            if (markTransform == null)
                return;

            Image markImage = markTransform.GetComponent<Image>();
            if (markImage != null)
                markImage.raycastTarget = false;

            markTransform.gameObject.SetActive(active);
        }
    }
}
