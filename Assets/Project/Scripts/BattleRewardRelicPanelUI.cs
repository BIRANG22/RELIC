using System;
using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 전투 및 이벤트에서 획득한 유물을 캐릭터에게 직접 장착하는 패널입니다.
public sealed class BattleRewardRelicPanelUI : MonoBehaviour
{
    [SerializeField] private Image rarityLine;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private TMP_Text rewardName;
    [SerializeField] private TMP_Text rewardRarity;
    [SerializeField] private TMP_Text rewardEffect;
    [SerializeField] private Transform content;
    [SerializeField] private Color commonColor = Color.white;
    [SerializeField] private Color rareColor = new Color32(81, 157, 228, 255);
    [SerializeField] private Color epicColor = new Color32(165, 107, 221, 255);
    [SerializeField] private Color uniqueColor = new Color32(233, 177, 69, 255);

    [Serializable]
    private sealed class CharacterReferences
    {
        public Transform root;
        public Image characterIcon;
        public TMP_Text characterName;
        public GameObject select;
        public Image relic01Icon;
        public Image relic02Icon;
        public Image relic03Icon;
        public Image relic04Icon;
        public Image relic05Icon;
        public Image relic06Icon;

        public Image GetRelicIcon(int index)
        {
            switch (index)
            {
                case 0: return relic01Icon;
                case 1: return relic02Icon;
                case 2: return relic03Icon;
                case 3: return relic04Icon;
                case 4: return relic05Icon;
                case 5: return relic06Icon;
                default: return null;
            }
        }
    }

    [Header("content / Char1~3 - inspector references")]
    [SerializeField] private CharacterReferences[] characters = new CharacterReferences[3];

    private readonly CharacterEntry[] entries = new CharacterEntry[3];
    private BattleRewardData current;
    private Action onResolved;
    private bool resolving;

    [Header("Equip Close Delay")]
    [SerializeField, Min(0f)] private float equipCloseDelay = 1f;
    private Coroutine equipCloseCoroutine;

    private sealed class CharacterEntry
    {
        public GameObject root;
        public string id;
        public Image icon;
        public TMP_Text name;
        public GameObject select;
        public Button button;
        public Image[] relics = new Image[6];
    }

    private void Awake()
    {
        Bind();
    }

    private void Start()
    {
        if (current == null && gameObject.activeSelf) gameObject.SetActive(false);
    }

    public static bool TryOpenRelicReward(string relicId, Action callback = null)
    {
        if (string.IsNullOrWhiteSpace(relicId) || DataManager.Instance?.RelicDatabase == null) return false;
        if (!DataManager.Instance.RelicDatabase.TryGet(relicId.Trim(), out RelicData relic) || relic == null) return false;
        BattleRewardRelicPanelUI panel = FindFirstObjectByType<BattleRewardRelicPanelUI>(FindObjectsInactive.Include);
        if (panel == null || panel.current != null) return false;
        Sprite icon = null;
        DataManager.Instance.RelicIconDatabase?.TryGetIcon(relicId.Trim(), out icon);
        panel.Open(new BattleRewardData { Type = BattleRewardType.Relic, RewardId = relicId.Trim(), Icon = icon }, callback);
        return true;
    }

    public void Open(BattleRewardData reward, Action callback)
    {
        if (reward == null || reward.Type != BattleRewardType.Relic || current != null) return;
        Bind();
        current = reward;
        onResolved = callback;
        resolving = false;
        if (equipCloseCoroutine != null)
        {
            StopCoroutine(equipCloseCoroutine);
            equipCloseCoroutine = null;
        }
        BattleUIBlurRootCollector.ConfigureForPanel(gameObject);
        gameObject.SetActive(true);
        // 활성화 후에도 새 하이어라키의 텍스트를 다시 연결하여 갱신합니다.
        Bind();
        Refresh();
        transform.SetAsLastSibling();
    }

    private static TMP_Text FindText(Transform target)
    {
        if (target == null) return null;
        TMP_Text text = target.GetComponent<TMP_Text>();
        // TMP SubMeshUI is not a primary text label. Do not bind a submesh as Name.
        return text != null ? text : target.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void Bind()
    {
        Transform item = transform.Find("item");
        rarityLine = rarityLine != null ? rarityLine : item?.Find("Background/Line2")?.GetComponent<Image>();
        rewardIcon = rewardIcon != null ? rewardIcon : item?.Find("Itemimage/Icon")?.GetComponent<Image>();
        rewardName = rewardName != null ? rewardName : FindText(item?.Find("Name"));
        rewardRarity = rewardRarity != null ? rewardRarity : FindText(item?.Find("Rarity"));
        rewardEffect = rewardEffect != null ? rewardEffect : FindText(item?.Find("Effect"));
        // Support both the old Contant hierarchy and the renamed content hierarchy.
        // 이전 프리팹에서 직렬화된 참조가 남아 있을 수 있어 현재 하이어라키를 우선합니다.
        Transform currentContent = transform.Find("content") ?? transform.Find("Contant") ?? transform.Find("Content");
        if (currentContent != null) content = currentContent;
        if (content == null) return;
        for (int i = 0; i < 3; i++)
        {
            CharacterReferences refs = characters != null && i < characters.Length ? characters[i] : null;
            Transform root = refs?.root != null ? refs.root : content.Find("Char" + (i + 1));
            if (root == null) continue;
            CharacterEntry e = entries[i] ?? new CharacterEntry();
            entries[i] = e;
            e.root = root.gameObject;
            e.icon = refs?.characterIcon != null ? refs.characterIcon : root.Find("Icon/Mask/Image")?.GetComponent<Image>();
            e.name = refs?.characterName != null ? refs.characterName : FindText(root.Find("Name"));
            e.select = refs?.select != null ? refs.select : root.Find("Select")?.gameObject;
            e.button = root.GetComponent<Button>();
            if (e.button == null) e.button = root.gameObject.AddComponent<Button>();
            e.button.transition = Selectable.Transition.None;
            int index = i;
            e.button.onClick.RemoveAllListeners();
            e.button.onClick.AddListener(() => EquipTo(index));
            RelicHoverRelay relay = root.GetComponent<RelicHoverRelay>();
            if (relay == null) relay = root.gameObject.AddComponent<RelicHoverRelay>();
            relay.Configure(e.select);
            if (e.select != null)
                foreach (Graphic graphic in e.select.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            Transform relicRoot = root.Find("Relic");
            for (int j = 0; j < 6; j++) e.relics[j] = refs?.GetRelicIcon(j) != null ? refs.GetRelicIcon(j) : relicRoot?.Find("Relic" + (j + 1).ToString("00") + "/Icon")?.GetComponent<Image>();
        }
    }

    private void Refresh()
    {
        if (current == null || DataManager.Instance == null) return;
        RelicData relic = null;
        DataManager.Instance.RelicDatabase?.TryGet(current.RewardId, out relic);
        Sprite icon = current.Icon;
        if (DataManager.Instance.RelicIconDatabase != null) DataManager.Instance.RelicIconDatabase.TryGetIcon(current.RewardId, out icon);
        SetIcon(rewardIcon, icon);
        if (rewardName != null) rewardName.text = relic != null ? GameDataLocalization.RelicName(relic) : current.RewardId;
        if (rewardRarity != null) rewardRarity.text = relic != null ? GameDataLocalization.RelicRarity(relic) : string.Empty;
        if (rewardEffect != null) rewardEffect.text = relic != null ? GameDataLocalization.RelicEffectDescription(relic) : string.Empty;
        if (rarityLine != null && relic != null)
        {
            Color color = commonColor;
            if (!RecordPanelUI.TryGetCachedRarityDisplayColor(relic.Rarity, out color))
            {
                switch (RelicRarityUtility.GetRevealRank(ParseRarity(relic.Rarity)))
                {
                    case 2: color = rareColor; break;
                    case 3: color = epicColor; break;
                    case 4: color = uniqueColor; break;
                    default: color = commonColor; break;
                }
            }
            rarityLine.color = color;
        }
        for (int i = 0; i < entries.Length; i++)
        {
            CharacterEntry e = entries[i];
            if (e == null) continue;
            e.id = DataManager.Instance.PartyRuntimeStore?.GetCharacterId(i);
            bool present = !string.IsNullOrWhiteSpace(e.id);
            e.root.SetActive(present);
            if (!present) continue;
            if (e.select != null) e.select.SetActive(false);
            CharacterMasterData master = DataManager.Instance.CharacterDatabase?.Get(e.id);
            // TMP SubMesh가 아니라 Name의 원본 TMP에 직접 이름을 기록합니다.
            Transform nameRoot = content.Find("Char" + (i + 1) + "/Name");
            e.name = FindText(nameRoot);
            string displayName = master != null ? GameDataLocalization.CharacterName(master) : null;
            if (string.IsNullOrWhiteSpace(displayName)) displayName = master != null ? master.Name : null;
            if (string.IsNullOrWhiteSpace(displayName)) displayName = e.id;
            if (e.name != null) { e.name.text = displayName; e.name.SetAllDirty(); }
            else Debug.LogWarning("[BattleRewardRelicPanelUI] Char" + (i + 1) + "/Name TMP_Text를 찾지 못했습니다.", e.root);
            Sprite portrait = null;
            DataManager.Instance.CharacterIconDatabase?.TryGetIcon(e.id, out portrait);
            SetIcon(e.icon, portrait);
            DataManager.Instance.CharacterRuntimeStore.TryGet(e.id, out CharacterRuntimeData runtime);
            int visible = 0;
            if (runtime != null)
            {
                ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);
                // 0번은 활성 유물 슬롯이므로 일반 유물 표시에서는 제외합니다.
                for (int j = 1; j < runtime.EquippedRelicIds.Length && visible < 6; j++)
                {
                    string id = runtime.EquippedRelicIds[j];
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    Sprite equippedIcon = null;
                    DataManager.Instance.RelicIconDatabase?.TryGetIcon(id, out equippedIcon);
                    SetIcon(e.relics[visible++], equippedIcon);
                }
            }
            while (visible < 6) SetIcon(e.relics[visible++], null);
        }
    }

    private void EquipTo(int index)
    {
        if (resolving || current == null || index < 0 || index >= entries.Length) return;
        CharacterEntry e = entries[index];
        if (e == null || string.IsNullOrWhiteSpace(e.id) || DataManager.Instance == null) return;
        if (!DataManager.Instance.CharacterRuntimeStore.TryGet(e.id, out CharacterRuntimeData runtime) || runtime == null) return;
        if (!DataManager.Instance.RelicDatabase.TryGet(current.RewardId, out RelicData relic) || relic == null) return;
        ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);
        bool active = ActiveRelicEffectResolver.IsActiveRelic(relic);
        int start = active ? 0 : 1;
        int end = active ? 1 : Math.Min(7, runtime.EquippedRelicIds.Length);
        int empty = -1;
        for (int i = start; i < end; i++)
            if (string.IsNullOrWhiteSpace(runtime.EquippedRelicIds[i])) { empty = i; break; }
        if (empty < 0) return; // 빈 장착 슬롯이 없으면 기존 유물을 임의 교체하지 않습니다.
        resolving = true;
        runtime.EquippedRelicIds[empty] = current.RewardId.Trim();
        if (active) ActiveRelicRuntimeUtility.ResetUses(runtime, relic);
        DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(runtime);
        Refresh(); // Show the equipped relic in the destination slot before closing.
        equipCloseCoroutine = StartCoroutine(CloseAfterEquippedPreview());
    }

    private IEnumerator CloseAfterEquippedPreview()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, equipCloseDelay));
        equipCloseCoroutine = null;
        Action callback = onResolved;
        onResolved = null;
        current = null;
        gameObject.SetActive(false);
        callback?.Invoke();
    }

    private static RelicRarity ParseRarity(string raw)
    {
        return RelicRarityUtility.TryParseChestRarity(raw, out RelicRarity value) ? value : RelicRarity.Common;
    }

    private static void SetIcon(Image target, Sprite sprite)
    {
        if (target == null) return;
        target.sprite = sprite;
        target.enabled = sprite != null;
    }
}

// 호버 효과는 장착 입력과 분리합니다.
public sealed class RelicHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject selection;
    public void Configure(GameObject target) { selection = target; if (selection != null) selection.SetActive(false); }
    public void OnPointerEnter(PointerEventData eventData) { if (selection != null) selection.SetActive(true); }
    public void OnPointerExit(PointerEventData eventData) { if (selection != null) selection.SetActive(false); }
    private void OnDisable() { if (selection != null) selection.SetActive(false); }
}
