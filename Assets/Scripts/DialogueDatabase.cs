using UnityEngine;
using System.Collections.Generic;

// loads every line from Resource/dialogue.json once, so any script can look a line up by its ID.
public static class DialogueDatabase{ // static: no GameObject needed, callable from anywhere
    [System.Serializable] // JsonUtility can only fill classes marked Serializable
    private class Wrapper {public DialogueLine[] lines;} // JsonUtility can't read a bare [ ... ] array, so we wrap it
    private static Dictionary<string, DialogueLine> byId; // id -> line, for fast lookup

    // Every line in the same order as dialogue.json.
    private static List<DialogueLine> all;

    public static DialogueLine Get(string id)
    {
        if(byId==null) Load(); // load lazily the first time anyone asks
        byId.TryGetValue(id, out var line); // line stays null if the id doesn't exist
        return line;
    }

    // All lines of one section, in file order. 
    public static List<DialogueLine> GetSection(string section)
    {
        // Load on first use, same as Get.
        if(all==null)Load();

        var result=new List<DialogueLine>();
        // Walk every line in file order and keep the ones from this section.
        foreach(var l in all)
        {
            if (l.section==section) result.Add(l);
        }
        return result;
    }

    private static void Load()
    {
        byId=new Dictionary<string, DialogueLine>();
        all=new List<DialogueLine>();
        var file=Resources.Load<TextAsset>("dialogue"); // finds Assets/Resources/dialogue.json (no extension)
        var data=JsonUtility.FromJson<Wrapper>("{\"lines\":" + file.text + "}"); // wrap the array so it parses correctly
        foreach ( var l in data.lines)
        {
            byId[l.id]=l;
            all.Add(l);
        }
    }
}
