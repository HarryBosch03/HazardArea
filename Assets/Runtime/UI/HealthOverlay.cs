using Runtime.Player;
using TMPro;
using UnityEngine;

public class HealthOverlay : MonoBehaviour
{
    public TMP_Text healthText;

    private HealthController health;

    private void Awake()
    {
        health = GetComponentInParent<HealthController>();
    }

    private void Update()
    {
        if (health.isAlive.Value) healthText.text = $"+{health.currentHealth.Value:0}";
        else healthText.text = "Dead";
    }
}
