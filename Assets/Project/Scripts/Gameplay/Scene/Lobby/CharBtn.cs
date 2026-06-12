using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Relic.Gameplay.Data;

public class CharBtn : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Character")]
    [SerializeField] private CharacterType characterType;
    [SerializeField] private string characterId;

    [Header("Lock")]
    [SerializeField] private bool isLocked;

    [Header("Option")]
    [SerializeField] private bool playClickSound = true;

    [Header("UI")]
    [SerializeField] private Image borderImg;

    private CharPick charPick;
    private RectTransform rect;
    private CanvasGroup canvasGroup;

    public CharacterType CharacterType => characterType;
    public string CharacterId => characterId;
    public RectTransform Rect => rect;
    public bool IsLocked => isLocked;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Init(CharPick pick)
    {
        charPick = pick;

        SetCenter(false);
        SetVisible(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (charPick == null)
            return;

        charPick.ClickBtn(this);
    }

    public void Execute()
    {
        if (isLocked)
        {
            Debug.Log("[CharBtn] 잠긴 캐릭터입니다.");
            return;
        }

        PlayClickSound();

        if (!SelectCharacterState())
            return;

        CreateOrUpdateRuntimeData();
        SaveCharacterToPartySlot();
    }

    private void PlayClickSound()
    {
        if (playClickSound && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SfxType.Click);
    }

    private bool SelectCharacterState()
    {
        if (CharacterSelectionState.Instance == null)
        {
            Debug.LogWarning("[CharBtn] CharacterSelectionState instance is missing.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(characterId))
        {
            Debug.LogWarning("[CharBtn] CharacterId is empty.");
            return false;
        }

        CharacterSelectionState.Instance.SelectCharacter(characterType, characterId);
        return true;
    }

    private void CreateOrUpdateRuntimeData()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[CharBtn] DataManager instance is missing.");
            return;
        }

        if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out var master))
        {
            Debug.LogWarning($"[CharBtn] Character master not found: {characterId}");
            return;
        }

        var runtimeStore = DataManager.Instance.CharacterRuntimeStore;

        if (!runtimeStore.TryGet(characterId, out var runtime))
        {
            runtime = new CharacterRuntimeData
            {
                CharacterId = master.CharacterId,
                Level = 1,
                Exp = 0,

                CurrentHealth = master.MaxHealth,
                CurrentStamina = master.MaxStamina,
                CurrentResource = master.MaxResource,
                CurrentMoveLevel = 1,

                IsUnlocked = master.IsDefaultProvided,

                MoveSkillId = "S_Move_1",
                PassiveSkillId = master.PassiveSkill1,
                UniqueSkillId = master.UniqueSkill1,
                AbilitySkillId = master.CharacterSkill1,

                EquippedSkillIds = new string[4]
                {
                master.UniqueSkill1,
                master.CharacterSkill1,
                master.CommonSkill1,
                ""
                }
            };

            runtimeStore.AddOrUpdate(runtime);
        }
    }

    private void SaveCharacterToPartySlot()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[CharBtn] DataManager instance is missing.");
            return;
        }

        var partyStore = DataManager.Instance.PartyRuntimeStore;

        int existingSlot = partyStore.FindCharacterSlot(characterId);

        // 이미 파티에 있으면 유지
        if (existingSlot >= 0)
        {
            Debug.Log(
                $"[Party] Already Exists / " +
                $"CharacterId: {characterId}, " +
                $"Slot: {existingSlot}"
            );

            return;
        }

        int emptySlot = partyStore.FindEmptySlot();

        if (emptySlot < 0)
        {
            Debug.LogWarning("[Party] 파티 슬롯이 가득 찼습니다.");
            return;
        }

        bool success = partyStore.SetCharacter(emptySlot, characterId);

        if (!success)
            return;

        if (partyStore.GetGridIndex(emptySlot) < 0)
            partyStore.SetGridIndex(emptySlot, emptySlot);

        Debug.Log(
            $"[Party] Added / " +
            $"CharacterId: {characterId}, " +
            $"Slot: {emptySlot}, " +
            $"Grid: {partyStore.GetGridIndex(emptySlot)}"
        );
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (charPick == null)
            return;

        charPick.BeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (charPick == null)
            return;

        charPick.Drag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (charPick == null)
            return;

        charPick.EndDrag(eventData);
    }

    public void SetCenter(bool isCenter)
    {
        if (borderImg == null)
            return;

        borderImg.gameObject.SetActive(isCenter);
        borderImg.enabled = isCenter;
    }

    public void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }
}