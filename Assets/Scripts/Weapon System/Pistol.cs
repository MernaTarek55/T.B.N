using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Pistol : Weapon
{
    Player player;

    [SerializeField] private DeadeyeSkill deadEye;
    [SerializeField] private bool deadEyeBool = false;

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Effects")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Rig")]
    [SerializeField] private Transform Spher;
    [SerializeField] private Rig rig;
    [SerializeField] private float littleTimer = 0.0f;
    [SerializeField] private float littleTimerMax = 1.0f;

    private float reloadTimer;
    private float fireCooldown;
    private bool isReloading;
    private Vector3 shootDirection;

    [SerializeField] private Transform playerBody;
    [Header("UI")]
    [SerializeField] private List<GraphicRaycaster> uiRaycasters = new();
    [SerializeField] private EventSystem eventSystem;

    // For PC input
    private bool isMouseOverUI = false;

    protected override void Awake()
    {
        base.Awake();

        if (weaponData == null)
        {
            Debug.LogError("WeaponData not assigned in Inspector.");
            return;
        }
        currentAmmo = weaponData.maxAmmo;
        littleTimer = littleTimerMax;
    }

    private void Update()
    {
        if (fireCooldown > 0)
        {
            fireCooldown -= Time.unscaledDeltaTime;
        }

        if (!isReloading && currentAmmo == 0)
        {
            Reload();
        }

        if (isReloading)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0f)
            {
                currentAmmo = weaponData.maxAmmo;
                isReloading = false;

                if (audioSource && reloadSound)
                {
                    audioSource.PlayOneShot(reloadSound);
                }
            }
        }

        // Check for mouse input on PC
        CheckMouseInput();

        if (littleTimer > 0)
        {
            littleTimer -= Time.deltaTime;
        }
        else
        {
            rig.weight = 0;
        }
    }

    private void CheckMouseInput()
    {
        // Check if mouse is over UI
        isMouseOverUI = IsMouseOverUI();

        // Handle left mouse button for shooting
        if (Input.GetMouseButtonDown(0) && !isMouseOverUI && deadEye.canShoot)
        {
            player?.SetShooting(true);
        }

        if (Input.GetMouseButton(0) && !isMouseOverUI && deadEye.canShoot)
        {
            ShootAtMousePosition(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            player?.SetShooting(false);
        }
    }

    private void ShootAtMousePosition(Vector2 screenPosition)
    {
        if (isReloading || currentAmmo <= 0 || fireCooldown > 0f)
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);

        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit) ? hit.point : ray.origin + (ray.direction * 100f);
        Spher.position = targetPoint;
        rig.weight = 1;
        littleTimer = littleTimerMax;

        Vector3 lookDirection = targetPoint - playerBody.position;
        lookDirection.y = 0f;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        playerBody.rotation = targetRotation;

        Shoot(targetPoint);
    }

    private bool IsMouseOverUI()
    {
        PointerEventData eventData = new PointerEventData(eventSystem);
        eventData.position = Input.mousePosition;

        foreach (var raycaster in uiRaycasters)
        {
            List<RaycastResult> results = new();
            raycaster.Raycast(eventData, results);
            if (results.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    // ... rest of the Pistol class remains the same ...
    public override void ShootFromAnimation()
    {
        base.ShootFromAnimation();
        StartCoroutine(WaitAndShootWhenIKReady(targetForAnimations));
    }

    Vector3 targetForAnimations;

    public override void Shoot(Vector3 targetPoint)
    {
        targetForAnimations = targetPoint;
        animator.SetBool("shoot", true);
    }

    public void waitAndShoot()
    {
        StartCoroutine(WaitAndShootWhenIKReady(targetForAnimations));
    }

    private IEnumerator WaitAndShootWhenIKReady(Vector3 targetPoint)
    {
        shootDirection = (targetPoint - firePoint.position).normalized;
        firePoint.rotation = Quaternion.LookRotation(shootDirection);

        if (isReloading && fireCooldown > 0f)
        {
            yield break;
        }

        if (!deadEyeBool)
        {
            fireCooldown = weaponData.fireRate;
        }

        currentAmmo--;

        if (WeaponType == WeaponType.Auto)
        {
            GameObject Laser = PoolManager.Instance.GetPrefabByTag(PoolType.Laser);
            Laser.GetComponent<Laser>().InitializeLaser(firePoint.transform.position, firePoint.transform.rotation, true);
            Laser.GetComponent<Laser>().SetLaserDamage(weaponData.damage);
            AudioManager.Instance.PlaySound(SoundType.Laser);

            Rigidbody rb = Laser.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(Laser.transform.forward * weaponData.bulletForce, ForceMode.Impulse);
        }
        else
        {
            GameObject bullet = PoolManager.Instance.GetPrefabByTag(PoolType.Bullet);
            bullet.transform.position = firePoint.position;
            bullet.transform.rotation = Quaternion.LookRotation(shootDirection);
            bullet.SetActive(true);
            AudioManager.Instance.PlaySound(SoundType.Gun);

            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(bullet.transform.forward * weaponData.bulletForce, ForceMode.Impulse);

            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.SetDamage(5);
            }
        }

        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        if (audioSource && shootSound)
        {
            audioSource.PlayOneShot(shootSound);
        }

        animator.SetBool("shoot", false);
    }

    public override IEnumerator ShootForDeadEye(Vector3 targetPosition)
    {
        if (WeaponType == WeaponType.Auto)
        {
            shootDirection = (targetPosition - firePoint.position).normalized;
            firePoint.rotation = Quaternion.LookRotation(shootDirection);
            deadEyeBool = true;
            Shoot(targetPosition);
        }
        else
        {
            deadEyeBool = true;
            Shoot(targetPosition);
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    public override void Reload()
    {
        if (!isReloading && currentAmmo < weaponData.maxAmmo)
        {
            isReloading = true;
            reloadTimer = weaponData.reloadTime;

            if (audioSource && reloadSound)
            {
                audioSource.PlayOneShot(reloadSound);
            }
        }
    }
}