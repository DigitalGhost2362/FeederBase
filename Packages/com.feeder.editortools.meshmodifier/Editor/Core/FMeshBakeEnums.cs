#if UNITY_EDITOR
namespace Feeder
{
    public enum FAtlasPackingAlgorithm
    {
        UnityPackTextures,
        TightPacker,
        FastShelfPacker,
        HorizontalStrips,
        VerticalStrips
    }

    public enum FMeshPivotLocation
    {
        WorldOrigin,
        BoundsCenter,
        CustomLocation
    }
}
#endif
