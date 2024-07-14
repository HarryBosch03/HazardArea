using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Player
{
    public class InteractionUI : MonoBehaviour
    {
        public Image progress;
        public TMP_Text text;

        private FPSController player;

        private void Awake()
        {
            player = GetComponentInParent<FPSController>();
        }

        private void Update()
        {
            if (player.currentInteractable)
            {
                text.text = player.currentInteractable.getDisplayText(player);
                progress.fillAmount = player.currentInteractable.progress;
            }
            else if (player.lookingAt)
            {
                text.text = player.lookingAt.getDisplayText(player);
                progress.fillAmount = 0f;
            }
            else
            {
                text.text = string.Empty;
                progress.fillAmount = 0f;
            }
        }
    }
}