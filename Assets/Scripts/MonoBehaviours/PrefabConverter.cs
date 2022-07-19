using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public static class PrefabConverter
{
    private static BlobAssetStore BlobStore;
    private static GameObjectConversionSettings Settings;

    public static Entity Convert(GameObject Obj)
    {
        BlobStore ??= new BlobAssetStore();
        Settings ??= GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, BlobStore);

        Entity E = GameObjectConversionUtility.ConvertGameObjectHierarchy(Obj, Settings);

        return E;
    }

    public static void Dispose()
    {
        BlobStore.Dispose();
    }
}