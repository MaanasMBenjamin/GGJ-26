using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class CutScene : MonoBehaviour
{
    private VideoPlayer videoPlayer;

    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.loopPointReached += LoadNextScene;
    }
    void LoadNextScene(VideoPlayer vp)
    {
        SceneManager.LoadScene(2);
    }
    void Update()
{
    if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
    {
        SceneManager.LoadScene(2);
    }
}

}
