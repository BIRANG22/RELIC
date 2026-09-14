using System;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PositionPanel/Info_Panel의 파티 캐릭터 정보를 표시합니다.
/// Info_Panel의 활성/비활성은 LobbyPositionSharedModalBackground가 BackgroundPanel과 함께 관리합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LobbyInfoPanelUI : MonoBehaviour
{
    private const string InfoPanelName = "Info_Panel";
    private const int CharacterCount = 3;
    private const int VisibleRelicSlotCount = 6;
    private const int VisibleSkillSlotCount = 3;
    private static readonly int[] RuntimeSkillSlotIndices = { 1, 2, 3 };

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
        if (dataManager == null || dataManager.PartyRuntimeStore == null || dataManager.CharacterRuntimeStore == null)
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
            Root = root,
            NameText = FindTextByNames(root, "Name"),
            Mark1Image = FindImageByNames(root, "mark1", "Mark1"),
            Mark2Image = FindImageByNames(root, "mark2", "Mark2")
        };

        Transform activeRoot = root.Find("Active") ?? FindChildRecursive(root, "Active");
        if (activeRoot != null)
            view.ActiveCompoundIcon = FindImageByNames(activeRoot, "Icon");

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

            view.RelicSlots[i] = new RelicSlotView
            {
                NumberText = FindTextByNames(slotRoot, "Number"),
                IconImage = FindImageByNames(slotRoot, "Icon")
            };
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

    private static void RefreshCharacterIdentity(CharacterView view, string characterId, CharacterMasterData master)
    {
        if (view.NameText != null)
        {
            string displayName = master != null ? GameDataLocalization.CharacterName(master) : characterId;
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

    private static void RefreshCharacterActiveCompound(CharacterView view, CharacterRuntimeData runtime)
    {
        string compoundId = ActiveRelicRuntimeUtility.GetActiveRelicId(runtime);
        ApplyImage(view.ActiveCompoundIcon, ResolveRelicIcon(compoundId));
    }

    private static void RefreshCharacterRelics(CharacterView view, CharacterRuntimeData runtime)
    {
        if (runtime != null)
            ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);

        for (int i = 0; i < VisibleRelicSlotCount; i++)
        {
            int runtimeRelicIndex = i + 1;
            string relicId = runtime?.EquippedRelicIds != null && runtimeRelicIndex < runtime.EquippedRelicIds.Length
                ? runtime.EquippedRelicIds[runtimeRelicIndex]
                : null;

            RelicSlotView slotView = view.RelicSlots[i];
            if (slotView == null)
                continue;

            bool hasEquippedRelic = !string.IsNullOrWhiteSpace(relicId);
            ApplyImage(slotView.IconImage, ResolveRelicIcon(relicId));

            if (slotView.NumberText != null)
                slotView.NumberText.gameObject.SetActive(!hasEquippedRelic);
        }
    }

    private static void RefreshCharacterSkills(CharacterView view, CharacterRuntimeData runtime)
    {
        for (int i = 0; i < VisibleSkillSlotCount; i++)
        {
            int runtimeIndex = RuntimeSkillSlotIndices[i];
            string skillId = GetEquippedSkillId(runtime, runtimeIndex);

            Sprite icon = null;
            if (!string.IsNullOrWhiteSpace(skillId) && DataManager.Instance?.SkillIconDatabase != null)
                DataManager.Instance.SkillIconDatabase.TryGetIcon(skillId, out icon);

            ApplyImage(view.SkillIcons[i], icon);
            SkillUpgradeMarkStyle.ApplyShared(view.SkillIcons[i], skillId);
        }
    }

    private static string GetEquippedSkillId(CharacterRuntimeData runtime, int runtimeIndex)
    {
        if (runtime == null)
            return null;

        if (runtimeIndex == 1 && !string.IsNullOrWhiteSpace(runtime.AbilitySkillId))
            return runtime.AbilitySkillId;

        if (runtime.EquippedSkillIds == null || runtimeIndex < 0 || runtimeIndex >= runtime.EquippedSkillIds.Length)
            return null;

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
            ClearCharacterView(characterViews[i]);
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

        for (int i = 0; i < view.RelicSlots.Length; i++)
        {
            RelicSlotView slotView = view.RelicSlots[i];
            if (slotView == null)
                continue;

            ApplyImage(slotView.IconImage, null);
            if (slotView.NumberText != null)
                slotView.NumberText.gameObject.SetActive(true);
        }

        for (int i = 0; i < view.SkillIcons.Length; i++)
        {
            ApplyImage(view.SkillIcons[i], null);
            SkillUpgradeMarkStyle.ApplyShared(view.SkillIcons[i], (string)null);
        }
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

    private static TMP_Text FindTextByNames(Transform root, params string[] names)
    {
        if (root == null)
            return null;

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
        if (root == null)
            return null;

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
    private sealed class RelicSlotView
    {
        public TMP_Text NumberText;
        public Image IconImage;
    }

    [Serializable]
    private sealed class CharacterView
    {
        public Transform Root;
        public TMP_Text NameText;
        public Image Mark1Image;
        public Image Mark2Image;
        public Image ActiveCompoundIcon;
        public RelicSlotView[] RelicSlots = new RelicSlotView[VisibleRelicSlotCount];
        public Image[] SkillIcons = new Image[VisibleSkillSlotCount];
    }
}
