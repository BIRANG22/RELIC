using System.Collections;
using UnityEngine;

public sealed class BattleWorldVfxHandle : MonoBehaviour
{
    private Transform followTarget;
    private Vector3 followWorldOffset;
    private Renderer proxyRenderer;
    private int sortingOrderOffset;
    private float yMultiplier;
    private GameObject renderGroup;
    private RenderTexture renderTexture;
    private Material runtimeMaterial;
    private bool cleanedUp;

    public void Initialize(
        Transform followTarget,
        Vector3 followWorldOffset,
        Renderer proxyRenderer,
        int sortingOrderOffset,
        float yMultiplier,
        GameObject renderGroup,
        RenderTexture renderTexture,
        Material runtimeMaterial)
    {
        this.followTarget = followTarget;
        this.followWorldOffset = followWorldOffset;
        this.proxyRenderer = proxyRenderer;
        this.sortingOrderOffset = sortingOrderOffset;
        this.yMultiplier = yMultiplier;
        this.renderGroup = renderGroup;
        this.renderTexture = renderTexture;
        this.runtimeMaterial = runtimeMaterial;

        RefreshTransformAndSorting();
    }

    public void SetWorldPosition(Vector3 position)
    {
        followTarget = null;
        transform.position = position + followWorldOffset;
        RefreshSorting();
    }

    public IEnumerator DestroyAfter(float lifeTime)
    {
        if (lifeTime > 0f)
            yield return new WaitForSeconds(lifeTime);
        else
            yield return null;

        if (this != null)
            Destroy(gameObject);
    }

    private void LateUpdate()
    {
        RefreshTransformAndSorting();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void RefreshTransformAndSorting()
    {
        if (followTarget != null)
            transform.position = followTarget.position + followWorldOffset;

        RefreshSorting();
    }

    private void RefreshSorting()
    {
        if (proxyRenderer == null)
            return;

        proxyRenderer.sortingOrder = BattleWorldVfxSortUtility.CalculateSortingOrder(
            transform.position.y,
            yMultiplier,
            sortingOrderOffset);
    }

    private void Cleanup()
    {
        if (cleanedUp)
            return;

        cleanedUp = true;

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }

        if (renderGroup != null)
        {
            Destroy(renderGroup);
            renderGroup = null;
        }
    }
}
