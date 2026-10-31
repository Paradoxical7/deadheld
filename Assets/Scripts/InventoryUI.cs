using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InventoryUI : MonoBehaviour
{
    public GameObject panel;
    public WeaponData startingWeapon;     // Pistol: what a new game owns
    public bool pauseWhileOpen = true;

    [Header("Stats")]
    public TMP_Text hpText;
    public TMP_Text nrgText;
    public TMP_Text dodgeText;
    public TMP_Text critText;
    public TMP_Text extraEnergyText;
    public TMP_Text selectedWeaponText;

    [Header("Weapon")]
    public TMP_Dropdown weaponDropdown;
    public TMP_Text movesText;

    [Header("Items")]
    public Transform itemGrid;
    public GameObject itemSlotPrefab;
    public TMP_Text itemDescriptionText;

    private bool isOpen;
    private CursorLockMode prevLock;
    private bool prevVisible;

    void Awake()
    {
        PlayerProgress.EnsureInit(startingWeapon);
        if (panel != null) panel.SetActive(false);
        if (weaponDropdown != null) weaponDropdown.onValueChanged.AddListener(OnWeaponChosen);
    }

    void Update()
    {
        if (TabPressed())
        {
            if (isOpen) Close(); else Open();
        }
    }

    bool TabPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Tab);
#endif
    }

    void Open()
    {
        isOpen = true;
        panel.SetActive(true);
        RefreshAll();

        prevLock = Cursor.lockState;
        prevVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (pauseWhileOpen) Time.timeScale = 0f;
    }

    void Close()
    {
        isOpen = false;
        panel.SetActive(false);

        Cursor.lockState = prevLock;
        Cursor.visible = prevVisible;
        if (pauseWhileOpen) Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        if (isOpen && pauseWhileOpen) Time.timeScale = 1f;
    }

    void RefreshAll()
    {
        RefreshStats();
        RefreshWeaponDropdown();
        RefreshWeaponInfo();
        RefreshItems();
    }

    void RefreshStats()
    {
        GameManager gm = GameManager.Instance;
        if (hpText != null)
            hpText.text = gm != null ? "HP: " + Mathf.CeilToInt(gm.currentHP) + "/" + Mathf.CeilToInt(gm.maxHP) : "HP: --";
        if (nrgText != null) nrgText.text = "NRG: " + PlayerProgress.MaxEnergy;
        if (dodgeText != null) dodgeText.text = "Dodge chance: " + PlayerProgress.Dodge + "%";
        if (critText != null) critText.text = "Crit chance: " + PlayerProgress.Crit + "%";
        if (extraEnergyText != null) extraEnergyText.text = "Extra Energy Gain: " + PlayerProgress.ExtraEnergyChance + "%";
    }

    void RefreshWeaponDropdown()
    {
        if (weaponDropdown == null) return;

        weaponDropdown.ClearOptions();
        weaponDropdown.AddOptions(PlayerProgress.ownedWeapons.Select(w => w.weaponName).ToList());

        int index = PlayerProgress.ownedWeapons.IndexOf(PlayerProgress.equippedWeapon);
        weaponDropdown.SetValueWithoutNotify(Mathf.Max(0, index));
        weaponDropdown.RefreshShownValue();
    }

    void OnWeaponChosen(int index)
    {
        if (index < 0 || index >= PlayerProgress.ownedWeapons.Count) return;
        PlayerProgress.Equip(PlayerProgress.ownedWeapons[index]);
        RefreshWeaponInfo();
    }

    void RefreshWeaponInfo()
    {
        WeaponData w = PlayerProgress.equippedWeapon;
        if (selectedWeaponText != null)
            selectedWeaponText.text = "Selected Weapon: " + (w != null ? w.weaponName : "None");

        if (movesText == null) return;
        if (w == null) { movesText.text = ""; return; }

        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrEmpty(w.description)) sb.Append(w.description).Append("\n\n");
        foreach (PlayerMove m in w.moves)
        {
            sb.Append("<b>").Append(m.moveName).Append("</b>  (").Append(m.energyCost).Append(" nrg");
            if (m.cooldown > 0) sb.Append(", cooldown ").Append(m.cooldown);
            sb.Append(")\n").Append(m.description).Append("\n\n");
        }
        movesText.text = sb.ToString().TrimEnd();
    }

    void RefreshItems()
    {
        foreach (Transform child in itemGrid)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        if (itemDescriptionText != null) itemDescriptionText.text = "";

        foreach (KeyValuePair<ItemData, int> pair in PlayerProgress.items)
        {
            if (pair.Value <= 0) continue;
            GameObject go = Instantiate(itemSlotPrefab, itemGrid);
            go.GetComponent<ItemSlot>().Setup(pair.Key, pair.Value, ShowItem, ShowItem);
        }
    }

    void ShowItem(ItemData item)
    {
        if (itemDescriptionText != null)
            itemDescriptionText.text = item.itemName + "\n" + item.description;
    }
}