using UnityEngine;
using TMPro;
using Unity.Entities;

public class HUD : MonoBehaviour
{
    [SerializeField]
    private TMP_Text scoreText;

    protected void Start()
    {
        World.DefaultGameObjectInjectionWorld.GetExistingSystem<ScoringSystem>().RegisterCallback(UpdateScore);
    }

    private void UpdateScore(int NewScore)
    {
        scoreText.text = $"Score: {NewScore}";
    }
}
