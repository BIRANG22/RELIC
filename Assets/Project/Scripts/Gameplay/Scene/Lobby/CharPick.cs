using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Relic.Gameplay.Data;

public class CharPick : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private List<CharBtn> charBtns = new List<CharBtn>();

    [Header("Party Slots")]
    [SerializeField] private List<PartySlot> partySlots = new List<PartySlot>();

    [Header("Preview")]
    [SerializeField] private Transform previewRoot;

    [Header("Setting Panel")]
    [SerializeField] private Setting setting;

    [Header("Preview Background Animation")]
    [SerializeField] private Transform previewBackground;
    [SerializeField] private float bgShrinkDuration = 0.12f;
    [SerializeField] private float bgExpandDuration = 0.12f;

    [Header("Position")]
    [SerializeField] private float spacing = 260f;

    [Header("Scale")]
    [SerializeField] private float centerScale = 1.15f;
    [SerializeField] private float sideScale = 0.9f;

    [Header("Drag")]
    [SerializeField] private float dragThreshold = 180f;

    [Header("Smooth")]
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float scaleSpeed = 12f;

    private int centerIndex = 0;

    private bool isDragging;
    private float dragStartX;
    private bool movedByDrag;

    private GameObject currentPreview;
    private string currentPreviewCharacterId;

    private Coroutine bgAnimRoutine;
    private Vector3 previewBackgroundOriginalScale;
    private bool hasPreviewBackgroundOriginalScale;

    private void Start()
    {
        CachePreviewBackgroundScale();

        for (int i = 0; i < charBtns.Count; i++)
        {
            if (charBtns[i] != null)
                charBtns[i].Init(this);
        }

        for (int i = 0; i < partySlots.Count; i++)
        {
            if (partySlots[i] != null)
                partySlots[i].Init(this, i);
        }

        RefreshInstant();

        if (charBtns.Count > 0 && charBtns[centerIndex] != null)
        {
            CharBtn centerBtn = charBtns[centerIndex];

            CreateOrUpdateRuntimeData(centerBtn);
            RefreshCenterInfo();
        }
    }

    private void Update()
    {
        RefreshSmooth();
    }

    public void ClickBtn(CharBtn btn)
    {
        if (movedByDrag)
        {
            movedByDrag = false;
            return;
        }

        int index = charBtns.IndexOf(btn);

        if (index < 0)
            return;

        if (index != centerIndex)
        {
            centerIndex = index;
            RefreshCenterInfo();
            return;
        }

        ToggleChar(btn);
    }

    public void BeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        movedByDrag = false;
        dragStartX = eventData.position.x;
    }

    public void Drag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        float dragAmount = eventData.position.x - dragStartX;

        if (Mathf.Abs(dragAmount) < dragThreshold)
            return;

        if (dragAmount < 0)
            Next();
        else
            Prev();

        movedByDrag = true;
        dragStartX = eventData.position.x;

        RefreshCenterInfo();
    }

    public void EndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    private void ToggleChar(CharBtn btn)
    {
        if (btn == null)
            return;

        if (btn.IsLocked)
        {
            Debug.Log("아직 잠긴 캐릭터입니다.");
            return;
        }

        string characterId = btn.CharacterId;

        if (string.IsNullOrWhiteSpace(characterId))
        {
            Debug.LogWarning("[CharPick] CharacterId is empty.");
            return;
        }

        CreateOrUpdateRuntimeData(btn);

        if (IsSelected(characterId))
        {
            RemoveChar(characterId);
            return;
        }

        AddChar(characterId);
    }

    private void CreateOrUpdateRuntimeData(CharBtn btn)
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[CharPick] DataManager instance is missing.");
            return;
        }

        string characterId = btn.CharacterId;

        if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out var master))
        {
            Debug.LogWarning($"[CharPick] Character master not found: {characterId}");
            return;
        }

        var runtimeStore = DataManager.Instance.CharacterRuntimeStore;

        if (runtimeStore.TryGet(characterId, out var runtime))
        {
            Debug.Log("[CharacterRuntime] Already Exists");
            return;
        }

        runtime = new CharacterRuntimeData
        {
            CharacterId = master.CharacterId,
            Level = 1,
            Exp = 0,
            CurrentHealth = master.MaxHealth,
            CurrentStamina = master.MaxStamina,
            CurrentResource = master.MaxResource,
            IsUnlocked = master.IsDefaultProvided
        };

        runtimeStore.AddOrUpdate(runtime);
        Debug.Log("[CharacterRuntime] Created");
    }

    private void AddChar(string characterId)
    {
        for (int i = 0; i < partySlots.Count; i++)
        {
            if (partySlots[i] != null && partySlots[i].IsEmpty)
            {
                partySlots[i].SetChar(characterId);
                
                if (setting != null)
                    setting.OpenCharacterSetting(characterId);

                return;
            }
        }

        Debug.Log("파티 슬롯이 가득 찼습니다.");
    }

    public void RemoveChar(string characterId)
    {
        for (int i = 0; i < partySlots.Count; i++)
        {
            if (partySlots[i] != null &&
                partySlots[i].CurrentCharacterId == characterId)
            {
                partySlots[i].Clear();

                if (setting != null)
                    setting.Clear();

                return;
            }
        }
    }

    private bool IsSelected(string characterId)
    {
        for (int i = 0; i < partySlots.Count; i++)
        {
            if (partySlots[i] != null &&
                partySlots[i].CurrentCharacterId == characterId)
                return true;
        }

        return false;
    }

    private void Next()
    {
        if (charBtns.Count <= 0)
            return;

        centerIndex++;

        if (centerIndex >= charBtns.Count)
            centerIndex = 0;
    }

    private void Prev()
    {
        if (charBtns.Count <= 0)
            return;

        centerIndex--;

        if (centerIndex < 0)
            centerIndex = charBtns.Count - 1;
    }

    private void RefreshCenterInfo()
    {
        if (charBtns.Count <= 0)
            return;

        if (centerIndex < 0 || centerIndex >= charBtns.Count)
            return;

        CharBtn centerBtn = charBtns[centerIndex];

        if (centerBtn == null)
            return;

        string characterId = centerBtn.CharacterId;

        ShowPreview(characterId);

        if (setting != null && !string.IsNullOrWhiteSpace(characterId))
            setting.OpenCharacterSetting(characterId);
    }

    private void ShowPreview(string characterId)
    {
        if (previewRoot == null)
            return;

        if (characterId == currentPreviewCharacterId)
            return;

        currentPreviewCharacterId = characterId;

        if (currentPreview != null)
            Destroy(currentPreview);

        if (string.IsNullOrWhiteSpace(characterId))
            return;

        if (DataManager.Instance == null)
            return;

        if (DataManager.Instance.CharacterPrefabDatabase == null)
            return;

        if (!DataManager.Instance.CharacterPrefabDatabase.TryGetPrefab(characterId, out var prefab))
            return;

        if (prefab == null)
            return;

        currentPreview = Instantiate(prefab, previewRoot);
        currentPreview.transform.localPosition = Vector3.zero;
        currentPreview.transform.localRotation = Quaternion.identity;
        currentPreview.transform.localScale = Vector3.one;

        PlayPreviewBackgroundAnim();
    }

    private void CachePreviewBackgroundScale()
    {
        if (previewBackground == null)
            return;

        previewBackgroundOriginalScale = previewBackground.localScale;
        hasPreviewBackgroundOriginalScale = true;
    }

    private void PlayPreviewBackgroundAnim()
    {
        if (previewBackground == null)
            return;

        if (!hasPreviewBackgroundOriginalScale)
            CachePreviewBackgroundScale();

        if (bgAnimRoutine != null)
            StopCoroutine(bgAnimRoutine);

        previewBackground.localScale = previewBackgroundOriginalScale;
        bgAnimRoutine = StartCoroutine(PreviewBackgroundAnimRoutine());
    }

    private IEnumerator PreviewBackgroundAnimRoutine()
    {
        Vector3 originalScale = previewBackgroundOriginalScale;

        float startX = originalScale.x;
        float y = originalScale.y;
        float z = originalScale.z;

        float timer = 0f;

        while (timer < bgShrinkDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / bgShrinkDuration);
            float x = Mathf.Lerp(startX, 0f, t);

            previewBackground.localScale = new Vector3(x, y, z);

            yield return null;
        }

        previewBackground.localScale = new Vector3(0f, y, z);

        timer = 0f;

        while (timer < bgExpandDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / bgExpandDuration);
            float x = Mathf.Lerp(0f, startX, t);

            previewBackground.localScale = new Vector3(x, y, z);

            yield return null;
        }

        previewBackground.localScale = originalScale;
        bgAnimRoutine = null;
    }

    private void RefreshInstant()
    {
        for (int i = 0; i < charBtns.Count; i++)
        {
            if (charBtns[i] == null)
                continue;

            int offset = GetOffset(i);

            if (offset == -1)
                ApplyInstant(charBtns[i], new Vector2(-spacing, 0f), sideScale, true, false);
            else if (offset == 0)
                ApplyInstant(charBtns[i], Vector2.zero, centerScale, true, true);
            else if (offset == 1)
                ApplyInstant(charBtns[i], new Vector2(spacing, 0f), sideScale, true, false);
            else
            {
                charBtns[i].SetVisible(false);
                charBtns[i].SetCenter(false);
            }
        }
    }

    private void RefreshSmooth()
    {
        for (int i = 0; i < charBtns.Count; i++)
        {
            if (charBtns[i] == null)
                continue;

            int offset = GetOffset(i);

            if (offset == -1)
                ApplySmooth(charBtns[i], new Vector2(-spacing, 0f), sideScale, true, false);
            else if (offset == 0)
                ApplySmooth(charBtns[i], Vector2.zero, centerScale, true, true);
            else if (offset == 1)
                ApplySmooth(charBtns[i], new Vector2(spacing, 0f), sideScale, true, false);
            else
            {
                charBtns[i].SetVisible(false);
                charBtns[i].SetCenter(false);
            }
        }
    }

    private void ApplyInstant(CharBtn btn, Vector2 pos, float scale, bool visible, bool center)
    {
        btn.SetVisible(visible);
        btn.SetCenter(center);
        btn.Rect.anchoredPosition = pos;
        btn.Rect.localScale = Vector3.one * scale;
    }

    private void ApplySmooth(CharBtn btn, Vector2 pos, float scale, bool visible, bool center)
    {
        btn.SetVisible(visible);
        btn.SetCenter(center);

        if (!visible)
            return;

        btn.Rect.anchoredPosition = Vector2.Lerp(
            btn.Rect.anchoredPosition,
            pos,
            Time.deltaTime * moveSpeed
        );

        btn.Rect.localScale = Vector3.Lerp(
            btn.Rect.localScale,
            Vector3.one * scale,
            Time.deltaTime * scaleSpeed
        );
    }

    private int GetOffset(int index)
    {
        int count = charBtns.Count;

        if (count <= 0)
            return 999;

        if (index == centerIndex)
            return 0;

        int leftIndex = centerIndex - 1;
        if (leftIndex < 0)
            leftIndex = count - 1;

        int rightIndex = centerIndex + 1;
        if (rightIndex >= count)
            rightIndex = 0;

        if (index == leftIndex)
            return -1;

        if (index == rightIndex)
            return 1;

        return 999;
    }

    public void OpenPartySetting(int partyIndex)
    {
        if (setting == null)
            return;

        setting.OpenPartySetting(partyIndex);
    }
}