using UnityEngine;

public class Pod : MonoBehaviour
{
    public enum PodState {Empty, Flatline, Pulse}

    [Header("State")]
    [SerializeField] private PodState state = PodState.Empty;

    [Header("References")]
    [Header("Monitor")]
    [SerializeField] private Renderer monitor;
    [SerializeField] private Color offColor = new Color(0.043f, 0.051f, 0.071f); // #0B0D12
    [SerializeField] private Color flatlineColor = new Color(0.157f, 0.325f, 0.271f);     // #285345
    [SerializeField] private Color pulseColor = new Color(0.373f, 0.839f, 0.647f); // #5FD6A5
    [SerializeField] private Light podLight;
    [SerializeField] private GameObject creature;

    [Header("Pulse")]
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float minIntesity = 0.5f;
    [SerializeField] private float maxIntesity = 3f;

    public PodState State => state;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetState(state);
    }

    // Update is called once per frame
    void Update()
    {
        if (state != PodState.Pulse) return;

        // Smooth 0..1 wave so the light throbs like a heartbeat
        float t = (Mathf.Sin(Time.time*pulseSpeed) + 1f) * 0.5f;
        podLight.intensity = Mathf.Lerp(minIntesity, maxIntesity, t);

        if(monitor != null)
        {
            monitor.material.color=Color.Lerp(flatlineColor, pulseColor, t);
        }
    }

    public void SetState(PodState newState)
    {
        if (podLight == null || creature == null)
        {
            Debug.LogError("Pod is missing a reference", this);
            return;
        }
        state = newState;

        podLight.gameObject.SetActive(state == PodState.Pulse);
        creature.SetActive(state != PodState.Empty);

        if(monitor != null)
        {
            monitor.material.color = state == PodState.Empty ? offColor : flatlineColor;
        }
    }

    // Lets you flip the state in the Inspector while in Play mode
    void OnValidate()
    {
        if (Application.isPlaying &&podLight != null && creature != null)
        {
            SetState(state);
        }
    }
}
