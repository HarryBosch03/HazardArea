using Runtime.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

public class WeaponOverlay : MonoBehaviour
{
    public TMP_Text weaponName;
    public TMP_Text weaponAmmo;
    public Image reloadProgress;
    
    private PlayerController player;

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
    }

    private void Update()
    {
        var weapon = player.activeWeapon;
        if (weapon == null)
        {
            weaponName.text = string.Empty;
            weaponAmmo.text = string.Empty;
            reloadProgress.fillAmount = 0f;
            return;
        }

        weaponName.text = weapon.displayName;
        weaponAmmo.text = weapon.ammoCountText;
        if (weapon.isReloading)
        {
            reloadProgress.color = new Color(1f, 1f, 1f, 8f / 255f);
            reloadProgress.fillAmount = weapon.reloadPercent;
        }
        else
        {
            var color = reloadProgress.color;
            color.a = Mathf.Max(0f, color.a - Time.deltaTime * 2f * (8f / 255f));
            reloadProgress.color = color;
        }
    }
}