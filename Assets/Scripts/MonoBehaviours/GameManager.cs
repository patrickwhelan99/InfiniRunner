using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public void GameOver()
    {
        Instantiate((GameObject)Resources.Load("Prefabs/UI/GameOverUI"));
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainMenu");
    }
}
