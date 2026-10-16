using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;

public class KeypadStation : MonoBehaviour
{
    [SerializeField] private GameObject prompt;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text display;
    [SerializeField] private TMP_Text message;
    [SerializeField] private UnityEvent onSolved;

    private const int CodeLength = 4;

    private bool playerInside;
    private bool open;
    private bool solved;
    private string entered = "";
    private TankController player;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;
        player = other.GetComponentInParent<TankController>();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }

    private void OnDisable()
    {
        if (Keyboard.current != null) Keyboard.current.onTextInput -= OnTextInput;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (prompt != null) prompt.SetActive(playerInside && !open && !solved);

        if (!open)
        {
            if (playerInside && !solved && kb.eKey.wasPressedThisFrame) Open();
            return;
        }

        if (solved) return;

        if (kb.escapeKey.wasPressedThisFrame) Close();
        else if (kb.backspaceKey.wasPressedThisFrame) Backspace();
        else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) Submit();
    }

    private void Open()
    {
        open = true;
        entered = "";
        message.text = "";
        Refresh();
        panel.SetActive(true);
        LockPlayer(true);
        Keyboard.current.onTextInput += OnTextInput;
    }

    private void Close()
    {
        open = false;
        panel.SetActive(false);
        LockPlayer(false);
        Keyboard.current.onTextInput -= OnTextInput;
    }

    private void OnTextInput(char c)
    {
        if (!open || solved) return;
        if (c < '0' || c > '9') return;
        if (entered.Length >= CodeLength) return;

        entered += c;
        message.text = "";
        Refresh();
    }

    private void Backspace()
    {
        if (entered.Length == 0) return;
        entered = entered.Substring(0, entered.Length - 1);
        Refresh();
    }

    private void Submit()
    {
        if (entered.Length < CodeLength) return;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("KeypadStation: no GameManager in the scene");
            return;
        }

        if (GameManager.Instance.TryUnlockDoor(entered))
        {
            solved = true;
            message.text = "ACCESS GRANTED";
            onSolved.Invoke();
            Invoke(nameof(Close), 1f);
        }
        else
        {
            message.text = "ACCESS DENIED";
            entered = "";
            Refresh();
        }
    }

    private void Refresh()
    {
        display.text = entered.PadRight(CodeLength, '_');
    }

    private void LockPlayer(bool locked)
    {
        if (player == null) return;
        player.enabled = !locked;

        if (locked)
        {
            var rb = player.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            var anim = player.GetComponentInChildren<Animator>();
            if (anim != null) anim.SetFloat("Speed", 0f);
        }
    }
}