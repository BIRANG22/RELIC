using System;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine.Localization.Components;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PositionPanel/Info_Panel의 파티 캐릭터 정보를 표시합니다.
/// 새 Info_Panel 구조에 맞춰 캐릭터 아이콘, 장착 유물 2개, 장착 연성제 1개만 표시합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LobbyInfoPanelUI : MonoBehaviour
{
    private const string InfoPanelName = "Info_Panel";
    private const int CharacterCount = 3;
    private const int VisibleRelicSlotCount = 2;
    private const int VisibleCompoundSlotCount = 1;
    private const string RelicTitle = "유물";
    private const string CompoundTitle = "연성제";

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Character Data")]
    [Tooltip("Info_Panel 아래의 Char1~3 구조를 이름으로 자동 연결합니다.")]
    [SerializeField] private bool autoBindCharacterHierarchy = true;

    private readonly CharacterView[] characterViews = new CharacterView[CharacterCount];

    private void Awake()
    {
        ResolvePanelRoot();
        ResolveCharacterViewsIfNeeded();
    }

    private void OnEnable()
    {
        RefreshCharacterData();
    }

    public void RefreshCharacterData()
    {
        ResolveCharacterViewsIfNeeded();

        DataManager dataManager = DataManager.Instance;
        if (dataManager == null ||
            dataManager.PartyRuntimeStore == null ||
            dataManager.CharacterRuntimeStore == null)
        {
            ClearCharacterViews();
            return;
        }

        PartyRuntimeStore partyStore = dataManager.PartyRuntimeStore;
        CharacterRuntimeStore characterStore = dataManager.CharacterRuntimeStore;

        for (int i = 0; i < CharacterCount; i++)
        {
            CharacterView view = characterViews[i];
            if (view == null)
                continue;

            string characterId = partyStore.GetCharacterId(i);
            bool hasCharacter = !string.IsNullOrWhiteSpace(characterId);

            if (view.Root != null)
                view.Root.gameObject.SetActive(true);

            ApplyFixedTitles(view);

            if (!hasCharacter)
            {
                ClearCharacterView(view);
                continue;
            }

            CharacterRuntimeData runtime = null;
            characterStore.TryGet(characterId, out runtime);

            RefreshCharacterIcon(view, characterId);
            RefreshCharacterRelics(view, runtime);
            RefreshCharacterCompound(view, runtime);
        }
    }

    public static void RefreshAll()
    {
        LobbyInfoPanelUI[] panels = FindObjectsByType<LobbyInfoPanelUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
                panels[i].RefreshCharacterData();
        }
    }

    private void ResolveCharacterViewsIfNeeded()
    {
        if (!autoBindCharacterHierarchy)
            return;

        Transform searchRoot = ResolvePanelRoot()?.transform;
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
            Root = root
        };

        // CharN/Relic/Name/RelicText, CharN/Compound/Name/CompoundText는 고정 제목입니다.
        Transform relicTitle = FindChildRecursive(root, "RelicText");
        view.RelicTitleText = relicTitle != null ? relicTitle.GetComponent<TMP_Text>() : null;

        Transform compoundTitle = FindChildRecursive(root, "CompoundText");
        view.CompoundTitleText = compoundTitle != null ? compoundTitle.GetComponent<TMP_Text>() : null;

        ProtectFixedTitle(view.RelicTitleText, RelicTitle);
        ProtectFixedTitle(view.CompoundTitleText, CompoundTitle);

        // CharN/Icon/CharMask/Icon
        Transform outerIconRoot = FindDirectChild(root, "Icon") ?? FindChildRecursive(root, "Icon");
        Transform charMask = outerIconRoot != null
            ? FindDirectChild(outerIconRoot, "CharMask") ?? FindChildRecursive(outerIconRoot, "CharMask")
            : FindChildRecursive(root, "CharMask");
        Transform characterIcon = charMask != null
            ? FindDirectChild(charMask, "Icon") ?? FindChildRecursive(charMask, "Icon")
            : null;
        view.CharacterIconImage = characterIcon != null ? characterIcon.GetComponent<Image>() : null;

        // CharN/Relic/Relic01~02/Icon
        Transform relicRoot = FindDirectChild(root, "Relic") ?? FindChildRecursive(root, "Relic");
        for (int i = 0; i < VisibleRelicSlotCount; i++)
        {
            string slotName = "Relic" + (i + 1).ToString("00");
            Transform slotRoot = relicRoot != null
                ? FindDirectChild(relicRoot, slotName) ?? FindChildRecursive(relicRoot, slotName)
                : null;

            Transform icon = slotRoot != null
                ? FindDirectChild(slotRoot, "Icon") ?? FindChildRecursive(slotRoot, "Icon")
                : null;

            view.RelicIcons[i] = icon != null ? icon.GetComponent<Image>() : null;
        }

        // CharN/Compound/Compound01/Icon
        Transform compoundRoot = FindDirectChild(root, "Compound") ?? FindChildRecursive(root, "Compound");
        Transform compoundSlot = compoundRoot != null
            ? FindDirectChild(compoundRoot, "Compound01") ?? FindChildRecursive(compoundRoot, "Compound01")
            : null;
        Transform compoundIcon = compoundSlot != null
            ? FindDirectChild(compoundSlot, "Icon") ?? FindChildRecursive(compoundSlot, "Icon")
            : null;
        view.CompoundIcons[0] = compoundIcon != null ? compoundIcon.GetComponent<Image>() : null;

        return view;
    }

    private static void ApplyFixedTitles(CharacterView view)
    {
        if (view == null)
            return;

        ProtectFixedTitle(view.RelicTitleText, RelicTitle);
        ProtectFixedTitle(view.CompoundTitleText, CompoundTitle);
    }

    private static void ProtectFixedTitle(TMP_Text text, string fixedText)
    {
        if (text == null)
            return;

        GameObject target = text.gameObject;

        Transform titleRoot = text.transform.parent;
        if (titleRoot != null && !titleRoot.gameObject.activeSelf)
            titleRoot.gameObject.SetActive(true);

        if (!target.activeSelf)
            target.SetActive(true);

        if (target.GetComponent<LocalizationIgnore>() == null)
            target.AddComponent<LocalizationIgnore>();

        LocalizedTMPText localizedTmp = target.GetComponent<LocalizedTMPText>();
        if (localizedTmp != null)
            localizedTmp.enabled = false;

        LocalizeStringEvent legacyLocalizer = target.GetComponent<LocalizeStringEvent>();
        if (legacyLocalizer != null)
            legacyLocalizer.enabled = false;

        text.text = fixedText;
    }

    private static void RefreshCharacterIcon(CharacterView view, string characterId)
    {
        Sprite icon = null;
        CharacterIconDatabase iconDatabase = DataManager.Instance?.CharacterIconDatabase;
        if (iconDatabase != null && !string.IsNullOrWhiteSpace(characterId))
            iconDatabase.TryGetIcon(characterId, out icon);

        ApplyImage(view.CharacterIconImage, icon);
    }

    private static void RefreshCharacterRelics(CharacterView view, CharacterRuntimeData runtime)
    {
        if (runtime != null)
            ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);

        for (int i = 0; i < VisibleRelicSlotCount; i++)
        {
            // 0번은 연성제 슬롯이므로 일반 유물은 1, 2번 런타임 슬롯을 사용합니다.
            int runtimeRelicIndex = i + 1;
            string relicId = runtime?.EquippedRelicIds != null &&
                             runtimeRelicIndex < runtime.EquippedRelicIds.Length
                ? runtime.EquippedRelicIds[runtimeRelicIndex]
                : null;

            ApplyImage(view.RelicIcons[i], ResolveRelicIcon(relicId));
        }
    }

    private static void RefreshCharacterCompound(CharacterView view, CharacterRuntimeData runtime)
    {
        string compoundId = ActiveRelicRuntimeUtility.GetActiveRelicId(runtime);
        ApplyImage(view.CompoundIcons[0], ResolveRelicIcon(compoundId));
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
            if (view?.Root != null)
                view.Root.gameObject.SetActive(true);

            ApplyFixedTitles(view);
            ClearCharacterView(view);
        }
    }

    private static void ClearCharacterView(CharacterView view)
    {
        if (view == null)
            return;

        ApplyImage(view.CharacterIconImage, null);

        for (int i = 0; i < view.RelicIcons.Length; i++)
            ApplyImage(view.RelicIcons[i], null);

        for (int i = 0; i < view.CompoundIcons.Length; i++)
            ApplyImage(view.CompoundIcons[i], null);
    }

    private static void ApplyImage(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.color = Color.white;
        image.enabled = sprite != null;
    }

    private GameObject ResolvePanelRoot()
    {
        if (panelRoot != null && panelRoot.name == InfoPanelName)
            return panelRoot;

        if (gameObject.name == InfoPanelName)
        {
            panelRoot = gameObject;
            return panelRoot;
        }

        GameObject found = FindSceneObject(InfoPanelName);
        if (found != null)
            panelRoot = found;
        else if (panelRoot == null)
            panelRoot = gameObject;

        return panelRoot;
    }

    private static GameObject FindSceneObject(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return null;

        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, targetName);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                return child;
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
        public TMP_Text RelicTitleText;
        public TMP_Text CompoundTitleText;
        public Image CharacterIconImage;
        public Image[] RelicIcons = new Image[VisibleRelicSlotCount];
        public Image[] CompoundIcons = new Image[VisibleCompoundSlotCount];
    }
}
