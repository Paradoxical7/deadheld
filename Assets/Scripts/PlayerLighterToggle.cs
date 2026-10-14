
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerLighterToggle : MonoBehaviour
{
    [SerializeField] private GameObject lighter;

    private void Start()
    {
        if (lighter != null)
            lighter.SetActive(false);
    }

    private void Update()
    {
        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        pressed = Keyboard.current != null &&
                  Keyboard.current.fKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        pressed = Input.GetKeyDown(KeyCode.F);
#endif

        if (pressed && lighter != null)
        {
            lighter.SetActive(!lighter.activeSelf);
        }
    }
}

