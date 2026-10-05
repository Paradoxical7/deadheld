using UnityEngine;

[System.Serializable]
public class HandleTurn
{
    public string Attacker; // Name of Attacker
    public string Type;
    public string Action = "Attack"; // Attack, Guard, Focus
    public GameObject AttackersGameObject; // Who attacked
    public GameObject AttackersTarget; // Who's going to be attacked

    public float MoveEnergyCost;
    public float MoveDamageMultiplier = 1f;
}
