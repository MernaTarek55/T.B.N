using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSwitch : MonoBehaviour
{
    [SerializeField] private GameObject[] weapons;
    [SerializeField] private PlayerInventoryHolder inventoryHolder;

    //[SerializeField] private Button deadeyeButton;
    //[SerializeField] private GameObject SwitchBTN;
    private List<WeaponType> ownedWeapons;
    private int currentWeaponIndex = 0;
    bool activated = false;

    // Add input cooldown to prevent rapid switching
    private float switchCooldown = 0.3f;
    private float lastSwitchTime = 0f;

    private void Start()
    {
        inventoryHolder = SaveManager.Singleton.playerInventoryHolder;
        ownedWeapons = inventoryHolder.Inventory.inventorySaveData.ownedWeapons;

        foreach (var owned in ownedWeapons)
        {
            Debug.Log($"WhyyyyyyOwned weapon: {owned}");
        }

        FilterOwnedWeapons();

        for (int i = 0; i < weapons.Length; i++)
        {
            if (activated) break;
            ActivateWeaponInStart(i);
        }

        //if (ownedWeapons.Count >= 2)
        //{
        //    SwitchBTN.SetActive(true);
        //}
    }

    private void Update()
    {
        // Handle right mouse click for weapon switching
        if (Input.GetMouseButtonDown(1) && Time.time - lastSwitchTime > switchCooldown)
        {
            lastSwitchTime = Time.time;
            SwitchWeapons();
        }
    }

    private void FilterOwnedWeapons()
    {
        // Deactivate weapons player doesn't own
        for (int i = 0; i < weapons.Length; i++)
        {
            var weapon = weapons[i].GetComponent<Weapon>();
            if (weapon != null && !ownedWeapons.Contains(weapon.WeaponType))
            {
                weapons[i].SetActive(false);
            }
        }
    }

    public void SwitchWeapons()
    {
        if (ownedWeapons == null || ownedWeapons.Count == 0)
        {
            Debug.LogWarning("No owned weapons to switch.");
            return;
        }

        do
        {
            currentWeaponIndex = (currentWeaponIndex + 1) % weapons.Length;
        } while (!IsWeaponOwned(currentWeaponIndex));

        ActivateWeapon(currentWeaponIndex);
    }

    private bool IsWeaponOwned(int index)
    {
        var weapon = weapons[index].GetComponent<Weapon>();
        return weapon != null && ownedWeapons.Contains(weapon.WeaponType);
    }

    private void ActivateWeapon(int index)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                bool shouldActivate = i == index && IsWeaponOwned(i);

                weapons[i].SetActive(shouldActivate);
                if (shouldActivate)
                {
                    var weapon = weapons[i].GetComponent<Weapon>();
                    //weapon.ApplyUpgrades(inventoryHolder.Inventory);
                }
            }
        }

        //if (GetCurrentWeapon().WeaponType == WeaponType.GrenadeLauncher || ownedWeapons.Count == 0)
        //{
        //    deadeyeButton.interactable = false;
        //}
        //else
        //{
        //    deadeyeButton.interactable = true;
        //}
    }

    private void ActivateWeaponInStart(int index)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                bool shouldActivate = i == index && IsWeaponOwned(i);

                if (shouldActivate)
                {
                    var weapon = weapons[i].GetComponent<Weapon>();
                    Debug.Log("Whyyyyyy" + shouldActivate);
                    weapons[i].SetActive(shouldActivate);
                    currentWeaponIndex = i;
                    activated = true;
                    return;
                }
            }
        }

        //if (GetCurrentWeapon().WeaponType == WeaponType.GrenadeLauncher || ownedWeapons.Count == 0)
        //{
        //    deadeyeButton.interactable = false;
        //}
        //else
        //{
        //    deadeyeButton.interactable = true;
        //}
    }

    public Weapon GetCurrentWeapon()
    {
        if (weapons == null || weapons.Length == 0)
        {
            Debug.LogWarning("Weapons array is null or empty.");
            return null;
        }

        if (currentWeaponIndex < 0 || currentWeaponIndex >= weapons.Length)
        {
            Debug.LogWarning("Current weapon index is out of range.");
            return null;
        }

        if (weapons[currentWeaponIndex] == null)
        {
            Debug.LogWarning("Current weapon is null.");
            return null;
        }

        return weapons[currentWeaponIndex].GetComponent<Weapon>();
    }

    public void FireBulletFromEvent()
    {
        Weapon currentWeapon = GetCurrentWeapon();
        if (currentWeapon == null) return;

        currentWeapon.ShootFromAnimation();
    }

    public void OnWeaponPurchased(WeaponType weaponType)
    {
        ownedWeapons = inventoryHolder.Inventory.inventorySaveData.ownedWeapons;
        //if (ownedWeapons.Count >= 2)
        //{
        //    SwitchBTN.SetActive(true);
        //}

        if (ownedWeapons.Count == 1)
        {
            for (int i = 0; i < weapons.Length; i++)
            {
                var weapon = weapons[i].GetComponent<Weapon>();
                if (weapon != null && weapon.WeaponType == weaponType)
                {
                    currentWeaponIndex = i;
                    ActivateWeapon(currentWeaponIndex);
                    break;
                }
            }
        }
    }
}