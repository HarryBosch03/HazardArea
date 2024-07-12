using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Player
{
    public class HealthBar : MonoBehaviour
    {
        public TMP_Text text;
        public Image fill;

        private HealthController health;

        private void Awake()
        {
            health = GetComponentInParent<HealthController>();
        }

        private void Update()
        {
            var currentHealth = health.currentHealth.Value;
            var maxHealth = health.maxHealth.Value;
            
            if (text) text.text = $"{currentHealth:0}/{maxHealth:0}";
            if (fill) fill.fillAmount = currentHealth / maxHealth;
        }
    }
}