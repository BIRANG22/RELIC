using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleTurnReadyPanelUI : MonoBehaviour
{
    private const int SlotCount = 3;

    private readonly Image[] iconImages = new Image[SlotCount];
    private readonly Image[] readyFillImages = new Image[SlotCount];
    private readonly GameObject[] slotObjects = new GameObject[SlotCount];

    public static BattleTurnReadyPanelUI Ensure(BattleTurnExecutor executor)
    {
        if (executor == null || executor.EndTurnButton == null)
            return null;

        Transform parent = executor.EndTurnButton.transform.parent;
        if (parent == null)
            return null;

        Transform found = parent.Find("BattleTurnReadyPanel");
        BattleTurnReadyPanelUI panel = found != null
            ? found.GetComponent<BattleTurnReadyPanelUI>()
            : null;

        if (panel != null)
            return panel;

        GameObject panelObject = new GameObject(
            "BattleTurnReadyPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(HorizontalLayoutGroup),
            typeof(BattleTurnReadyPanelUI));
        panelObject.layer = executor.EndTurnButton.gameObject.layer;
        panelObject.transform.SetParent(parent, false);

        RectTransform buttonRect = executor.EndTurnButton.transform as RectTransform;
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();

        if (buttonRect != null)
        {
            panelRect.anchorMin = buttonRect.anchorMin;
            panelRect.anchorMax = buttonRect.anchorMax;
            panelRect.pivot = buttonRect.pivot;
            panelRect.sizeDelta = new Vector2(144f, 36f);
            panelRect.anchoredPosition =
                buttonRect.anchoredPosition + new Vector2(0f, buttonRect.rect.height + 18f);
        }
        else
        {
            panelRect.sizeDelta = new Vector2(144f, 36f);
        }

        HorizontalLayoutGroup layout = panelObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;

        panel = panelObject.GetComponent<BattleTurnReadyPanelUI>();
        panel.BuildSlots(executor.EndTurnButton.gameObject.layer);
        return panel;
    }

    public void Refresh(BattleNetworkSnapshot snapshot, ulong localSteamId)
    {
        EnsureSlots();

        for (int i = 0; i < SlotCount; i++)
        {
            BattleNetworkPartySlotSnapshot partySlot = GetPartySlot(snapshot, i);
            bool hasCharacter = partySlot != null && !string.IsNullOrWhiteSpace(partySlot.characterId);

            if (slotObjects[i] != null)
                slotObjects[i].SetActive(hasCharacter);

            if (!hasCharacter)
                continue;

            if (iconImages[i] != null)
            {
                iconImages[i].sprite = ResolveCharacterIcon(partySlot.characterId);
                iconImages[i].enabled = iconImages[i].sprite != null;
                iconImages[i].preserveAspect = true;
            }

            bool ready = IsOwnerReady(snapshot, partySlot.ownerSteamId);
            if (readyFillImages[i] != null)
                readyFillImages[i].gameObject.SetActive(ready);
        }
    }

    private void BuildSlots(int layer)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            GameObject slot = new GameObject(
                "ReadySlot_" + (i + 1),
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            slot.layer = layer;
            slot.transform.SetParent(transform, false);

            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(36f, 36f);

            LayoutElement layoutElement = slot.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 36f;
            layoutElement.preferredHeight = 36f;

            Image background = slot.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.45f);
            background.raycastTarget = false;

            GameObject icon = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            icon.layer = layer;
            icon.transform.SetParent(slot.transform, false);
            Stretch(icon.GetComponent<RectTransform>(), 5f);
            Image iconImage = icon.GetComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.color = Color.white;

            GameObject ready = new GameObject(
                "ReadyFill",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            ready.layer = layer;
            ready.transform.SetParent(slot.transform, false);
            Stretch(ready.GetComponent<RectTransform>(), 0f);
            Image readyImage = ready.GetComponent<Image>();
            readyImage.color = new Color(0.25f, 0.85f, 1f, 0.28f);
            readyImage.raycastTarget = false;

            slotObjects[i] = slot;
            iconImages[i] = iconImage;
            readyFillImages[i] = readyImage;
        }
    }

    private void EnsureSlots()
    {
        if (slotObjects[0] != null)
            return;

        BuildSlots(gameObject.layer);
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static BattleNetworkPartySlotSnapshot GetPartySlot(
        BattleNetworkSnapshot snapshot,
        int slotIndex)
    {
        if (snapshot?.partySlots == null)
            return null;

        for (int i = 0; i < snapshot.partySlots.Length; i++)
        {
            BattleNetworkPartySlotSnapshot slot = snapshot.partySlots[i];

            if (slot != null && slot.slotIndex == slotIndex)
                return slot;
        }

        return null;
    }

    private static bool IsOwnerReady(BattleNetworkSnapshot snapshot, string ownerSteamId)
    {
        if (snapshot?.readyStates == null || string.IsNullOrWhiteSpace(ownerSteamId))
            return false;

        for (int i = 0; i < snapshot.readyStates.Length; i++)
        {
            BattleNetworkMemberReadyState state = snapshot.readyStates[i];

            if (state != null && state.memberSteamId == ownerSteamId)
                return state.ready;
        }

        return false;
    }

    private static Sprite ResolveCharacterIcon(string characterId)
    {
        if (DataManager.Instance == null ||
            DataManager.Instance.CharacterIconDatabase == null ||
            string.IsNullOrWhiteSpace(characterId))
        {
            return null;
        }

        if (DataManager.Instance.CharacterIconDatabase.TryGetTimelineIcon(characterId, out Sprite timelineIcon))
            return timelineIcon;

        if (DataManager.Instance.CharacterIconDatabase.TryGetIcon(characterId, out Sprite icon))
            return icon;

        return null;
    }
}
