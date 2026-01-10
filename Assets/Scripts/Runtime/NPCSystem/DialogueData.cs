using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerChoice
{
    public string choiceText;

    [TextArea(2, 5)]
    public List<string> npcResponses;

    [HideInInspector]
    public bool hasBeenChosen;
}

[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 5)]
    public string text;
}

[CreateAssetMenu(fileName = "DialogueData", menuName = "Scriptable Objects/DialogueData")]
public class NPCDialogueData : ScriptableObject
{
    [TextArea(2, 5)]
    public string introLine;

    public List<PlayerChoice> choices;
    
    [TextArea(2, 5)]
    public string endLine = "The bell tolls for you.";
}

public enum DialogueState
{
    Intro,
    Choosing,
    NPCResponding
}