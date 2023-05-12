using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroMenu : MonoBehaviour
{
    public void NewGame()
    {
        SceneManager.LoadScene("Map02");
    }
    public void Exit()
    {
        Application.Quit();
    }
}
