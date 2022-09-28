using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class AddPrefabComponentAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [SerializeField]
    public PrefabID ID;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent<Prefab>(entity);
        dstManager.AddComponentData(entity, new PrefabComponent()
        {
            Value = ID
        });
    }
}
