using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameScene = "Zone1_Greybox_Workspace";
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeValue;   // optional "100%" readout

    private void Start()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(AudioListener.volume);
        ShowVolume(AudioListener.volume);
    }

    public void StartGame()
    {
        // a GameManager left over from an earlier run would carry its HP/stamina into the new game
        if (GameManager.Instance != null) GameManager.Instance.ResetPlayerStats();
        SceneManager.LoadScene(gameScene);
    }

    public void OpenOptions()
    {
        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
    }

    // Wired to the Master Volume slider (0..1).
    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
        ShowVolume(volume);
    }

    private void ShowVolume(float volume)
    {
        if (volumeValue != null) volumeValue.text = Mathf.RoundToInt(volume * 100f) + "%";
    }

    public void QuitGame()
    {
        // Application.Quit is ignored in the editor, so log it to show the button works.
        Debug.Log("MainMenu: Quit pressed");
        Application.Quit();
    }
}
