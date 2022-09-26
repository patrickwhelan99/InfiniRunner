using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject initialMenu;

    [SerializeField]
    private GameObject startNewGameMenu;

    [SerializeField]
    private TMPro.TMP_InputField newGameSeedField;

    private string newGameSeed;

    [SerializeField]
    private Button startNewGameButton;

    [SerializeField]
    private Button backButton;

    [SerializeField]
    private Button newGameButton;

    [SerializeField]
    private Button quitButton;

    // Start is called before the first frame update
    private void Start()
    {
        SetupMainMenu();
        SetupNewGameMenu();
    }

    private void OnDestroy()
    {
        newGameButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();
    }

    private void SetupMainMenu()
    {
        newGameButton.onClick.AddListener(() =>
        {
            initialMenu.SetActive(false);
            startNewGameMenu.SetActive(true);
        });

        quitButton.onClick.AddListener(() => Application.Quit(0));
    }

    private void SetupNewGameMenu()
    {
        backButton.onClick.AddListener(() =>
        {
            startNewGameMenu.SetActive(false);
            initialMenu.SetActive(true);
        });

        startNewGameButton.onClick.AddListener(() =>
        {
            newGameSeed = newGameSeedField.text;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Scene");
        });
    }
}
