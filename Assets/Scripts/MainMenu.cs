using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
    public AudioSource ButtonSound;
    void Awake()
    {
        Time.timeScale = 0;
    }

    public void PlayClick()
    {
        if(ButtonSound != null)
        {
            ButtonSound.PlayOneShot(ButtonSound.clip);
        }
    }


    public void PlayGame()
    {
        Debug.Log("clicked");
        Time.timeScale = 1;
        SceneManager.LoadSceneAsync(1);
    }

    public void QuitGame() 
    {
        Application.Quit();
    }
}
