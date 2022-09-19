using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public void GameOver()
    {
        Destroy(Instantiate((GameObject)Resources.Load("Prefabs/UI/GameOverUI")), 5.0f);
    }
}
