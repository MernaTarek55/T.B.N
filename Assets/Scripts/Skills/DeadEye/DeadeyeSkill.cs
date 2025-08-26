using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeadeyeSkill : MonoBehaviour
{
    [Header("Logic")]
    [SerializeField] private float slowMotionFactor = 0;
    [SerializeField] private WeaponSwitch currentWeapon;
    [Header("UI")]
    [SerializeField] public Transform[] targetsImages;
    [SerializeField] private Image deadEyeCooldownImage;

    // public variables
    public bool canShoot = true;

    // private IMPORTANT variables
    private List<Transform> markedTargets = new List<Transform>();
    private PlayerInventory playerInventory;
    private bool isExcutingTargets;
    private bool isUsingAbility;
    private bool doneWithLastTargets;

    // to be replaced with inventory values
    private float duration;
    private float timer;
    private float cooldownTime;
    private float lastUsedTime;

    public event Action OnDeadeyeEffectEnded;

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        playerInventory = player.GetComponent<PlayerInventoryHolder>()?.Inventory;

        cooldownTime = 1f;
        duration = 20f;
        lastUsedTime = Time.time - cooldownTime;

        // initializers 
        canShoot = true;
        doneWithLastTargets = true;
    }

    private void Update()
    {
        // Handle Q key for Dead Eye activation
        if (Input.GetKeyDown(KeyCode.Q))
        {
            UseDeadeye();
        }

        if (Time.time - lastUsedTime >= cooldownTime)
        {
            isExcutingTargets = false;
            canShoot = true;
            isUsingAbility = false;
            UpdateCooldownUI(1);
        }

        if (isUsingAbility)
        {
            canShoot = false;

            timer += Time.unscaledDeltaTime;
            UpdateCooldownUI(1 - (timer / duration));

            // Replace touch input with mouse input
            if (Input.GetMouseButtonDown(0)) // Left mouse button
            {
                Vector2 mousePos = Input.mousePosition;
                AddTapPosition(mousePos);
            }
        }
        else if (!isUsingAbility && doneWithLastTargets && !isExcutingTargets && markedTargets.Count > 0)
        {
            TerminateEnemies();
        }

        if (!isExcutingTargets && isUsingAbility)
        {
            UpdateTargetsImages();
        }
        else if (isExcutingTargets)
        {
            UpdateTargetsImages();
        }
        else
        {
            UpdateTargetsImagesforAfterDeadeye();
        }
    }

    public void UseDeadeye()
    {
        if (Time.time - lastUsedTime >= cooldownTime)
        {
            markedTargets.Clear();
            timer = 0;
            lastUsedTime = Time.time;

            Debug.Log("Deadeye skill used!");

            Time.timeScale = slowMotionFactor;

            StartCoroutine(DeadeyeEffectCoroutine());
            isUsingAbility = true;
        }
        else
        {
            isUsingAbility = false;
            Debug.Log("Deadeye skill is on cooldown.");
        }
    }

    private IEnumerator DeadeyeEffectCoroutine()
    {
        Time.timeScale = slowMotionFactor;

        yield return new WaitForSecondsRealtime(duration);

        // Reset time scale when DeadEye ends
        Time.timeScale = 1f;
        isUsingAbility = false;
        OnDeadeyeEffectEnded?.Invoke();
        Debug.Log("Deadeye effect ended.");
    }

    private void UpdateCooldownUI(float fillAmount)
    {
        if (deadEyeCooldownImage != null)
        {
            deadEyeCooldownImage.fillAmount = fillAmount;
        }
    }

    public void AddTapPosition(Vector2 screenPosition)
    {
        if (markedTargets.Count >= targetsImages.Length)
            return;

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GameObject tap = new GameObject("TargetMarker");
            tap.transform.position = hit.point;
            tap.transform.parent = hit.collider.transform;
            markedTargets.Add(tap.transform);

            GameObject tmpImage = new GameObject("TargetImage");
            tmpImage.transform.position = hit.point;
            tmpImage.transform.parent = hit.transform;
        }
        else
        {
            GameObject tap = new GameObject("TargetMarker");
            tap.transform.position = ray.origin + ray.direction * 100f;
        }
    }

    private void UpdateTargetsImages()
    {
        markedTargets.RemoveAll(marker => marker == null);

        for (int i = 0; i < targetsImages.Length; i++)
        {
            if (i < markedTargets.Count && markedTargets[i] != null)
            {
                targetsImages[i].gameObject.SetActive(true);
                targetsImages[i].position = Camera.main.WorldToScreenPoint(markedTargets[i].position);
            }
            else
            {
                targetsImages[i].gameObject.SetActive(false);
            }
        }
    }

    private void UpdateTargetsImagesforAfterDeadeye()
    {
        for (int i = 0; i < targetsImages.Length; i++)
        {
            if (i < markedTargets.Count && markedTargets[i] != null)
            {
                targetsImages[i].position = Camera.main.WorldToScreenPoint(markedTargets[i].position);
            }
        }
    }

    private void TerminateEnemies()
    {
        Weapon weapon = currentWeapon.GetCurrentWeapon();
        if (weapon == null)
        {
            return;
        }

        isExcutingTargets = true;
        StartCoroutine(ShootEnemiesSequentially(weapon));
    }

    private IEnumerator ShootEnemiesSequentially(Weapon weapon)
    {
        doneWithLastTargets = false;

        for (int i = 0; i < markedTargets.Count; i++)
        {
            if (markedTargets[i] == null) continue;

            yield return StartCoroutine(weapon.ShootForDeadEye(markedTargets[i].position));
            yield return new WaitForSeconds(1f);
            targetsImages[i].gameObject.SetActive(false);
            Debug.Log("ana hena");
        }

        doneWithLastTargets = true;
        Debug.Log("Deadeye Done");

        markedTargets.Clear();
        UpdateTargetsImages();
        isExcutingTargets = false;
    }
}