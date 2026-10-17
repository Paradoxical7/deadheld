using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Combat/Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public List<PlayerMove> moves = new List<PlayerMove>();
}
