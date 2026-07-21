using UnityEngine;

/// <summary>
/// Sprawdza czy dany punkt znajduje się w water zone (obszarze wody).
/// Umieść ten skrypt na obiekcie wody (tym samym co FishEscapeZone z CircleCollider2D).
/// PlayerMovement używa go do blokowania rzutów poza wodę.
/// </summary>
public class WaterZoneChecker : MonoBehaviour
{
    private CircleCollider2D waterCollider;

    void Awake()
    {
        waterCollider = GetComponent<CircleCollider2D>();
        if (waterCollider == null)
        {
            Debug.LogError("WaterZoneChecker wymaga CircleCollider2D na tym samym obiekcie!");
        }
    }

    /// <summary>
    /// Sprawdza czy punkt znajduje się w water zone.
    /// </summary>
    public bool IsPointInWater(Vector3 point)
    {
        if (waterCollider == null) return false;

        // Sprawdzamy czy punkt jest wewnątrz collidera
        return waterCollider.OverlapPoint(point);
    }
}
