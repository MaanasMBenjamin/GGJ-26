using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    public AudioSource ClickSound;
    public void MenuButton()
    {
        SceneManager.LoadScene(0);
    }

    public void RetryButton()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(2);
    }

    public void ButtonSound()
    {
        ClickSound.PlayOneShot(ClickSound.clip);
    }
}
