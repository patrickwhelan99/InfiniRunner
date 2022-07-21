using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class PrefabConverter
{
    private BlobAssetStore BlobStore;
    private GameObjectConversionSettings Settings;

    public static PrefabConverter _instance;
    public static PrefabConverter instance => _instance == null ? _instance = new PrefabConverter() : _instance;

    public static Entity Convert(GameObject Obj)
    {
        instance.BlobStore ??= new BlobAssetStore();
        instance.Settings ??= GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, instance.BlobStore);

        Entity E = GameObjectConversionUtility.ConvertGameObjectHierarchy(Obj, instance.Settings);

        return E;
    }

    public static void Dispose()
    {
        instance.BlobStore.Dispose();
    }
}