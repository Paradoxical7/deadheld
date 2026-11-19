using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LighterPickup : MonoBehaviour
{
    public PlayerLighterToggle playerLighter;
    public Transform player;
    public float pickupDistance = 2f;

    private void Update()
    {
        if (player == null || playerLighter == null)
            return;

        float distance = Vector3.Distance(
            player.position,
            transform.position
        );

        if (distance > pickupDistance)
        {
            playerLighter.HideNearbyMessage();
            return;
        }

        playerLighter.ShowNearbyMessage();

        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        pressed = Keyboard.current != null &&
                  Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        pressed = Input.GetKeyDown(KeyCode.E);
#endif

        if (pressed)
        {
            playerLighter.PickupLighter();
            Destroy(gameObject);
        }
    }
}
