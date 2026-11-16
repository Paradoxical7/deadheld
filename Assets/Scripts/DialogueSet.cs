using System.Collections.Generic;
using UnityEngine;

// One conversation: an ordered list of lines. Write it in the Inspector, no code needed.
[CreateAssetMenu(fileName = "NewDialogueSet", menuName = "Deadheld/Dialogue Set")] // adds Create > Deadheld > Dialogue Set to the right-click menu
public class DialogueSet : ScriptableObject
{
    public List<DialogueLine> lines = new List<DialogueLine>(); // the Inspector shows this as a list with a + button
}