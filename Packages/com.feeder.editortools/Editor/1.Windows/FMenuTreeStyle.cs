using Sirenix.OdinInspector.Editor;

namespace Feeder
{
    public static class FMenuTreeStyle
    {
        public static OdinMenuTree Create()
        {
            var tree = new OdinMenuTree(false)
            {
                DefaultMenuStyle = new OdinMenuStyle
                {
                    BorderPadding = 0f,
                    AlignTriangleLeft = true,
                    TriangleSize = 16f,
                    TrianglePadding = 0f,
                    Offset = 20f,
                    Height = 23,
                    IconPadding = 0f,
                    BorderAlpha = 0.323f
                }
            };
            tree.Config.DrawSearchToolbar = true;
            return tree;
        }
    }
}
