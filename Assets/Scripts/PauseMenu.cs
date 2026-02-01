using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    public GameObject container;
    public AudioSource menuSound;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if (!container.activeSelf)
            {
                PauseButton();
            }
            else
            {
                ResumeButton();
            }
            playClick();
        }
    }

    public void playClick()
    {
        if(menuSound != null)
        {
            menuSound.PlayOneShot(menuSound.clip);
        }
    }
    public void PauseButton()
    {
        container.SetActive(true);
        Time.timeScale = 0;
    }

    public void ResumeButton()
    {
        container.SetActive(false);
        Time.timeScale = 1;
    }

    public void RestartButton()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitMenu()
    {
        Time.timeScale = 1;
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        SceneManager.LoadScene(0);
    }
}
