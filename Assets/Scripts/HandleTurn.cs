using UnityEngine;

[System.Serializable]
public class HandleTurn
{
    public string Attacker; // Name of Attacker
    public string Type;
    public GameObject AttackersGameObject; // Who attacked
    public GameObject AttackersTarget; // Who's going to be attacked

    // Which attacks are performed (write later)
}
