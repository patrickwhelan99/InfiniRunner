using UnityEngine;
using Unity.Entities;

public class PrefabConverter : Singleton<PrefabConverter>
{
    private BlobAssetStore BlobStore;
    private GameObjectConversionSettings Settings;
    public static Entity Convert(GameObject Obj, World DestinationWorld = default)
    {
        World DestWorld = DestinationWorld ?? World.DefaultGameObjectInjectionWorld;

        Instance.BlobStore ??= new BlobAssetStore();
        Instance.Settings ??= GameObjectConversionSettings.FromWorld(DestWorld, Instance.BlobStore); // new GameObjectConversionSettings(DestWorld, GameObjectConversionUtility.ConversionFlags.IsBuildingForPlayer);

        Instance.Settings.DestinationWorld = DestWorld;

        Entity E = GameObjectConversionUtility.ConvertGameObjectHierarchy(Obj, Instance.Settings);

        return E;
    }

    protected void OnDestroy()
    {
        BlobStore?.Dispose();
        // Instance?.BlobStore?.Dispose();
    }
}