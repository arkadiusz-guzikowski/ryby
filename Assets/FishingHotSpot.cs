using UnityEngine;

public class FishingHotSpot : MonoBehaviour
{
    [Header("Konfiguracja gorącego miejsca")]
    [SerializeField] private string playerTag = "Player";

    [Header("Komunikaty")]
    [SerializeField] private string enterMessage = "🔥 Jesteś w gorącym miejscu! Szansa na gigantycznego karpia 30-40kg!";
    [SerializeField] private string exitMessage = "Opuszczasz gorące miejsce. Szanse wracają do normy.";
    [SerializeField] private string enterColorHex = "#00FF00";
    [SerializeField] private string exitColorHex = "#FFFFFF";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            if (FishSizes.Instance != null)
            {
                FishSizes.Instance.IsInHotSpot = true;
                Debug.Log($"<color={enterColorHex}>{enterMessage}</color>");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            if (FishSizes.Instance != null)
            {
                FishSizes.Instance.IsInHotSpot = false;
                Debug.Log($"<color={exitColorHex}>{exitMessage}</color>");
            }
        }
    }
}
