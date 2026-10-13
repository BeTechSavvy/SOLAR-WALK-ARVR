using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void LoadMarker() => Load("Marker");
    public void LoadWalk()   => Load("Walk");
    public void Quit()       => Application.Quit();

    void Load(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogWarning($"Scene '{sceneName}' is not in the Build Profiles scene list yet.");
    }
}