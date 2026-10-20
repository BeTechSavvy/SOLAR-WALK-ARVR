using UnityEngine;
using UnityEngine.SceneManagement;

// Main menu buttons. Scene names must match the Build Profiles scene list
// (Solar Walk > Set Up Project adds them for you).
public class MainMenu : MonoBehaviour
{
    public void LoadDesktop() => Load("desktop");
    public void LoadMarker()  => Load("Marker");
    public void LoadWalk()    => Load("Walk");
    public void Quit()        => Application.Quit();

    void Load(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogWarning($"Scene '{sceneName}' is not in the Build Profiles scene list yet. Run Solar Walk > Set Up Project.");
    }
}
