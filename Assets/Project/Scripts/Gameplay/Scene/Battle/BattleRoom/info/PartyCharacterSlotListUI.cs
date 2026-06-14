using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 현재 선택된 파티 캐릭터들을 슬롯 UI에 표시하는 클래스입니다.
/// Backimage는 절대 변경하지 않고,
/// Backimage 하위의 mask 하위 PortraitImage에만 캐릭터 이미지를 표시합니다.
/// </summary>
public class PartyCharacterSlotListUI : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private PartyCharacterSlotUI[] slots;

    private const string BackImageName = "Backimage";
    private const string MaskName = "mask";
    private const string PortraitImageName = "PortraitImage";
    private const string NameTextName = "Name";

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (DataManager.Instance == null)
        {
            ClearAll();
            return;
        }

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        if (partyStore == null)
        {
            ClearAll();
            return;
        }

        ClearAll();

        int displaySlotIndex = 0;

        for (int partyIndex = 0; partyIndex < slots.Length; partyIndex++)
        {
            string characterId = partyStore.GetCharacterId(partyIndex);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (displaySlotIndex >= slots.Length)
                break;

            PartyCharacterSlotUI slot = slots[displaySlotIndex];

            if (slot == null)
            {
                displaySlotIndex++;
                continue;
            }

            CharacterMasterData masterData = null;

            if (DataManager.Instance.CharacterDatabase != null)
                masterData = DataManager.Instance.CharacterDatabase.Get(characterId);

            Sprite portraitSprite = null;

            if (DataManager.Instance.CharacterIconDatabase != null)
                DataManager.Instance.CharacterIconDatabase.TryGetIcon(characterId, out portraitSprite);

            string characterName = masterData != null
                ? masterData.Name
                : characterId;

            ApplySlot(slot.transform, characterName, portraitSprite);

            displaySlotIndex++;
        }
    }

    /// <summary>
    /// 슬롯에 캐릭터 이름과 초상화 이미지를 적용합니다.
    /// Backimage는 건드리지 않고 PortraitImage만 변경합니다.
    /// </summary>
    private void ApplySlot(Transform slotTransform, string characterName, Sprite portraitSprite)
    {
        if (slotTransform == null)
            return;

        TMP_Text nameText = FindNameText(slotTransform);

        if (nameText != null)
            nameText.text = characterName;

        Image portraitImage = FindOrCreatePortraitImage(slotTransform);

        if (portraitImage == null)
            return;

        portraitImage.sprite = portraitSprite;
        portraitImage.enabled = portraitSprite != null;
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;
    }

    /// <summary>
    /// 슬롯 안에서 이름 텍스트를 찾습니다.
    /// </summary>
    private TMP_Text FindNameText(Transform slotTransform)
    {
        if (slotTransform == null)
            return null;

        Transform nameTransform = slotTransform.Find(NameTextName);

        if (nameTransform != null)
            return nameTransform.GetComponent<TMP_Text>();

        return slotTransform.GetComponentInChildren<TMP_Text>(true);
    }

    /// <summary>
    /// Backimage/mask/PortraitImage 구조에서 PortraitImage만 찾거나 생성합니다.
    /// Backimage의 Image 컴포넌트는 절대 수정하지 않습니다.
    /// mask의 Image 컴포넌트도 캐릭터 이미지로 바꾸지 않습니다.
    /// PortraitImage의 기존 크기와 위치도 유지합니다.
    /// </summary>
    private Image FindOrCreatePortraitImage(Transform slotTransform)
    {
        if (slotTransform == null)
            return null;

        Transform backImageTransform = slotTransform.Find(BackImageName);

        if (backImageTransform == null)
        {
            Debug.LogWarning($"{gameObject.name}: {slotTransform.name} 안에서 {BackImageName}을 찾지 못했습니다.");
            return null;
        }

        Transform maskTransform = backImageTransform.Find(MaskName);

        if (maskTransform == null)
        {
            GameObject maskObject = new GameObject(MaskName, typeof(RectTransform), typeof(RectMask2D));
            maskTransform = maskObject.transform;
            maskTransform.SetParent(backImageTransform, false);

            RectTransform maskRect = maskObject.GetComponent<RectTransform>();
            SetDefaultChildRect(maskRect);
        }
        else
        {
            if (maskTransform.GetComponent<Mask>() == null &&
                maskTransform.GetComponent<RectMask2D>() == null)
            {
                maskTransform.gameObject.AddComponent<RectMask2D>();
            }
        }

        Transform portraitTransform = maskTransform.Find(PortraitImageName);

        if (portraitTransform == null)
        {
            GameObject portraitObject = new GameObject(PortraitImageName, typeof(RectTransform), typeof(Image));
            portraitTransform = portraitObject.transform;
            portraitTransform.SetParent(maskTransform, false);

            RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
            SetDefaultChildRect(portraitRect);
        }

        Image portraitImage = portraitTransform.GetComponent<Image>();

        if (portraitImage == null)
            portraitImage = portraitTransform.gameObject.AddComponent<Image>();

        return portraitImage;
    }

    /// <summary>
    /// 새로 생성한 mask 또는 PortraitImage의 기본 위치만 설정합니다.
    /// 이미 존재하던 오브젝트의 크기와 위치는 건드리지 않습니다.
    /// </summary>
    private void SetDefaultChildRect(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return;

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(100f, 100f);
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 모든 슬롯의 캐릭터 이름과 PortraitImage만 비웁니다.
    /// Backimage는 절대 비우지 않습니다.
    /// </summary>
    private void ClearAll()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            ClearSlot(slots[i].transform);
        }
    }

    /// <summary>
    /// 특정 슬롯의 이름과 PortraitImage만 비웁니다.
    /// Backimage, mask의 sprite/enabled/color는 변경하지 않습니다.
    /// </summary>
    private void ClearSlot(Transform slotTransform)
    {
        if (slotTransform == null)
            return;

        TMP_Text nameText = FindNameText(slotTransform);

        if (nameText != null)
            nameText.text = string.Empty;

        Image portraitImage = FindOrCreatePortraitImage(slotTransform);

        if (portraitImage == null)
            return;

        portraitImage.sprite = null;
        portraitImage.enabled = false;
    }
}