using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Animations.Rigging;

public class GrenadeLauncher : Weapon
{
    Player player;

    [SerializeField] private GameObject grenadePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Effects")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private Transform playerBody;

    [Header("UI")]
    [SerializeField] private List<GraphicRaycaster> uiRaycasters = new();
    [SerializeField] private EventSystem eventSystem;

    [Header("Launcher")]
    [SerializeField] private float upwardMultiplier = 0.5f;
    [SerializeField] private float forwardForce = 20f;

    [Header("Rig")]
    [SerializeField] private Transform Spher;
    [SerializeField] private Rig rig;
    [SerializeField] private float littleTimer = 0.0f;
    [SerializeField] private float littleTimerMax = 1.0f;

    private float fireCooldown;
    private float reloadTimer;
    private bool isReloading;
    private Vector3 targetPoint;
    private Vector3 shootDirection;
    private PlayerInventoryHolder playerInventoryHolder;
    WeaponUpgradeState upgradeState;

    // For PC input
    private bool isMouseOverUI = false;

    protected override void Awake()
    {
        base.Awake();
        player = GetComponentInParent<Player>();
        playerInventoryHolder = SaveManager.Singleton.playerInventoryHolder;

        if (weaponData == null)
        {
            Debug.LogError("WeaponData not assigned in Inspector.");
            return;
        }
        littleTimer = littleTimerMax;
        currentAmmo = weaponData.maxAmmo;
    }

    private void Start()
    {
        upgradeState = playerInventoryHolder.Inventory.GetUpgradeState(WeaponType);
        if (upgradeState == null) Debug.LogWarning("Upgrade state is null for weapon: " + WeaponType);
    }

    private void Update()
    {
        if (fireCooldown > 0)
            fireCooldown -= Time.deltaTime;

        if (!isReloading && currentAmmo == 0)
            Reload();

        if (isReloading)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0f)
            {
                currentAmmo = weaponData.maxAmmo;
                isReloading = false;
                if (audioSource && reloadSound)
                    audioSource.PlayOneShot(reloadSound);
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
        if (Input.GetMouseButtonDown(0) && !isMouseOverUI)
        {
            player?.SetShooting(true);
        }

        if (Input.GetMouseButton(0) && !isMouseOverUI)
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
            return;

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        targetPoint = Physics.Raycast(ray, out RaycastHit hit) ? hit.point : ray.origin + (ray.direction * 100f);
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

    // ... rest of the GrenadeLauncher class remains the same ...
    public override void Reload()
    {
        if (!isReloading && currentAmmo < weaponData.maxAmmo)
        {
            isReloading = true;
            reloadTimer = weaponData.reloadTime;

            if (audioSource && reloadSound)
                audioSource.PlayOneShot(reloadSound);
        }
    }

    Vector3 targetForAnimations;

    public override void Shoot(Vector3 targetPoint)
    {
        targetForAnimations = targetPoint;
        animator.SetBool("shoot", true);
    }

    public override void ShootFromAnimation()
    {
        base.ShootFromAnimation();
        StartCoroutine(WaitAndShootWhenIKReady());
    }

    private IEnumerator WaitAndShootWhenIKReady()
    {
        if (isReloading || currentAmmo <= 0 || fireCooldown > 0f)
            yield break;

        fireCooldown = upgradeState.GetLevel(UpgradableStatType.FireRate);
        currentAmmo--;

        GameObject grenade = Instantiate(grenadePrefab, firePoint.position, Quaternion.identity);
        Rigidbody rb = grenade.GetComponent<Rigidbody>();

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        grenade.transform.parent = null;
        rb.isKinematic = false;

        Vector3 shootDir = (targetPoint - firePoint.position).normalized;
        float upwardForce = (targetPoint - firePoint.position).magnitude * upwardMultiplier;
        Vector3 force = shootDir * forwardForce + Vector3.up * upwardForce;
        rb.AddForce(force, ForceMode.Impulse);

        if (muzzleFlash) muzzleFlash.Play();
        if (audioSource && shootSound) audioSource.PlayOneShot(shootSound);

        ShellBullet bulletScript = grenade.GetComponent<ShellBullet>();
        if (bulletScript != null)
        {
            bulletScript.SetDamage(weaponData.damage * upgradeState.GetLevel(UpgradableStatType.Damage));
        }

        animator.SetBool("shoot", false);
    }
}