using UnityEngine;

[CreateAssetMenu(fileName = "ShipPalette", menuName = "Deadheld/Ship Palette")]
public class ShipPalette : ScriptableObject
{
    public Color baseBlack = new Color32(0x0B, 0x0D, 0x12, 255);
    public Color slate = new Color32(0x3A, 0x43, 0x52, 255);
    public Color charcoal = new Color32(0x22, 0x26, 0x2E, 255);
    public Color highlight = new Color32(0xC9, 0xC6, 0xDD, 255);
    public Color danger = new Color32(0x6E, 0x1F, 0x26, 255);
    public Color energy = new Color32(0x9B, 0xA8, 0x4A, 255);
    public Color teal = new Color32(0x2F, 0x6E, 0x6B, 255);
    public Color alertAmber = new Color32(0xC7, 0x7A, 0x1E, 255);
    public Color alertRed = new Color32(0xB3, 0x26, 0x2B, 255);
    public Color terminal = new Color32(0x5F, 0xD6, 0xA5, 255);
}