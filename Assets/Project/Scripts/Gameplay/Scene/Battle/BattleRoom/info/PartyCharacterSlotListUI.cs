using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 현재 선택된 파티 캐릭터들을 배틀 인벤토리 패널의 슬롯 UI에 표시합니다.
/// 슬롯 하위의 PortraitImage 오브젝트에 CharacterIconDatabase의 Mark 이미지를 표시합니다.
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

            Sprite markSprite = null;

            if (DataManager.Instance.CharacterIconDatabase != null)
                DataManager.Instance.CharacterIconDatabase.TryGetMark(characterId, out markSprite);

            string characterName = masterData != null
                ? masterData.Name
                : characterId;

            ApplySlot(slot.transform, characterName, markSprite);

            displaySlotIndex++;
        }
    }

    /// <summary>
    /// 슬롯에 캐릭터 이름과 캐릭터 마크 이미지를 적용합니다.
    /// </summary>
    private void ApplySlot(Transform slotTransform, string characterName, Sprite markSprite)
    {
        if (slotTransform == null)
            return;

        TMP_Text nameText = FindNameText(slotTransform);

        if (nameText != null)
            nameText.text = characterName;

        Image portraitImage = FindPortraitImage(slotTransform);

        if (portraitImage == null)
        {
            Debug.LogWarning($"{gameObject.name}: {slotTransform.name} 안에서 {PortraitImageName} 오브젝트를 찾지 못했습니다.");
            return;
        }

        portraitImage.sprite = markSprite;
        portraitImage.enabled = markSprite != null;
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
    /// 현재 구조인 Slot_01/PortraitImage를 우선으로 찾습니다.
    /// 이전 구조인 Backimage/mask/PortraitImage도 호환되도록 같이 찾습니다.
    /// </summary>
    private Image FindPortraitImage(Transform slotTransform)
    {
        if (slotTransform == null)
            return null;

        Transform directPortraitTransform = slotTransform.Find(PortraitImageName);

        if (directPortraitTransform != null)
        {
            Image directPortraitImage = directPortraitTransform.GetComponent<Image>();

            if (directPortraitImage != null)
                return directPortraitImage;
        }

        Image[] childImages = slotTransform.GetComponentsInChildren<Image>(true);

        for (int i = 0; i < childImages.Length; i++)
        {
            Image childImage = childImages[i];

            if (childImage == null)
                continue;

            if (childImage.gameObject.name == PortraitImageName)
                return childImage;
        }

        Transform backImageTransform = slotTransform.Find(BackImageName);

        if (backImageTransform != null)
        {
            Transform maskTransform = backImageTransform.Find(MaskName);

            if (maskTransform != null)
            {
                Transform oldPortraitTransform = maskTransform.Find(PortraitImageName);

                if (oldPortraitTransform != null)
                {
                    Image oldPortraitImage = oldPortraitTransform.GetComponent<Image>();

                    if (oldPortraitImage != null)
                        return oldPortraitImage;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 모든 슬롯의 캐릭터 이름과 PortraitImage만 비웁니다.
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
    /// </summary>
    private void ClearSlot(Transform slotTransform)
    {
        if (slotTransform == null)
            return;

        TMP_Text nameText = FindNameText(slotTransform);

        if (nameText != null)
            nameText.text = string.Empty;

        Image portraitImage = FindPortraitImage(slotTransform);

        if (portraitImage == null)
            return;

        portraitImage.sprite = null;
        portraitImage.enabled = false;
    }
}
