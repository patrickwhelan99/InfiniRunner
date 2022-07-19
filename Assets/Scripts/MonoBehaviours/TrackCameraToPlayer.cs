using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public class TrackCameraToPlayer : MonoBehaviour
{
    public static float3 PlayerPosition;

    void LateUpdate()
    {
        transform.position = PlayerPosition + new float3(0.0f, 20.0f, 0.0f);
    }
}
