using UnityEngine;
using System.Collections;

public class RevolverController : MonoBehaviour
{
    [Header("Munición y Tiempos")]
    public int maxAmmo = 6;
    public int currentAmmo;
    public float reloadDuration = 2.0f;
    public bool isReloading { get; private set; } = false;

    [Header("Proyectil y Efectos")]
    public Transform firePoint;
    public GameObject bulletPrefab;
    public ParticleSystem muzzleFlash;

    [Header("Precisión")]
    public float movingSpreadAngle = 15f; 

    private Animator playerAnimator;

    void Start()
    {
        currentAmmo = maxAmmo;
        playerAnimator = GetComponentInParent<Animator>();
    }

    public bool CanShoot()
    {
        return currentAmmo > 0 && !isReloading;
    }

    public void Shoot(bool isMoving, Vector3 aimDirection)
    {
        if (!CanShoot()) return;

        currentAmmo--;

        if (muzzleFlash != null) muzzleFlash.Play();

        if (bulletPrefab != null && firePoint != null)
        {
            Quaternion baseRotation = Quaternion.LookRotation(aimDirection);

            if (isMoving)
            {
                float randomY = Random.Range(-movingSpreadAngle, movingSpreadAngle); 
                baseRotation = baseRotation * Quaternion.Euler(0, randomY, 0);
            }

            Instantiate(bulletPrefab, firePoint.position, baseRotation);
        }

        if (currentAmmo <= 0)
        {
            StartCoroutine(ReloadRoutine());
        }
    }

    public IEnumerator ReloadRoutine()
    {
        isReloading = true;
        if (playerAnimator != null) playerAnimator.SetTrigger("ReloadWeapon");

        yield return new WaitForSeconds(reloadDuration);

        currentAmmo = maxAmmo;
        isReloading = false;
    }
}