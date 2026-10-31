using System.Collections.Generic;
using UnityEngine;

public enum WeaponPassive { None, BleedPoison }

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Combat/Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    [TextArea] public string description;
    public WeaponPassive passive = WeaponPassive.None;
    public List<PlayerMove> moves = new List<PlayerMove>();
}