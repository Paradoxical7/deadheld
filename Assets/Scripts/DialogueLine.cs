using System;
using UnityEngine;

// One line of dialogue. The green, yellow and red versions sit side by side
// so a writer sees all three wordings of the same moment together.
[Serializable] // lets Unity show this class inside the Inspector, so lines are editable without code
public class DialogueLine
{
    public string id; // unique line ID, also the audio file name (e.g. SA_S3_001)
    public string speaker; //Simon, Minerve, Deimon, Innox, It, or Nora
    public string section; // scene code: S3 is a script scene, E1 is examine text

    public string section_title; // the scene's name, for readers

    public string stage; // ""=  any time, or G, Y, R for green. yellow, red
    public string text; // what the player reads
    public string kind; // "spoken", "signal" (on screen only) or voicemail
    public bool voiced; // true if the line has a voiceover, false if not
    public string note; // writers note, never shown to the player
    public string[] needs; // story flags that must be set before this line plays