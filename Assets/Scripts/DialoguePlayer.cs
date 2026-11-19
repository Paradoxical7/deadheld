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

    // That sections's lines in order
    private List<DialogueLine> lines;
        
    // Which entry of lines is showing
    private int index;

    void Start()
    {
        // Fetch the whole section once.
        lines=DialogueDatabase.GetSection(section);

        ShowFrom(0);
    }

    void Update()
    {
        // No keyboard connected: nothing to read.
        var kb=Keyboard.current;
        if(kb==null) return;

        // Only react on the frame the key goes down.
        if(kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
        {
            ShowFrom(index + 1);
        }
    }

    // True if this line should play right now.
    bool CanPlay(DialogueLine line)
    {
        // Question 1: are all the story flags this line needs set?
        bool flagsOk=GameManager.Instance.HasFlags(line.needs);

        // Question 2: does it have no stage, or is it the current stage?
        bool stageOk=string.IsNullOrEmpty(line.stage)|| line.stage==GameManager.Instance.GetStage();

        //play only if both answers are yes
        return flagsOk && stageOk;
        
    }
    
    // Shows the first playable line at or after start, or ends
    void ShowFrom(int start)
    {
        for(int i=start; i<lines.Count; i++)
        {
            // Not playable (wrong stage or flags): try the next one.
            if(!CanPlay(lines[i])) continue;

            index=i;
            var line=lines[i];
            dialogueText.text=line.speaker+": "+line.text;
            // Test aid: tint the text by the line's stage
            dialogueText.color=StageColor(line.stage);
            GameManager.Instance.SetFlags(line.sets);
            return;
        }

        // Nothing playable left: the conversation is over. 
        dialogueText.gameObject.SetActive(false);
        index=lines.Count;
    }

    // Test aid: a color for each stage so we can see which variant played
    Color StageColor(string stage)
    {
        if(stage=="G") return Color.green;
        if(stage=="Y") return Color.yellow;
        if(stage=="R") return Color.red;
        return Color.white;
    }
}