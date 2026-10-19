using UnityEngine;
using TMPro;

public class CodeNoteText : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private string prefix = "EMERGENCY RELEASE ";

    private void OnEnable()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        if (label != null && GameManager.Instance != null)
            label.text = prefix + GameManager.Instance.puzzleCode;
    }
}