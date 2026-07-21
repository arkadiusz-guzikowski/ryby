using UnityEngine;

public class FishingHotSpot : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (FishSizes.Instance != null)
            {
                FishSizes.Instance.IsInHotSpot = true;
                Debug.Log("<color=#00FF00>🔥 Jesteś w gorącym miejscu! Szansa na gigantycznego karpia 30-40kg!</color>");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (FishSizes.Instance != null)
            {
                FishSizes.Instance.IsInHotSpot = false;
                Debug.Log("Opuszczasz gorące miejsce. Szanse wracają do normy.");
            }
        }
    }
}
