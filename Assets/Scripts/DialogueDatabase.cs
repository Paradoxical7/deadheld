using UnityEngine;
using System.Collections.Generic;

// loads every line from Resource/dialogue.json once, so any script can look a line up by its ID.
public static class DialogueDatabase{ // static: no GameObject needed, callable from anywhere
    [System.Serializable] // JsonUtility can only fill classes marked Serializable
    private class Wrapper {public DialogueLine[] lines;} // JsonUtility can't read a bare [ ... ] array, so we wrap it
    private static Dictionary<string, DialogueLine> byId; // id -> line, for fast lookup

    public static DialogueLine Get(string id)
    {
        if(byId==null) Load(); // load lazily the first time anyone asks
        byId.TryGetValue(id, out var line); // line stays null if the id doesn't exist
        return line;
    }

    private static void Load()
    {
        byId=new Dictionary<string, DialogueLine>();
        var file=Resources.Load<TextAsset>("dialogue"); // finds Assets/Resources/dialogue.json (no extension)
        var data=JsonUtility.FromJson<Wrapper>("{\"lines\":" + file.text + "}"); // wrap the array so it parses correctly
        foreach ( var l in data.lines) byId[l.id]=l; // later lookups are by id
    }
}
