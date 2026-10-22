using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ZoneMapUI : MonoBehaviour
{
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private RectTransform mapImage;
    [SerializeField] private RectTransform playerMarker;
    [SerializeField] private TMP_Text toast;
    [SerializeField] private string toastText = "ZONE 1 MAP ACQUIRED - PRESS M";
    [SerializeField] private float toastTime = 4f;

    [Header("World area the map image covers (x, z)")]
    [SerializeField] private Vector2 worldMin = new Vector2(-17f, -21.5f);
    [SerializeField] private Vector2 worldMax = new Vector2(63f, 78f);

    private Transform player;
    private bool open;
    private float toastUntil;

    // Play Mode Options skips the reload, so these keep their last-session values.
    private void OnEnable()
    {
        open = false;
        toastUntil = 0f;
        if (mapPanel != null) mapPanel.SetActive(false);
        if (toast != null) toast.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (toast != null) toast.gameObject.SetActive(Time.time < toastUntil);

        var kb = Keyboard.current;
        if (kb == null) return;

        bool hasMap = GameManager.Instance != null && GameManager.Instance.hasZoneMap;
        if (hasMap && kb.mKey.wasPressedThisFrame) SetOpen(!open);
        else if (open && kb.escapeKey.wasPressedThisFrame) SetOpen(false);

        if (open) UpdateMarker();
    }

    public void AnnounceMap()
    {
        if (toast == null) return;
        toast.text = toastText;
        toastUntil = Time.time + toastTime;
    }

    private void SetOpen(bool value)
    {
        open = value;
        mapPanel.SetActive(open);
    }

    private void UpdateMarker()
    {
        if (playerMarker == null || mapImage == null) return;

        // Look up the player by its controller, not the "Player" tag: other scene objects can carry that tag too.
        if (player == null)
        {
            var p = FindFirstObjectByType<TankController>();
            if (p == null) { playerMarker.gameObject.SetActive(false); return; }
            player = p.transform;
        }

        // The marker sits centre-anchored on the map image, so 0..1 across the world maps to -half..+half of the image.
        Vector2 uv = new Vector2(
            Mathf.InverseLerp(worldMin.x, worldMax.x, player.position.x),
            Mathf.InverseLerp(worldMin.y, worldMax.y, player.position.z));
        playerMarker.gameObject.SetActive(true);
        playerMarker.anchoredPosition = (uv - new Vector2(0.5f, 0.5f)) * mapImage.rect.size;
    }
}
