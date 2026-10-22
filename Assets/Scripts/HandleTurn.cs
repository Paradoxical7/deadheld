using UnityEngine;

[System.Serializable]
public class HandleTurn
{
    public string Attacker; // Name of Attacker
    public string Type;
    public string Action = "Attack"; // Attack, Guard, Focus, Heal, Buff
    public GameObject AttackersGameObject; // Who attacked
    public GameObject AttackersTarget; // Who's going to be attacked
    public PlayerMove Move; // NULL for guard/focus
}
