using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CameraZone : MonoBehaviour
{
    [SerializeField] private Camera zoneCamera;
    [SerializeField] private Camera fallbackCamera;
    [SerializeField] private Camera[] allCameras;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player")) Show(zoneCamera);
    }

    private void OnTriggerExit(Collider other)
    {
        if(fallbackCamera != null && other.CompareTag("Player")) Show(fallbackCamera);
    }

    private void Show(Camera cam)
    {
        foreach(var c in allCameras)
        {
            bool on = c == cam;
            c.enabled=on;
            var listener = c.GetComponent<AudioListener>();
            if(listener != null) listener.enabled=on;
        }
    }

}
