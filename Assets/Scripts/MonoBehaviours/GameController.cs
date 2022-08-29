public class GameController : Singleton<GameController>
{
    protected void OnDestroy()
    {
        PrefabConverter.Dispose();
    }
}
