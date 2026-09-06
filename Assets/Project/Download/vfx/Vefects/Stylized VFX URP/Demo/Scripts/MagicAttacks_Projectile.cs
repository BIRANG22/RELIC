using System.Collections;
using System.Collections.Generic;
using UnityEngine.VFX;
using UnityEngine;

public class MagicAttacks_Projectile : MonoBehaviour
{
    private Vector3 projectileDir;
    public GameObject FX_Hit;

    VisualEffect FX_Projectile;
    VisualEffect FX_ProjectileTail;

    AudioSource SFX_Projectile;

    private void Start()
    {
        if (transform.childCount > 0)
        {
            FX_Projectile = transform.GetChild(0).GetComponent<VisualEffect>();
        }

        if (transform.childCount > 1)
        {
            FX_ProjectileTail = transform.GetChild(1).GetComponent<VisualEffect>();
        }

        SFX_Projectile = GetComponent<AudioSource>();
    }

    public void Setup(Vector3 projectileDir)
    {
        this.projectileDir = projectileDir;
    }

    private void Update()
    {
        float moveSpeed = 60f;
        transform.position += projectileDir * moveSpeed * Time.deltaTime;
        Destroy(gameObject, 5f);
    }

    private void OnTriggerEnter(Collider col)
    {
        if (FX_Hit != null)
        {
            Instantiate(FX_Hit, col.transform.position, Quaternion.identity);
        }

        if (FX_Projectile != null)
        {
            Destroy(FX_Projectile);
        }

        if (FX_ProjectileTail != null)
        {
            FX_ProjectileTail.Stop();
        }

        if (SFX_Projectile != null)
        {
            SFX_Projectile.Stop();
        }

        Destroy(gameObject, 3f);
    }
}
