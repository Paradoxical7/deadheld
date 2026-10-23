using UnityEngine;

public class KeypadScreen : MonoBehaviour
{
    [SerializeField] private Renderer screen;
    [SerializeField] private SlidingDoors door;   // found automatically inside a door prefab
    [SerializeField] private float fadeSpeed = 2f;

    private static readonly int UnlockedId = Shader.PropertyToID("_Unlocked");
    private MaterialPropertyBlock block;
    private float current;

    void Start()
    {
        if (door == null) door = GetComponentInParent<SlidingDoors>();
        block = new MaterialPropertyBlock();
        current = IsUnlocked() ? 1f : 0f;
        Apply();
    }

    void Update()
    {
        float target = IsUnlocked() ? 1f : 0f;
        if (Mathf.Approximately(current, target)) return;
        current = Mathf.MoveTowards(current, target, fadeSpeed * Time.deltaTime);
        Apply();
    }

    private bool IsUnlocked()
    {
        if (door != null) return door.IsUnlocked;
        return GameManager.Instance != null && GameManager.Instance.doorUnlocked;
    }

    private void Apply()
    {
        screen.GetPropertyBlock(block);
        block.SetFloat(UnlockedId, current);
        screen.SetPropertyBlock(block);
    }
}