#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Feeder
{
    public sealed class FAtlasLayout
    {
        private const int MinAtlasSize = 16;
        private const float ShrinkStep = 0.9f;
        private const int MaxShrinkAttempts = 60;

        public Vector2Int Size;
        public RectInt[] Rects;
        public int Bleed;

        #region Build

        public static FAtlasLayout Build(IReadOnlyList<Vector2Int> tileSizes, int padding, int maxAtlasSize,
            FAtlasPackingAlgorithm algorithm)
        {
            if (tileSizes.Count == 0)
                throw new ArgumentException("Atlas layout needs at least one tile.", nameof(tileSizes));

            switch (algorithm)
            {
                case FAtlasPackingAlgorithm.UnityPackTextures:
                    return BuildWithUnity(tileSizes, padding, maxAtlasSize);
                case FAtlasPackingAlgorithm.TightPacker:
                    return BuildFree(tileSizes, padding, maxAtlasSize, TryPackMaxRects);
                case FAtlasPackingAlgorithm.FastShelfPacker:
                    return BuildFree(tileSizes, padding, maxAtlasSize, TryPackShelves);
                case FAtlasPackingAlgorithm.HorizontalStrips:
                    return BuildStrips(tileSizes, padding, maxAtlasSize, true);
                case FAtlasPackingAlgorithm.VerticalStrips:
                    return BuildStrips(tileSizes, padding, maxAtlasSize, false);
                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
            }
        }

        #endregion

        #region Free packing

        private delegate RectInt[] PackAttempt(Vector2Int[] paddedSizes, int width, int height);

        private static FAtlasLayout BuildFree(IReadOnlyList<Vector2Int> tileSizes, int padding, int maxAtlasSize,
            PackAttempt pack)
        {
            long area = tileSizes.Sum(s => (long)(s.x + 2 * padding) * (s.y + 2 * padding));
            float scale = Mathf.Min(1f, Mathf.Sqrt((float)maxAtlasSize * maxAtlasSize / Mathf.Max(1f, area)));
            List<Vector2Int> candidates = AtlasCandidates(maxAtlasSize);

            for (int attempt = 0; attempt < MaxShrinkAttempts; attempt++)
            {
                Vector2Int[] inner = tileSizes.Select(s => ScaleSize(s, scale)).ToArray();
                Vector2Int[] padded = inner.Select(s => new Vector2Int(s.x + 2 * padding, s.y + 2 * padding)).ToArray();
                long paddedArea = padded.Sum(s => (long)s.x * s.y);
                int widest = padded.Max(s => s.x);
                int tallest = padded.Max(s => s.y);

                foreach (Vector2Int size in candidates)
                {
                    if ((long)size.x * size.y < paddedArea || size.x < widest || size.y < tallest)
                        continue;

                    RectInt[] placed = pack(padded, size.x, size.y);
                    if (placed == null)
                        continue;

                    var rects = new RectInt[placed.Length];
                    for (int i = 0; i < placed.Length; i++)
                        rects[i] = new RectInt(placed[i].x + padding, placed[i].y + padding, inner[i].x, inner[i].y);
                    return new FAtlasLayout { Size = size, Rects = rects, Bleed = padding };
                }

                scale *= ShrinkStep;
            }

            throw new InvalidOperationException("Could not fit the textures into the atlas even after shrinking them.");
        }

        private static List<Vector2Int> AtlasCandidates(int maxAtlasSize)
        {
            var sizes = new List<Vector2Int>();
            for (int w = MinAtlasSize; w <= maxAtlasSize; w *= 2)
                for (int h = MinAtlasSize; h <= maxAtlasSize; h *= 2)
                    if (Mathf.Max(w, h) <= Mathf.Min(w, h) * 2)
                        sizes.Add(new Vector2Int(w, h));
            return sizes.OrderBy(s => (long)s.x * s.y).ThenBy(s => Mathf.Abs(s.x - s.y)).ThenByDescending(s => s.x).ToList();
        }

        private static RectInt[] TryPackMaxRects(Vector2Int[] sizes, int width, int height)
        {
            var free = new List<RectInt> { new RectInt(0, 0, width, height) };
            var result = new RectInt[sizes.Length];
            int[] order = Enumerable.Range(0, sizes.Length)
                .OrderByDescending(i => Mathf.Max(sizes[i].x, sizes[i].y)).ThenByDescending(i => sizes[i].x * sizes[i].y).ToArray();

            foreach (int i in order)
            {
                int bestIndex = -1;
                int bestShort = int.MaxValue;
                int bestLong = int.MaxValue;
                for (int f = 0; f < free.Count; f++)
                {
                    RectInt space = free[f];
                    if (space.width < sizes[i].x || space.height < sizes[i].y)
                        continue;
                    int dx = space.width - sizes[i].x;
                    int dy = space.height - sizes[i].y;
                    int shortSide = Mathf.Min(dx, dy);
                    int longSide = Mathf.Max(dx, dy);
                    if (shortSide < bestShort || (shortSide == bestShort && longSide < bestLong))
                    {
                        bestIndex = f;
                        bestShort = shortSide;
                        bestLong = longSide;
                    }
                }

                if (bestIndex < 0)
                    return null;

                var placed = new RectInt(free[bestIndex].x, free[bestIndex].y, sizes[i].x, sizes[i].y);
                result[i] = placed;
                SplitFreeRects(free, placed);
            }

            return result;
        }

        private static void SplitFreeRects(List<RectInt> free, RectInt used)
        {
            var created = new List<RectInt>();
            for (int f = free.Count - 1; f >= 0; f--)
            {
                RectInt space = free[f];
                if (!space.Overlaps(used))
                    continue;

                free.RemoveAt(f);
                if (used.xMin > space.xMin)
                    created.Add(new RectInt(space.xMin, space.yMin, used.xMin - space.xMin, space.height));
                if (used.xMax < space.xMax)
                    created.Add(new RectInt(used.xMax, space.yMin, space.xMax - used.xMax, space.height));
                if (used.yMin > space.yMin)
                    created.Add(new RectInt(space.xMin, space.yMin, space.width, used.yMin - space.yMin));
                if (used.yMax < space.yMax)
                    created.Add(new RectInt(space.xMin, used.yMax, space.width, space.yMax - used.yMax));
            }

            free.AddRange(created);
            for (int a = free.Count - 1; a >= 0; a--)
                for (int b = 0; b < free.Count; b++)
                    if (a != b && Contains(free[b], free[a]))
                    {
                        free.RemoveAt(a);
                        break;
                    }
        }

        private static bool Contains(RectInt outer, RectInt inner)
        {
            return inner.xMin >= outer.xMin && inner.yMin >= outer.yMin &&
                   inner.xMax <= outer.xMax && inner.yMax <= outer.yMax;
        }

        private static RectInt[] TryPackShelves(Vector2Int[] sizes, int width, int height)
        {
            var result = new RectInt[sizes.Length];
            int[] order = Enumerable.Range(0, sizes.Length).OrderByDescending(i => sizes[i].y).ToArray();
            int x = 0, y = 0, shelfHeight = 0;

            foreach (int i in order)
            {
                if (x + sizes[i].x > width)
                {
                    y += shelfHeight;
                    x = 0;
                    shelfHeight = 0;
                }
                if (sizes[i].x > width || y + sizes[i].y > height)
                    return null;

                result[i] = new RectInt(x, y, sizes[i].x, sizes[i].y);
                x += sizes[i].x;
                shelfHeight = Mathf.Max(shelfHeight, sizes[i].y);
            }

            return result;
        }

        #endregion

        #region Strip packing

        private static FAtlasLayout BuildStrips(IReadOnlyList<Vector2Int> tileSizes, int padding, int maxAtlasSize,
            bool horizontal)
        {
            int across = Mathf.Min(maxAtlasSize, Mathf.NextPowerOfTwo(tileSizes.Max(s => horizontal ? s.x : s.y)));
            int[] lengths = tileSizes.Select(s => Mathf.Max(1, horizontal ? s.y : s.x)).ToArray();
            int total = lengths.Sum() + 2 * padding * lengths.Length;
            float scale = 1f;
            if (total > maxAtlasSize)
            {
                int usable = maxAtlasSize - 2 * padding * lengths.Length;
                if (usable < lengths.Length)
                    throw new InvalidOperationException("Too many textures for a strip atlas of this size; lower the padding or raise Max Atlas Size.");
                scale = (float)usable / lengths.Sum();
            }

            int[] scaled = lengths.Select(l => Mathf.Max(1, Mathf.FloorToInt(l * scale))).ToArray();
            int along = Mathf.Max(MinAtlasSize, Mathf.NextPowerOfTwo(scaled.Sum() + 2 * padding * scaled.Length));
            along = Mathf.Min(along, maxAtlasSize);

            var rects = new RectInt[scaled.Length];
            int cursor = 0;
            for (int i = 0; i < scaled.Length; i++)
            {
                cursor += padding;
                rects[i] = horizontal ? new RectInt(0, cursor, across, scaled[i]) : new RectInt(cursor, 0, scaled[i], across);
                cursor += scaled[i] + padding;
            }

            return new FAtlasLayout
            {
                Size = horizontal ? new Vector2Int(across, along) : new Vector2Int(along, across),
                Rects = rects,
                Bleed = padding
            };
        }

        #endregion

        #region Unity packing

        private static FAtlasLayout BuildWithUnity(IReadOnlyList<Vector2Int> tileSizes, int padding, int maxAtlasSize)
        {
            var dummies = tileSizes.Select(s => new Texture2D(s.x, s.y, TextureFormat.Alpha8, false)).ToArray();
            var atlas = new Texture2D(MinAtlasSize, MinAtlasSize, TextureFormat.Alpha8, false);
            try
            {
                Rect[] uvRects = atlas.PackTextures(dummies, padding, maxAtlasSize, false);
                if (uvRects == null)
                    throw new InvalidOperationException("Texture2D.PackTextures failed to lay out the atlas.");

                var size = new Vector2Int(atlas.width, atlas.height);
                var rects = uvRects.Select(r => new RectInt(
                    Mathf.RoundToInt(r.x * size.x), Mathf.RoundToInt(r.y * size.y),
                    Mathf.Max(1, Mathf.RoundToInt(r.width * size.x)), Mathf.Max(1, Mathf.RoundToInt(r.height * size.y)))).ToArray();
                return new FAtlasLayout { Size = size, Rects = rects, Bleed = padding / 2 };
            }
            finally
            {
                foreach (Texture2D dummy in dummies)
                    Object.DestroyImmediate(dummy);
                Object.DestroyImmediate(atlas);
            }
        }

        #endregion

        private static Vector2Int ScaleSize(Vector2Int size, float scale)
        {
            return new Vector2Int(Mathf.Max(1, Mathf.FloorToInt(size.x * scale)), Mathf.Max(1, Mathf.FloorToInt(size.y * scale)));
        }
    }
}
#endif
