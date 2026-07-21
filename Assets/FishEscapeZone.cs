using UnityEngine;

/// <summary>
/// Umieść ten skrypt na obiekcie wody (z CircleCollider2D jako trigger).
/// Gdy Bobber (z rybą na haczyku) opuści water zone, ryba ucieka.
/// </summary>
public class FishEscapeZone : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showLogs = true;

    private bool hasFish = false;
    private bool fishResolved = false; // true gdy ryba złowiona lub uciekła — blokuje ponowną ucieczkę
    private Bobber currentBobber;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Spławik wchodzi do wody
        if (other.TryGetComponent<Bobber>(out Bobber bobber))
        {
            currentBobber = bobber;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Spławik wychodzi z wody
        if (other.TryGetComponent<Bobber>(out Bobber bobber))
        {
            // Jeśli ryba jest na haczyku i jeszcze nie została rozwiązana, ucieka
            if (hasFish && !fishResolved && bobber == currentBobber)
            {
                OnFishEscaped();
            }

            currentBobber = null;
        }
    }

    /// <summary>
    /// Wołane przez FishingSystem gdy ryba zostanie zaciśnięta.
    /// </summary>
    public void OnFishHooked()
    {
        hasFish = true;
        fishResolved = false;

        if (showLogs)
        {
            Debug.Log("<color=#FFA500>🎣 Ryba na haczyku! Jeśli spławik opuści wodę, ryba ucieknie.</color>");
        }
    }

    /// <summary>
    /// Wołane gdy ryba została złowiona lub uciekła.
    /// </summary>
    public void OnFishResolved()
    {
        hasFish = false;
        fishResolved = true;
    }

    private void OnFishEscaped()
    {
        if (showLogs)
        {
            Debug.Log("<color=#FF4500>⚠️ Ryba uciekła na mieliznę... MALIZNA!</color>");
        }

        // Resetujemy sprite spławika
        if (currentBobber != null)
        {
            currentBobber.ResetToBobberSprite();
        }

        // Resetujemy stan w ReelInSystem
        if (ReelInSystem.Instance != null)
        {
            ReelInSystem.Instance.ResetFish();
        }

        // Resetujemy stan w FishingSystem
        if (FishingSystem.Instance != null)
        {
            FishingSystem.Instance.OnFishLanded();
        }

        hasFish = false;
    }
}
