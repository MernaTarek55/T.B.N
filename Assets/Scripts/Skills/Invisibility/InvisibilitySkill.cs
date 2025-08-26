using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InvisibilitySkill : MonoBehaviour
{
    public SkinnedMeshRenderer[] renderers;
    public MeshRenderer[] Weaponrenderers;
    private Material[] materials;

    public float invisibilityDuration = 30f;
    public float cooldownDuration = 10f;

    public float fadeSpeed = 2f;
    private PlayerInventory playerInventory;

    public bool isInvisible = false;
    private bool isOnCooldown = false;

    //public Button InvisibleButton;

    //[SerializeField] private Image invisibilityImage; // Image fill UI

    // Add input cooldown to prevent rapid key presses
    private float keyPressCooldown = 0.5f;
    private float lastKeyPressTime = 0f;

    private void Awake()
    {
        List<Material> matsList = new();
        foreach (SkinnedMeshRenderer r in renderers)
        {
            matsList.AddRange(r.materials);
        }
        foreach (MeshRenderer r in Weaponrenderers)
        {
            matsList.AddRange(r.materials);
        }
        materials = matsList.ToArray();
    }

    private void Start()
    {
        var holder = SaveManager.Singleton.playerInventoryHolder;
        if (holder == null)
        {
            Debug.LogError("PlayerInventoryHolder not found on player!");
            return;
        }

        playerInventory = holder.Inventory;
        UpdateStatsFromInventory();
    }

    private void Update()
    {
        // Handle E key press for invisibility
        if (Input.GetKeyDown(KeyCode.E) && Time.time - lastKeyPressTime > keyPressCooldown)
        {
            lastKeyPressTime = Time.time;
            UseInvisibility();
        }
    }

    public void UpdateStatsFromInventory()
    {
        cooldownDuration = playerInventory.getPlayerStat(PlayerSkillsStats.InvisibilityCoolDown);
        invisibilityDuration = playerInventory.getPlayerStat(PlayerSkillsStats.InvisibilityDuration);

        Debug.Log($"Invisibility Skill Updated - Duration: {invisibilityDuration}, Cooldown: {cooldownDuration}");
    }

    public void UseInvisibility()
    {
        if (!isInvisible && !isOnCooldown)
        {
            //InvisibleButton.interactable = false;
            StartCoroutine(BecomeInvisible());
        }
    }

    private IEnumerator BecomeInvisible()
    {
        isInvisible = true;
        yield return Fade(0f, 1f);

        // Start updating the image fill during the skill duration
        yield return StartCoroutine(UpdateInvisibilityFill());
        yield return new WaitForSeconds(6);

        yield return Fade(1f, 0f);

        isInvisible = false;
        StartCoroutine(Cooldown());
    }

    private IEnumerator UpdateInvisibilityFill()
    {
        float timeRemaining = invisibilityDuration;
        //invisibilityImage.fillAmount = 1f;

        while (timeRemaining > 0f)
        {
            timeRemaining -= Time.deltaTime;
            //invisibilityImage.fillAmount = timeRemaining / invisibilityDuration;
            yield return null;
        }

        //invisibilityImage.fillAmount = 0f;
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = from;
        float direction = Mathf.Sign(to - from);

        while ((direction > 0 && t < to) || (direction < 0 && t > to))
        {
            t += direction * Time.deltaTime * fadeSpeed;
            t = Mathf.Clamp01(t);

            foreach (Material mat in materials)
            {
                mat.SetFloat("_Invisibility", t);
            }

            yield return null;
        }
    }

    public void ResetInvisibilityOnDeath()
    {
        StopAllCoroutines();
        isInvisible = false;
        isOnCooldown = false;

        // Start smooth fade back to visible
        StartCoroutine(Fade(1f, 0f));

        //if (InvisibleButton != null)
        //    InvisibleButton.interactable = true;

        //if (invisibilityImage != null)
        //    invisibilityImage.fillAmount = 1f;
    }

    private IEnumerator Cooldown()
    {
        isOnCooldown = true;

        float cooldownRemaining = cooldownDuration;

        // Start cooldown visual (image starts empty)
       // invisibilityImage.fillAmount = 0f;

        // Visual fill only (separate from the button)
        while (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;
            yield return null;
        }

        //invisibilityImage.fillAmount = 1f;
        isOnCooldown = false;

        // Enable button again
       // InvisibleButton.interactable = true;
    }
}