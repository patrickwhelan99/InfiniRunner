using UnityEngine;
using TMPro;
using Unity.Entities;
using System.Collections;

public class HUD : MonoBehaviour
{
    [SerializeField]
    private TMP_Text scoreText;

    protected void Start()
    {
        StartCoroutine(RegisterCallback());
    }

    /// <summary>
    /// Try registering in a loop since execution order is mismatched between traditional OOP unity and DOD unity
    /// </summary>
    /// <returns></returns>
    private IEnumerator RegisterCallback()
    {
        while (!World.DefaultGameObjectInjectionWorld.IsCreated)
        {
            yield return null;
        }

        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ScoringSystem>().RegisterCallback(UpdateScore);
    }

    private void UpdateScore(int NewScore)
    {
        scoreText.text = $"Score: {NewScore}";
    }
}
