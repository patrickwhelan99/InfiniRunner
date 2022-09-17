using UnityEngine;
using Unity.Mathematics;

public class TrackCameraToPlayer : MonoBehaviour
{
    public static float3 PlayerPosition;

    private void LateUpdate()
    {
        transform.position = PlayerPosition + new float3(0.0f, 20.0f, 0.0f);
    }
}
