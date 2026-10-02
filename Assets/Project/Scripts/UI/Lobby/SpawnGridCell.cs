using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Relic.Gameplay.Data;

public class SpawnGridCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Button button;

    [Header("Ready Panel Grid Hierarchy")]
    [Tooltip("GridXX/Select. Info_Panel에서 캐릭터가 선택되면 비어 있는 Grid에서 활성화됩니다.")]
    [SerializeField] private GameObject selectObject;
    [Tooltip("GridXX/Back. 비워두면 자동 연결합니다.")]
    [SerializeField] private Image backImage;
    [Tooltip("GridXX/Line. 비워두면 자동 연결합니다.")]
    [SerializeField] private GameObject lineObject;
    [Tooltip("GridXX/Char. 배치된 캐릭터의 Battle Idle 애니메이션을 표시합니다.")]
    [SerializeField] private Image characterImage;
    [Tooltip("Battle Idle 애니메이션의 한 프레임 유지 시간입니다.")]
    [SerializeField, Min(0.01f)] private float battleIdleFrameInterval = 0.12f;

    [Header("Colors")]
    [Tooltip("이전 SpawnGrid 색상 변경 방식을 사용할 때만 켭니다. 새 Ready_Panel에서는 Back 원래 색을 유지합니다.")]
    [SerializeField] private bool useLegacyCellColors = false;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color occupiedColor = Color.red;
    [SerializeField] private Color selectedColor = Color.blue;

    [Header("Ready Panel Occupied Back")]
    [Tooltip("캐릭터가 배치된 Grid의 Back에 유지할 색상입니다.")]
    [SerializeField] private Color occupiedBackColor = new Color32(0x3C, 0x44, 0x76, 0xFF);

    private Image cellImage;
    private SpawnGridPanel owner;
    private int gridIndex;
    private Coroutine battleIdleCoroutine;
    private string currentBattleIdleCharacterId;
    private Sprite[] currentBattleIdleFrames;
    private Color originalBackColor = Color.white;
    private bool hasOriginalBackColor;
    private bool isPointerHovering;
    private bool isGridDragging;
    private int suppressClickUntilFrame = -1;

    public int GridIndex => gridIndex;

    public void Init(SpawnGridPanel panel, int index)
    {
        owner = panel;
        gridIndex = index;

        AutoBindReadyPanelHierarchy();
        cellImage = backImage != null ? backImage : GetComponent<Image>();
        CacheOriginalBackColor();

        if (button == null)
            button = GetComponent<Button>();

        // Ready Grid는 Inspector에서 직접 연결한 Button만 사용합니다.
        // 플레이 중 AddComponent로 Inspector 대상 구조를 변경하지 않습니다.
        if (button != null)
        {
            if (button.targetGraphic == null && backImage != null)
                button.targetGraphic = backImage;

            button.onClick.RemoveListener(Execute);
            button.onClick.AddListener(Execute);
        }

        Refresh();
    }

    private void OnDisable()
    {
        StopBattleIdleAnimation();
    }

    public void Execute()
    {
        if (owner == null || Time.frameCount <= suppressClickUntilFrame)
            return;

        owner.OnClickCell(gridIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (owner == null || eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        isGridDragging = owner.BeginGridDrag(gridIndex, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isGridDragging)
            owner?.UpdateDragPreview(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isGridDragging)
            return;

        isGridDragging = false;
        suppressClickUntilFrame = Time.frameCount + 1;
        owner?.EndGridDrag(gridIndex, eventData);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (owner == null || eventData == null)
            return;

        if (owner.HandleDrop(gridIndex, eventData.pointerDrag))
            suppressClickUntilFrame = Time.frameCount + 1;
    }


    public Vector2 GetCharacterImageRectSize()
    {
        AutoBindReadyPanelHierarchy();
        if (characterImage == null)
            return new Vector2(96f, 96f);

        Rect rect = characterImage.rectTransform.rect;
        return rect.width > 0f && rect.height > 0f ? rect.size : new Vector2(96f, 96f);
    }

    public void Refresh()
    {
        int partySlotIndex = GetPartySlotIndexOnThisGrid();
        bool hasPartySlot = partySlotIndex >= 0;
        bool isSelected = owner != null && owner.IsSelectedGrid(gridIndex);

        // Info_Panel에서 캐릭터가 선택된 동안에는 캐릭터가 없는 Grid만 Select를 표시합니다.
        bool canPlaceHere = owner != null && owner.IsPlacementCandidateGrid(gridIndex);
        if (selectObject != null)
            selectObject.SetActive(canPlaceHere);

        ApplyBackVisual(hasPartySlot, isSelected, canPlaceHere);

        RefreshCharacterImage(partySlotIndex, hasPartySlot);

        // Line은 Grid의 기본 테두리이므로 런타임에서 활성/비활성 상태를 변경하지 않습니다.
        // 배치 가능 여부 표시는 Select 오브젝트만 사용합니다.
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerHovering = true;
        RefreshBackVisualOnly();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerHovering = false;
        RefreshBackVisualOnly();
    }

    private void RefreshBackVisualOnly()
    {
        int partySlotIndex = GetPartySlotIndexOnThisGrid();
        bool hasPartySlot = partySlotIndex >= 0;
        bool isSelected = owner != null && owner.IsSelectedGrid(gridIndex);
        bool canPlaceHere = owner != null && owner.IsPlacementCandidateGrid(gridIndex);
        ApplyBackVisual(hasPartySlot, isSelected, canPlaceHere);
    }

    private void ApplyBackVisual(bool hasPartySlot, bool isSelected, bool canPlaceHere)
    {
        if (cellImage == null)
            cellImage = backImage != null ? backImage : GetComponent<Image>();

        if (cellImage == null)
            return;

        if (useLegacyCellColors)
        {
            if (isSelected)
                cellImage.color = selectedColor;
            else if (hasPartySlot)
                cellImage.color = occupiedColor;
            else
                cellImage.color = normalColor;

            return;
        }

        CacheOriginalBackColor();

        // 캐릭터가 배치된 Grid는 항상 선택 색을 유지합니다.
        // 빈 Grid는 현재 배치 가능한 상태(Select 활성)일 때만 호버 색을 표시합니다.
        bool showHighlight = hasPartySlot || (canPlaceHere && isPointerHovering);
        Color target = showHighlight ? occupiedBackColor : originalBackColor;
        cellImage.color = WithPreservedAlpha(target, cellImage.color.a);
    }

    private void CacheOriginalBackColor()
    {
        if (hasOriginalBackColor)
            return;

        AutoBindReadyPanelHierarchy();
        Image target = backImage != null ? backImage : GetComponent<Image>();
        if (target == null)
            return;

        originalBackColor = target.color;
        hasOriginalBackColor = true;
    }

    private static Color WithPreservedAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private void AutoBindReadyPanelHierarchy()
    {
        if (backImage == null)
        {
            Transform back = transform.Find("Back");
            if (back != null)
                backImage = back.GetComponent<Image>();
        }

        if (selectObject == null)
        {
            Transform select = transform.Find("Select");
            if (select != null)
                selectObject = select.gameObject;
        }

        if (lineObject == null)
        {
            Transform line = transform.Find("Line");
            if (line != null)
                lineObject = line.gameObject;
        }

        if (characterImage == null)
        {
            Transform charRoot = transform.Find("Char");
            if (charRoot != null)
                characterImage = charRoot.GetComponent<Image>();

            if (characterImage == null && charRoot != null)
                characterImage = charRoot.GetComponentInChildren<Image>(true);
        }
    }

    private void RefreshCharacterImage(int partySlotIndex, bool hasPartySlot)
    {
        AutoBindReadyPanelHierarchy();

        if (characterImage == null)
            return;

        if (!hasPartySlot ||
            DataManager.Instance == null ||
            DataManager.Instance.PartyRuntimeStore == null ||
            DataManager.Instance.CharacterDatabase == null ||
            DataManager.Instance.CharacterIconDatabase == null)
        {
            ClearCharacterImage();
            return;
        }

        string characterId = DataManager.Instance.PartyRuntimeStore.GetCharacterId(partySlotIndex);
        if (string.IsNullOrWhiteSpace(characterId))
        {
            ClearCharacterImage();
            return;
        }

        characterId = characterId.Trim();

        // 실제 캐릭터 마스터 데이터가 존재하는 경우에만 Ready_Panel에 표시합니다.
        if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out var characterMaster) || characterMaster == null)
        {
            ClearCharacterImage();
            return;
        }

        if (!DataManager.Instance.CharacterIconDatabase.TryGetLobbyBattleIdleFrames(characterId, out Sprite[] frames) ||
            !HasAnySprite(frames))
        {
            ClearCharacterImage();
            return;
        }

        if (string.Equals(currentBattleIdleCharacterId, characterId, System.StringComparison.OrdinalIgnoreCase) &&
            currentBattleIdleFrames == frames &&
            battleIdleCoroutine != null)
        {
            if (!characterImage.gameObject.activeSelf)
                characterImage.gameObject.SetActive(true);

            characterImage.enabled = true;
            return;
        }

        StartBattleIdleAnimation(characterId, frames);
    }

    private void StartBattleIdleAnimation(string characterId, Sprite[] frames)
    {
        StopBattleIdleAnimation();

        currentBattleIdleCharacterId = characterId;
        currentBattleIdleFrames = frames;

        Sprite firstFrame = FindFirstValidFrame(frames);
        if (firstFrame == null)
        {
            ClearCharacterImage();
            return;
        }

        characterImage.sprite = firstFrame;
        characterImage.enabled = true;
        if (!characterImage.gameObject.activeSelf)
            characterImage.gameObject.SetActive(true);

        battleIdleCoroutine = StartCoroutine(PlayBattleIdleAnimation());
    }

    private IEnumerator PlayBattleIdleAnimation()
    {
        int frameIndex = 0;

        while (currentBattleIdleFrames != null && currentBattleIdleFrames.Length > 0)
        {
            Sprite frame = FindNextValidFrame(currentBattleIdleFrames, ref frameIndex);
            if (frame != null && characterImage != null)
            {
                characterImage.sprite = frame;
                characterImage.enabled = true;
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, battleIdleFrameInterval));
        }

        battleIdleCoroutine = null;
    }

    private void ClearCharacterImage()
    {
        StopBattleIdleAnimation();

        if (characterImage == null)
            return;

        characterImage.sprite = null;
        characterImage.enabled = false;
        if (characterImage.gameObject.activeSelf)
            characterImage.gameObject.SetActive(false);
    }

    private void StopBattleIdleAnimation()
    {
        if (battleIdleCoroutine != null)
        {
            StopCoroutine(battleIdleCoroutine);
            battleIdleCoroutine = null;
        }

        currentBattleIdleCharacterId = null;
        currentBattleIdleFrames = null;
    }

    private static bool HasAnySprite(Sprite[] frames)
    {
        return FindFirstValidFrame(frames) != null;
    }

    private static Sprite FindFirstValidFrame(Sprite[] frames)
    {
        if (frames == null)
            return null;

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
                return frames[i];
        }

        return null;
    }

    private static Sprite FindNextValidFrame(Sprite[] frames, ref int frameIndex)
    {
        if (frames == null || frames.Length == 0)
            return null;

        for (int checkedCount = 0; checkedCount < frames.Length; checkedCount++)
        {
            int index = frameIndex % frames.Length;
            frameIndex = (frameIndex + 1) % frames.Length;

            if (frames[index] != null)
                return frames[index];
        }

        return null;
    }

    private int GetPartySlotIndexOnThisGrid()
    {
        if (DataManager.Instance == null)
            return -1;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            if (partyStore.GetSpawnGridIndex(i) == gridIndex)
                return i;
        }

        return -1;
    }
}
