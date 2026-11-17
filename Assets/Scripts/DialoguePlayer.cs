using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;



// shows one line from dialogue.json on screen, if the story flags allow it.
public class DialoguePlayer : MonoBehaviour
{
    [SerializeField] private TMP_Text dialogueText; // drag the on screen text object here in the Inspector
    // Line IDs to play in order, set in the Inspector.
    [SerializeField] private string[] lineIds={"SH_S1_001", "SH_S2_001", "SH_S3_001"}; // which lines to show
    
    // Which entry of lineIds is showing right now.
    private int index;

    void Start()
    {
        // Show the first line when the scene starts.
        ShowLine(index);
    }

    void Update()
    {
        // No keyboard connected: nothing to read.
        var kb=Keyboard.current;
        if(kb==null) return;

        // Only react on the frame the key goes down.
        if(kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
        {
            index++;
            if(index >= lineIds.Length)
            {
                // The conversation is over, so hide the dialogue box.
                dialogueText.gameObject.SetActive(false);

                // Keep index at the end so more key presses do nothing
                index=lineIds.Length;
                return;
            }
            ShowLine(index);
        }
    }
    
    // Shows lineIds[i] on screen, if the story flags allow it.
    void ShowLine(int i)
    {
        // Fetch the line by its ID.
        var line=DialogueDatabase.Get(lineIds[i]);

        // A typo in the ID gives null, so show a message and stop.
        if (line==null)
        {
            dialogueText.text="Missing line: " + lineIds[i];
            return;
        }
        if(!GameManager.Instance.HasFlags(line.needs)) return;

        dialogueText.text=line.speaker+ ": "+line.text;
        GameManager.Instance.SetFlags(line.sets);
    }
}