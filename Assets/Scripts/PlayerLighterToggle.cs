using UnityEngine;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerLighterToggle : MonoBehaviour
{
    [SerializeField] private GameObject lighter;
    [SerializeField] private TMP_Text lighterMessage;

    private bool hasLighter = false;
    private bool showPickupMessage = false;
    private int pickupFrame = -1;

    private void Start()
    {
        if (lighter != null)
            lighter.SetActive(false);

        if (lighterMessage != null)
            lighterMessage.text = "";
    }

    private void Update()
    {
        bool fPressed = false;
        bool ePressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            fPressed = Keyboard.current.fKey.wasPressedThisFrame;
            ePressed = Keyboard.current.eKey.wasPressedThisFrame;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        fPressed = Input.GetKeyDown(KeyCode.F);
        ePressed = Input.GetKeyDown(KeyCode.E);
#endif

        // Press E again to dismiss the pickup message
        if (showPickupMessage && ePressed &&
            Time.frameCount != pickupFrame)
        {
            showPickupMessage = false;

            if (lighterMessage != null)
                lighterMessage.text = "";
        }

        // Press F to equip/unequip the lighter
        if (hasLighter && fPressed && lighter != null)
        {
            lighter.SetActive(!lighter.activeSelf);
        }
    }

    public void PickupLighter()
    {
        hasLighter = true;
        showPickupMessage = true;

        // Prevent the pickup E press from dismissing the message
        pickupFrame = Time.frameCount;

        if (lighterMessage != null)
        {
            lighterMessage.text =
                "Lighter acquired - Press [F] to equip/unequip\n" +
                "Press [E] to Exit";
        }
    }

    public void ShowNearbyMessage()
    {
        if (hasLighter)
            return;

        if (lighterMessage != null)
            lighterMessage.text = "[E] Pick up lighter";
    }

    public void HideNearbyMessage()
    {
        if (hasLighter)
            return;

        if (lighterMessage != null)
            lighterMessage.text = "";
    }
}
