using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        Debug.Log("clicked");
        SceneManager.LoadSceneAsync(1);
    }

    public void QuitGame() 
    {
        Application.Quit();
    }
}
