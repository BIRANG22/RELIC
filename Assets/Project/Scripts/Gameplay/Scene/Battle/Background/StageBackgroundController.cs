using System;
using System.Collections.Generic;
using UnityEngine;

public class StageBackgroundController : MonoBehaviour
{
    [Serializable]
    public sealed class BackgroundRange
    {
        [Min(1)]
        [SerializeField] private int minRow = 1;

        [Min(1)]
        [SerializeField] private int maxRow = 1;

        [SerializeField] private GameObject prefab;

        public int MinRow => minRow;
        public int MaxRow => maxRow;
        public GameObject Prefab => prefab;

        public BackgroundRange(
            int minRow,
            int maxRow,
            GameObject prefab)
        {
            this.minRow = minRow;
            this.maxRow = maxRow;
            this.prefab = prefab;
        }

        public bool Contains(int row)
        {
            return minRow <= maxRow
                   && row >= minRow
                   && row <= maxRow;
        }
    }

    [Header("배경 생성 설정")]
    [Tooltip("배경 프리팹이 생성될 부모 오브젝트")]
    [SerializeField] private Transform spawnRoot;

    [Tooltip("행 범위별로 생성할 배경 프리팹")]
    [SerializeField]
    private List<BackgroundRange> backgroundRanges = new();

    [Header("St1 Boss 연출 설정")]
    [Tooltip("보스 구간에서 생성되는 St1_boss 프리팹")]
    [SerializeField] private GameObject st1BossPrefab;

    [Tooltip("St1_boss가 생성될 때 실행할 순차 등장 연출")]
    [SerializeField]
    private TimedObjectRevealSequence timedObjectRevealSequence;

    [Tooltip("연출 스크립트 오브젝트가 꺼져 있으면 자동으로 활성화")]
    [SerializeField] private bool activateSequenceObject = true;

    private GameObject currentPrefab;
    private GameObject currentInstance;

    public IBattleRoomIntroSequence CurrentBattleRoomIntroSequence =>
        st1BossPrefab != null && currentPrefab == st1BossPrefab
            ? timedObjectRevealSequence
            : null;

    /// <summary>
    /// 현재 레이어에 해당하는 배경을 생성합니다.
    /// layerIndex는 0부터 시작하므로 행 번호는 1을 더합니다.
    /// </summary>
    public void ShowForLayer(int layerIndex)
    {
        int row = layerIndex + 1;
        BackgroundRange range = FindRange(row);

        if (range == null || range.Prefab == null)
        {
            ClearCurrentBackground();

            Debug.LogWarning(
                $"[StageBackgroundController] " +
                $"No background is configured for row {row}.",
                this
            );

            return;
        }

        // 같은 배경이 이미 생성되어 있다면 다시 생성하지 않음
        if (currentPrefab == range.Prefab &&
            currentInstance != null)
        {
            if (range.Prefab == st1BossPrefab)
                PlaySt1BossRevealSequence();

            return;
        }

        ClearCurrentBackground();

        Transform parent = spawnRoot != null
            ? spawnRoot
            : transform;

        currentInstance = Instantiate(
            range.Prefab,
            parent,
            false
        );

        currentInstance.name = range.Prefab.name;
        currentPrefab = range.Prefab;

        // St1_boss 프리팹이 생성됐을 때만 순차 등장 연출 실행
        if (range.Prefab == st1BossPrefab)
        {
            PlaySt1BossRevealSequence();
        }
    }

    /// <summary>
    /// 현재 행에 해당하는 배경 범위를 찾습니다.
    /// </summary>
    private BackgroundRange FindRange(int row)
    {
        if (backgroundRanges == null)
            return null;

        for (int i = 0; i < backgroundRanges.Count; i++)
        {
            BackgroundRange range = backgroundRanges[i];

            if (range != null && range.Contains(row))
            {
                return range;
            }
        }

        return null;
    }

    /// <summary>
    /// St1_boss 전용 순차 등장 연출을 실행합니다.
    /// </summary>
    private void PlaySt1BossRevealSequence()
    {
        if (timedObjectRevealSequence == null)
        {
            Debug.LogWarning(
                "[StageBackgroundController] " +
                "Timed Object Reveal Sequence가 지정되지 않았습니다.",
                this
            );

            return;
        }

        GameObject sequenceObject =
            timedObjectRevealSequence.gameObject;

        // 연출 스크립트가 붙은 오브젝트가 꺼져 있다면 활성화
        if (activateSequenceObject &&
            !sequenceObject.activeSelf)
        {
            sequenceObject.SetActive(true);
        }

        // 부모 오브젝트가 꺼져 있는 경우에는 실행 불가능
        if (!sequenceObject.activeInHierarchy)
        {
            Debug.LogWarning(
                "[StageBackgroundController] " +
                "Timed Object Reveal Sequence 오브젝트가 " +
                "Hierarchy에서 비활성화되어 있습니다.",
                this
            );

            return;
        }

        timedObjectRevealSequence.Play();
    }

    /// <summary>
    /// 현재 생성된 배경을 제거합니다.
    /// </summary>
    private void ClearCurrentBackground()
    {
        if (currentInstance != null)
        {
            if (Application.isPlaying)
            {
                currentInstance.SetActive(false);
                Destroy(currentInstance);
            }
            else
            {
                DestroyImmediate(currentInstance);
            }
        }

        currentInstance = null;
        currentPrefab = null;
    }
}