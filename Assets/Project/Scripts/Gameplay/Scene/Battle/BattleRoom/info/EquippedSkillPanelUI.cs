using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class EquippedSkillPanelUI : MonoBehaviour
{
    [Header("Character Rows")]
    [SerializeField] private EquippedSkillCharacterRowUI[] characterRows;

    [Header("Front Sorting")]
    [SerializeField] private bool bringToFrontOnEnable = true;
    [SerializeField] private bool forceCanvasSorting = true;
    [SerializeField] private int sortingOrder = 1000;
    [SerializeField] private bool addGraphicRaycaster = true;

    private Canvas cachedCanvas;
    private GraphicRaycaster cachedGraphicRaycaster;

    private void Awake()
    {
        ApplyFrontSorting();
    }

    private void OnEnable()
    {
        ApplyFrontSorting();
        Refresh();
    }

    public void Refresh()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[EquippedSkillPanelUI] DataManager is null.");
            return;
        }

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        CharacterRuntimeStore characterStore = DataManager.Instance.CharacterRuntimeStore;

        for (int i = 0; i < characterRows.Length; i++)
        {
            if (characterRows[i] == null)
                continue;

            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
            {
                characterRows[i].Clear();
                continue;
            }

            if (characterStore.TryGet(characterId, out CharacterRuntimeData characterData))
            {
                characterRows[i].Setup(characterData);
            }
            else
            {
                Debug.LogWarning($"[EquippedSkillPanelUI] CharacterRuntimeData ¾øÀ½: {characterId}");
                characterRows[i].Clear();
            }
        }
    }

    private void ApplyFrontSorting()
    {
        if (bringToFrontOnEnable)
            transform.SetAsLastSibling();

        if (!forceCanvasSorting)
            return;

        if (cachedCanvas == null)
        {
            cachedCanvas = GetComponent<Canvas>();
            if (cachedCanvas == null)
                cachedCanvas = gameObject.AddComponent<Canvas>();
        }

        cachedCanvas.overrideSorting = true;
        cachedCanvas.sortingOrder = sortingOrder;

        if (addGraphicRaycaster)
        {
            if (cachedGraphicRaycaster == null)
            {
                cachedGraphicRaycaster = GetComponent<GraphicRaycaster>();
                if (cachedGraphicRaycaster == null)
                    cachedGraphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            }
        }
    }
}
