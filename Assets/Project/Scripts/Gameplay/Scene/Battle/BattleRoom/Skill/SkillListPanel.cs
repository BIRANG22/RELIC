using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;

public class SkillListPanel : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Content")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private SkillListSlotUI skillSlotPrefab;

    [Header("Detail")]
    [SerializeField] private GameObject detailsBackground;
    [SerializeField] private TMP_Text detailsText;

    private CharacterRuntimeData currentRuntime;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        HideSkillDetail();
        Close();
    }

    public void Open(CharacterRuntimeData runtimeData)
    {
        currentRuntime = runtimeData;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        Refresh();
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        currentRuntime = null;
        Clear();
        HideSkillDetail();
    }

    public void Refresh()
    {
        Clear();

        if (currentRuntime == null)
            return;

        if (currentRuntime.EquippedSkillIds == null)
            return;

        for (int i = 0; i < currentRuntime.EquippedSkillIds.Length; i++)
        {
            string skillId = currentRuntime.EquippedSkillIds[i];

            SkillListSlotUI slot = Instantiate(skillSlotPrefab, contentRoot);
            slot.Setup(this, skillId);
        }
    }

    public void SelectSkill(string skillId)
    {
        Debug.Log($"[SkillListPanel] Skill Selected: {skillId}");
    }

    public void ShowSkillDetail(string text)
    {
        if (detailsBackground != null)
            detailsBackground.SetActive(true);

        if (detailsText != null)
            detailsText.text = text;
    }

    public void HideSkillDetail()
    {
        if (detailsBackground != null)
            detailsBackground.SetActive(false);

        if (detailsText != null)
            detailsText.text = "";
    }

    private void Clear()
    {
        if (contentRoot == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);
    }
}