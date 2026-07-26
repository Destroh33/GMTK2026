using UnityEngine;
using UnityEngine.UI;
using System;

public class WeaponUI : MonoBehaviour
{
    [SerializeField] WeaponController weaponController;
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

        swordIcon.color = weaponIndex == 0 ? selectedColor : unselectedColor;
        garlicIcon.color = weaponIndex == 1 ? selectedColor : unselectedColor;
        pineappleIcon.color = weaponIndex == 2 ? selectedColor : unselectedColor;
    }
}
