using UnityEngine;
using UnityEngine.UI;
using System;

public class WeaponUI : MonoBehaviour
{
    [SerializeField] WeaponController weaponController;
    [SerializeField] Image weaponImage;

    [Header("Index 0 = Sword, 1 = Garlic, 2 = Pineapple")]
    [SerializeField] private Sprite[] weaponSprites;

    private void OnEnable()
    {
        weaponController.OnWeaponChanged += UpdateWeaponUI;

        UpdateWeaponUI(weaponController.ActiveIndex);
    }

    private void OnDisable()
    {
        weaponController.OnWeaponChanged -= UpdateWeaponUI;
    }

    private void UpdateWeaponUI(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= weaponSprites.Length) { return; }

        weaponImage.sprite = weaponSprites[weaponIndex];
    }
}
