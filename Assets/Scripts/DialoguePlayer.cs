using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using TMPro;



// shows one line from dialogue.json on screen, if the story flags allow it.
public class DialoguePlayer : MonoBehaviour
{
    [SerializeField] private TMP_Text dialogueText; // drag the on screen text object here in the Inspector
        
    // Which section of dialogue.json to play, for example S1.
    [SerializeField] private string section="S1";

    // That sectin's lines in order
    private List<DialogueLine> lines;
        
    // Which entry of lines is showing
    private int index;

    void Start()
    {
        // Fetch the whole section once.
        lines=DialogueDatabase.GetSection(section);

        // No lines for this section: hide the box and stop.
        if(lines.Count==0)
        {
            dialogueText.gameObject.SetActive(false);
            return;
        }

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
            if(index >= lines.Count)
            {
                // The conversation is over, so hide the dialogue box.
                dialogueText.gameObject.SetActive(false);

                // Keep index at the end so more key presses do nothing
                index=lines.Count;
                return;
            }
            ShowLine(index);
        }
    }
    
    // Show lines[i] on screen
    void ShowLine(int i)
    {
        // Take the line from the section list.
        var line=lines[i];

        if(!GameManager.Instance.HasFlags(line.needs)) return;

        dialogueText.text=line.speaker+ ": "+line.text;
        GameManager.Instance.SetFlags(line.sets);
    }
}