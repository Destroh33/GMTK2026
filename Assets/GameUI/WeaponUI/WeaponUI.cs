using UnityEngine;
using UnityEngine.UI;
using System;

public class WeaponUI : MonoBehaviour
{
    [SerializeField] Image weaponImage;

    [Header("Index 0 = Sword, 1 = Garlic, 2 = Pineapple")]
    [SerializeField] private Sprite[] weaponSprites;

    [Header("Weapon Icons")]
    [SerializeField] private Image swordIcon;
    [SerializeField] private Image garlicIcon;
    [SerializeField] private Image pineappleIcon;

    [Header("Colors")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color unselectedColor = new Color(1f, 1f, 1f, 0.5f);

    WeaponController weaponController;

    private void OnEnable()
    {
        weaponController = FindAnyObjectByType<WeaponController>();
        if (weaponController == null) return;

        weaponController.OnWeaponChanged += UpdateWeaponUI;
        UpdateWeaponUI(weaponController.ActiveIndex);
    }

    private void OnDisable()
    {
        if (weaponController != null)
            weaponController.OnWeaponChanged -= UpdateWeaponUI;
        weaponController = null;
    }

    private void UpdateWeaponUI(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= weaponSprites.Length) { return; }

        weaponImage.sprite = weaponSprites[weaponIndex];

        swordIcon.color = weaponIndex == 0 ? selectedColor : unselectedColor;
        garlicIcon.color = weaponIndex == 1 ? selectedColor : unselectedColor;
        pineappleIcon.color = weaponIndex == 2 ? selectedColor : unselectedColor;
    }
}