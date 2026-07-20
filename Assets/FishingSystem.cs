using UnityEngine;
using UnityEngine.InputSystem;

public class FishingSystem : MonoBehaviour
{
    enum FishingState { Idle, Waiting, Biting }
    private FishingState state = FishingState.Idle;
    private float timer = 0f;
    private float waitTime = 0f;

    void Update()
    {
        switch (state)
        {
            case FishingState.Idle:
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    Debug.Log("Zaczynasz łowić! Czekaj na branie...");
                    state = FishingState.Waiting;
                    timer = 0f;
                    waitTime = Random.Range(2f, 5f);
                }
                break;

            case FishingState.Waiting:
                timer += Time.deltaTime;
                if (timer >= waitTime)
                {
                    Debug.Log("BRANIE! Naciśnij SPACJĘ!");
                    state = FishingState.Biting;
                }
                break;

            case FishingState.Biting:
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    float fishWeight = 0f;

                    if (FishSizes.Instance != null)
                        fishWeight = FishSizes.Instance.GetRandomCarpWeight();
                    else
                        fishWeight = Random.Range(1f, 40f);

                        if (FishWeightDisplay.Instance != null)
                            FishWeightDisplay.Instance.OnFishCaught(fishWeight);
                    else
                        Debug.Log($"Złowiłeś karpia! Waga: {fishWeight}kg");

                    state = FishingState.Idle;
                }
                break;
        }
    }
}

