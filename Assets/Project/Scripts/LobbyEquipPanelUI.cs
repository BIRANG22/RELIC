using System;
using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 장비 관리용 Equip_panel 컨트롤러입니다.
/// 패널 자체는 항상 활성 상태로 유지하고 Equip/Charter의 위치로 열림/닫힘을 표현합니다.
/// 또한 Charter/Char1~3에 현재 파티 캐릭터의 이름, 마크, 연성제, 유물, 교체 가능한 기억을 표시합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LobbyEquipPanelUI : MonoBehaviour
{
    private const int CharacterCount = 3;
    private const int VisibleRelicSlotCount = 6;
    private const int VisibleSkillSlotCount = 3;

    // 로비 Equip_panel의 Skill 1~3은 교체 가능한 기억만 표시합니다.
    // Skill1 = 구현 기억(AbilitySkillId / EquippedSkillIds[1])
    // Skill2 = 자유 장착 기억 1(EquippedSkillIds[2])
    // Skill3 = 자유 장착 기억 2(EquippedSkillIds[3])
    // 본능 기억(PassiveSkillId)과 발현 기억(UniqueSkillId)은 표시하지 않습니다.
    private static readonly int[] RuntimeSkillSlotIndices = { 1, 2, 3 };

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Slide Targets")]
    [SerializeField] private RectTransform equipRect;
    [SerializeField] private RectTransform charterRect;

    [Header("Slide Position")]
    [SerializeField] private float equipStartX = -1350f;
    [SerializeField] private float equipEndX = -450f;
    [SerializeField] private float charterStartX = 1350f;
    [SerializeField] private float charterEndX = 450f;

    [Header("Slide Animation")]
    [SerializeField, Min(0f)] private float slideDuration = 0.35f;
    [SerializeField]
    private AnimationCurve slideCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2f),
        new Keyframe(1f, 1f, 0f, 0f));

    [Header("Close Input")]
    [SerializeField] private bool closeOnOutsideClick = true;

    [Header("Opened Panel")]
    [SerializeField] private bool bringToFront = true;

    [Header("Character Data")]
    [Tooltip("Charter/Char1~3 구조를 이름으로 자동 연결합니다.")]
    [SerializeField] private bool autoBindCharacterHierarchy = true;

    private readonly CharacterView[] characterViews = new CharacterView[CharacterCount];

    private Coroutine slideAnimationCoroutine;
    private RectTransform toggleButtonRect;
    private bool isOpen;
    private bool isClosing;
    private int lastToggleFrame = -1;

    public bool IsOpen => isOpen && !isClosing;

    private void Awake()
    {
        ResolvePanelRoot();
        ResolveSlideTargets();
        ResolveCharacterViewsIfNeeded();
        ResetSlidePositions();
        isOpen = false;
        isClosing = false;
    }

    private void OnEnable()
    {
        // Equip_panel은 항상 활성화된 상태를 유지합니다.
        // 다시 활성화된 경우에도 닫힌 위치에서 시작합니다.
        if (!isOpen && !isClosing)
            ResetSlidePositions();

        RefreshCharacterData();
    }

    private void Update()
    {
        if (!IsOpen || Time.frameCount == lastToggleFrame)
            return;

        if (!closeOnOutsideClick || !Input.GetMouseButtonDown(0))
            return;

        Vector2 pointerPosition = Input.mousePosition;
        if (IsPointerInsideOpenArea(pointerPosition))
            return;

        Close();
    }

    private void OnDisable()
    {
        StopSlideAnimation();
        isOpen = false;
        isClosing = false;
        ResetSlidePositions();
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    private void OnDestroy()
    {
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    /// <summary>
    /// Equip 버튼 자신의 RectTransform을 등록합니다.
    /// 버튼 클릭을 패널 바깥 클릭으로 오인하지 않도록 사용합니다.
    /// </summary>
    public void SetToggleButton(RectTransform buttonRect)
    {
        toggleButtonRect = buttonRect;
    }

    /// <summary>
    /// Equip 버튼에서 호출합니다.
    /// 닫혀 있으면 열고, 열려 있으면 시작 위치로 슬라이드 아웃합니다.
    /// </summary>
    public void Toggle()
    {
        lastToggleFrame = Time.frameCount;

        if (isOpen && !isClosing)
            Close();
        else
            Open();
    }

    public void Open()
    {
        GameObject root = ResolvePanelRoot();
        if (root == null)
        {
            Debug.LogWarning("[LobbyEquipPanelUI] Equip_panel을 찾을 수 없습니다.", this);
            return;
        }

        if (isOpen && !isClosing)
            return;

        if (LobbyPositionModalInputBlocker.IsBlockedByAnother(this))
            return;

        if (UIPanelButton.IsMenuPanelOpen)
            return;

        TitleManager.CloseTitleModePanelsExceptInScene(root);

        if (!root.activeSelf)
            root.SetActive(true);

        if (bringToFront)
            root.transform.SetAsLastSibling();

        ResolveSlideTargets();
        ResolveCharacterViewsIfNeeded();
        RefreshCharacterData();
        StopSlideAnimation();

        // 닫히는 도중 다시 열면 현재 위치에서 자연스럽게 이어서 엽니다.
        isClosing = false;
        isOpen = true;
        LobbyPositionModalInputBlocker.Block(this);
        slideAnimationCoroutine = StartCoroutine(PlaySlideAnimation(true));
    }

    public void Close()
    {
        if (!isOpen || isClosing)
            return;

        ResolveSlideTargets();
        StopSlideAnimation();
        isClosing = true;
        isOpen = false;
        slideAnimationCoroutine = StartCoroutine(PlaySlideAnimation(false));
    }

    /// <summary>
    /// 현재 PartyRuntimeStore / CharacterRuntimeStore 기준으로 Char1~3 표시를 다시 갱신합니다.
    /// 파티 변경, 기억 장착, 연성제/유물 장착 후 필요하면 외부에서도 호출할 수 있습니다.
    /// </summary>
    public void RefreshCharacterData()
    {
        ResolveCharacterViewsIfNeeded();

        DataManager dataManager = DataManager.Instance;
        if (dataManager == null)
        {
            ClearCharacterViews();
            return;
        }

        PartyRuntimeStore partyStore = dataManager.PartyRuntimeStore;
        CharacterRuntimeStore characterStore = dataManager.CharacterRuntimeStore;

        if (partyStore == null || characterStore == null)
        {
            ClearCharacterViews();
            return;
        }

        for (int i = 0; i < CharacterCount; i++)
        {
            CharacterView view = characterViews[i];
            if (view == null)
                continue;

            string characterId = partyStore.GetCharacterId(i);
            bool hasCharacter = !string.IsNullOrWhiteSpace(characterId);

            if (view.Root != null)
                view.Root.gameObject.SetActive(hasCharacter);

            if (!hasCharacter)
            {
                ClearCharacterView(view);
                continue;
            }

            CharacterMasterData master = null;
            dataManager.CharacterDatabase?.TryGet(characterId, out master);

            CharacterRuntimeData runtime = null;
            characterStore.TryGet(characterId, out runtime);

            RefreshCharacterIdentity(view, characterId, master);
            RefreshCharacterActiveCompound(view, runtime);
            RefreshCharacterRelics(view, runtime);
            RefreshCharacterSkills(view, runtime);
        }
    }

    public static void RefreshAllCharacterData()
    {
        LobbyEquipPanelUI[] panels = FindObjectsByType<LobbyEquipPanelUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
                panels[i].RefreshCharacterData();
        }
    }

    private IEnumerator PlaySlideAnimation(bool opening)
    {
        ResolveSlideTargets();

        float equipFromX = equipRect != null ? equipRect.anchoredPosition.x : (opening ? equipStartX : equipEndX);
        float charterFromX = charterRect != null ? charterRect.anchoredPosition.x : (opening ? charterStartX : charterEndX);
        float equipToX = opening ? equipEndX : equipStartX;
        float charterToX = opening ? charterEndX : charterStartX;

        if (slideDuration <= 0f)
        {
            SetAnchoredX(equipRect, equipToX);
            SetAnchoredX(charterRect, charterToX);
            FinishSlide(opening);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / slideDuration);
            float curveValue = slideCurve != null ? slideCurve.Evaluate(normalized) : normalized;

            SetAnchoredX(equipRect, Mathf.LerpUnclamped(equipFromX, equipToX, curveValue));
            SetAnchoredX(charterRect, Mathf.LerpUnclamped(charterFromX, charterToX, curveValue));
            yield return null;
        }

        SetAnchoredX(equipRect, equipToX);
        SetAnchoredX(charterRect, charterToX);
        FinishSlide(opening);
    }

    private void FinishSlide(bool opening)
    {
        slideAnimationCoroutine = null;

        if (opening)
        {
            isOpen = true;
            isClosing = false;
            return;
        }

        FinishClose();
    }

    private void FinishClose()
    {
        slideAnimationCoroutine = null;
        isOpen = false;
        isClosing = false;
        LobbyPositionModalInputBlocker.Unblock(this);

        // Equip_panel 자체는 비활성화하지 않습니다.
        // 닫힘 상태는 Equip=-1350, Charter=1350 위치로만 표현합니다.
    }

    private void RefreshCharacterIdentity(CharacterView view, string characterId, CharacterMasterData master)
    {
        if (view.NameText != null)
        {
            string displayName = master != null
                ? GameDataLocalization.CharacterName(master)
                : characterId;

            view.NameText.text = string.IsNullOrWhiteSpace(displayName) ? characterId : displayName;
        }

        Sprite mark1 = null;
        Sprite mark2 = null;

        CharacterIconDatabase iconDatabase = DataManager.Instance?.CharacterIconDatabase;
        if (iconDatabase != null)
        {
            iconDatabase.TryGetMark(characterId, out mark1);
            iconDatabase.TryGetMark2(characterId, out mark2);
        }

        ApplyImage(view.Mark1Image, mark1);
        ApplyImage(view.Mark2Image, mark2);
    }

    private void RefreshCharacterActiveCompound(CharacterView view, CharacterRuntimeData runtime)
    {
        string compoundId = ActiveRelicRuntimeUtility.GetActiveRelicId(runtime);
        Sprite icon = ResolveRelicIcon(compoundId);
        ApplyImage(view.ActiveCompoundIcon, icon);
    }

    private void RefreshCharacterRelics(CharacterView view, CharacterRuntimeData runtime)
    {
        if (runtime != null)
            ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);

        for (int i = 0; i < VisibleRelicSlotCount; i++)
        {
            int runtimeRelicIndex = i + 1; // 0번은 Active 연성제 슬롯입니다.
            string relicId = runtime?.EquippedRelicIds != null && runtimeRelicIndex < runtime.EquippedRelicIds.Length
                ? runtime.EquippedRelicIds[runtimeRelicIndex]
                : null;

            ApplyImage(view.RelicIcons[i], ResolveRelicIcon(relicId));
        }
    }

    private void RefreshCharacterSkills(CharacterView view, CharacterRuntimeData runtime)
    {
        for (int i = 0; i < VisibleSkillSlotCount; i++)
        {
            int runtimeIndex = RuntimeSkillSlotIndices[i];
            string skillId = GetEquippedSkillId(runtime, runtimeIndex);

            Sprite icon = null;
            if (!string.IsNullOrWhiteSpace(skillId) && DataManager.Instance?.SkillIconDatabase != null)
                DataManager.Instance.SkillIconDatabase.TryGetIcon(skillId, out icon);

            ApplyImage(view.SkillIcons[i], icon, SkillRarityUtility.GetSkillIconColor(skillId));
        }
    }

    private static string GetEquippedSkillId(CharacterRuntimeData runtime, int runtimeIndex)
    {
        if (runtime == null)
            return null;

        if (runtimeIndex == 1 && !string.IsNullOrWhiteSpace(runtime.AbilitySkillId))
            return runtime.AbilitySkillId;

        if (runtime.EquippedSkillIds == null ||
            runtimeIndex < 0 ||
            runtimeIndex >= runtime.EquippedSkillIds.Length)
        {
            return null;
        }

        return runtime.EquippedSkillIds[runtimeIndex];
    }

    private static Sprite ResolveRelicIcon(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId) || DataManager.Instance?.RelicIconDatabase == null)
            return null;

        DataManager.Instance.RelicIconDatabase.TryGetIcon(relicId, out Sprite icon);
        return icon;
    }

    private void ClearCharacterViews()
    {
        for (int i = 0; i < characterViews.Length; i++)
        {
            CharacterView view = characterViews[i];
            if (view == null)
                continue;

            ClearCharacterView(view);
        }
    }

    private static void ClearCharacterView(CharacterView view)
    {
        if (view == null)
            return;

        if (view.NameText != null)
            view.NameText.text = string.Empty;

        ApplyImage(view.Mark1Image, null);
        ApplyImage(view.Mark2Image, null);
        ApplyImage(view.ActiveCompoundIcon, null);

        for (int i = 0; i < view.RelicIcons.Length; i++)
            ApplyImage(view.RelicIcons[i], null);

        for (int i = 0; i < view.SkillIcons.Length; i++)
            ApplyImage(view.SkillIcons[i], null);
    }

    private static void ApplyImage(Image image, Sprite sprite)
    {
        ApplyImage(image, sprite, Color.white);
    }

    private static void ApplyImage(Image image, Sprite sprite, Color color)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.color = color;
        image.enabled = sprite != null;
    }

    private bool IsPointerInsideOpenArea(Vector2 screenPosition)
    {
        if (ContainsScreenPoint(equipRect, screenPosition))
            return true;

        if (ContainsScreenPoint(charterRect, screenPosition))
            return true;

        if (ContainsScreenPoint(toggleButtonRect, screenPosition))
            return true;

        return false;
    }

    private static bool ContainsScreenPoint(RectTransform rect, Vector2 screenPosition)
    {
        if (rect == null || !rect.gameObject.activeInHierarchy)
            return false;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera eventCamera = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = canvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera);
    }

    private void StopSlideAnimation()
    {
        if (slideAnimationCoroutine == null)
            return;

        StopCoroutine(slideAnimationCoroutine);
        slideAnimationCoroutine = null;
    }

    private void ResetSlidePositions()
    {
        ResolveSlideTargets();
        SetAnchoredX(equipRect, equipStartX);
        SetAnchoredX(charterRect, charterStartX);
    }

    private void ResolveSlideTargets()
    {
        GameObject root = ResolvePanelRoot();
        if (root == null)
            return;

        if (equipRect == null)
        {
            Transform equip = FindChildRecursive(root.transform, "Equip");
            if (equip != null)
                equipRect = equip as RectTransform;
        }

        if (charterRect == null)
        {
            Transform charter = FindChildRecursive(root.transform, "Charter");
            if (charter != null)
                charterRect = charter as RectTransform;
        }
    }

    private void ResolveCharacterViewsIfNeeded()
    {
        if (!autoBindCharacterHierarchy)
            return;

        ResolveSlideTargets();
        Transform searchRoot = charterRect != null ? charterRect : ResolvePanelRoot()?.transform;
        if (searchRoot == null)
            return;

        for (int i = 0; i < CharacterCount; i++)
        {
            if (characterViews[i] != null && characterViews[i].Root != null)
                continue;

            characterViews[i] = BuildCharacterView(searchRoot, i);
        }
    }

    private static CharacterView BuildCharacterView(Transform searchRoot, int index)
    {
        Transform root = FindChildRecursive(searchRoot, "Char" + (index + 1));
        if (root == null)
            return null;

        CharacterView view = new CharacterView
        {
            Root = root,
            NameText = FindTextByNames(root, "Name"),
            Mark1Image = FindImageByNames(root, "mark1", "Mark1"),
            Mark2Image = FindImageByNames(root, "mark2", "Mark2")
        };

        Transform activeRoot = root.Find("Active") ?? FindChildRecursive(root, "Active");
        if (activeRoot != null)
            view.ActiveCompoundIcon = FindImageByNames(activeRoot, "Icon") ?? activeRoot.GetComponent<Image>();

        Transform relicRoot = root.Find("Relic") ?? FindChildRecursive(root, "Relic");
        for (int i = 0; i < VisibleRelicSlotCount; i++)
        {
            string twoDigitName = "Relic" + (i + 1).ToString("00");
            string oneDigitName = "Relic" + (i + 1);
            Transform slotRoot = relicRoot != null
                ? FindChildRecursive(relicRoot, twoDigitName) ?? FindChildRecursive(relicRoot, oneDigitName)
                : null;

            if (slotRoot == null)
                continue;

            view.RelicIcons[i] = FindImageByNames(slotRoot, "Icon") ?? slotRoot.GetComponent<Image>();
        }

        Transform skillRoot = root.Find("Skill") ?? FindChildRecursive(root, "Skill");
        for (int i = 0; i < VisibleSkillSlotCount; i++)
        {
            string lowerName = "skill" + (i + 1);
            string upperName = "Skill" + (i + 1);
            Transform slotRoot = skillRoot != null
                ? FindChildRecursive(skillRoot, lowerName) ?? FindChildRecursive(skillRoot, upperName)
                : null;

            if (slotRoot == null)
                continue;

            view.SkillIcons[i] = FindImageByNames(slotRoot, "Icon") ?? slotRoot.GetComponent<Image>();
        }

        return view;
    }

    private GameObject ResolvePanelRoot()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        return panelRoot;
    }

    private static void SetAnchoredX(RectTransform target, float x)
    {
        if (target == null)
            return;

        Vector2 position = target.anchoredPosition;
        position.x = x;
        target.anchoredPosition = position;
    }

    private static TMP_Text FindTextByNames(Transform root, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform target = FindChildRecursive(root, names[i]);
            if (target == null)
                continue;

            TMP_Text text = target.GetComponent<TMP_Text>() ?? target.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                return text;
        }

        return null;
    }

    private static Image FindImageByNames(Transform root, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform target = FindChildRecursive(root, names[i]);
            if (target == null)
                continue;

            Image image = target.GetComponent<Image>() ?? target.GetComponentInChildren<Image>(true);
            if (image != null)
                return image;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        if (string.Equals(root.name, targetName, StringComparison.OrdinalIgnoreCase))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    [Serializable]
    private sealed class CharacterView
    {
        public Transform Root;
        public TMP_Text NameText;
        public Image Mark1Image;
        public Image Mark2Image;
        public Image ActiveCompoundIcon;
        public Image[] RelicIcons = new Image[VisibleRelicSlotCount];
        public Image[] SkillIcons = new Image[VisibleSkillSlotCount];
    }
}
