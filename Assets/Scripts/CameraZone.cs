using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CameraZone : MonoBehaviour
{
    // Every camera a zone knows about registers here, so a prefab needs no scene wiring.
    private static readonly HashSet<Camera> registered = new HashSet<Camera>();

    [SerializeField] private Camera zoneCamera;
    [SerializeField] private Camera fallbackCamera;
    [SerializeField] private Camera[] allCameras;
    [Tooltip("Turn this zone's camera and AudioListener off at startup. Use on CameraRig pieces; leave off for the scene's starting camera.")]
    [SerializeField] private bool startDisabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => registered.Clear();

    private void Awake()
    {
        Register(zoneCamera);
        Register(fallbackCamera);
        if(allCameras != null)
            foreach(var c in allCameras) Register(c);

        if(startDisabled && zoneCamera != null) SetActive(zoneCamera, false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player")) Show(zoneCamera);
    }

    private void OnTriggerExit(Collider other)
    {
        // Only revert if this zone's camera is still the one showing; a neighbouring zone may already have taken over.
        if(!other.CompareTag("Player") || !zoneCamera.enabled) return;
        // No fallback: turn this camera off so the scene's base camera underneath shows again.
        if(fallbackCamera != null) Show(fallbackCamera);
        else SetActive(zoneCamera, false);
    }

    private static void Register(Camera cam)
    {
        if(cam != null) registered.Add(cam);
    }

    private static void Show(Camera cam)
    {
        registered.RemoveWhere(c => c == null);
        foreach(var c in registered) SetActive(c, c == cam);
    }

    private static void SetActive(Camera cam, bool on)
    {
        cam.enabled = on;
        var listener = cam.GetComponent<AudioListener>();
        if(listener != null) listener.enabled = on;
    }

}
