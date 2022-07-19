using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

using System;
using System.Reflection;
using System.Linq;

[DisallowMultipleComponent]
public class AddTagAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity
{

    [SerializeField]
    string ChosenTag;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        Type[] AllTypes = Assembly.GetExecutingAssembly()
                                    .GetTypes();
        // Get all Component types
        Type[] ValidTypes = AllTypes
                                    .Where(x => x.GetInterface(nameof(IComponentData)) != null)
                                    .ToArray();

        // Filter for zero sized components (tags)
        ValidTypes = ValidTypes.Where
                                (
                                    x => Unity.Entities.TypeManager.GetTypeInfo
                                        (
                                            Unity.Entities.TypeManager.GetTypeIndex(x)
                                        )
                                        .IsZeroSized
                                ).ToArray();

        Type ChosenType = ValidTypes.FirstOrDefault(x => x.Name == ChosenTag);

        if(ChosenType == default)
        {
            Debug.LogError($"Couldn't find tag of type {ChosenType} to add to entity.");
        }

        dstManager.AddComponent(entity, ChosenType);
    }
}
