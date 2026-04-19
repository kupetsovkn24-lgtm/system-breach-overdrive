using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SystemBreachOverdrive
{
    public static class RuntimeSpriteFactory
    {
        private const float TileWorldSize = 1.0f;
        private const float TileInset = 0.98f;
        private const float MinDimension = 0.0001f;

        private static Sprite _whiteSprite;
        private static Sprite _cornerSprite;
        private static Sprite _crossSprite;
        private static Sprite _straightSprite;
        private static Sprite _teeSprite;
        private static Sprite _bgSprite;
        private static float? _pipelineUniformScale;

        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite != null)
                {
                    return _whiteSprite;
                }

                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                texture.filterMode = FilterMode.Point;

                _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                _whiteSprite.name = "RuntimeWhiteSprite";
                return _whiteSprite;
            }
        }

        public static Sprite CornerSprite => _cornerSprite ??= LoadPipeSprite("corner");
        public static Sprite CrossSprite => _crossSprite ??= LoadPipeSprite("cross");
        public static Sprite StraightSprite => _straightSprite ??= LoadPipeSprite("straight");
        public static Sprite TeeSprite => _teeSprite ??= LoadPipeSprite("tee");
        public static Sprite BgSprite => _bgSprite ??= LoadPipeSprite("bg");

        public static bool HasPipelineSprites =>
            CornerSprite != null &&
            CrossSprite != null &&
            StraightSprite != null &&
            TeeSprite != null;

        public static float PipelineUniformScale
        {
            get
            {
                if (_pipelineUniformScale.HasValue)
                {
                    return _pipelineUniformScale.Value;
                }

                if (!HasPipelineSprites)
                {
                    _pipelineUniformScale = 1f;
                    return _pipelineUniformScale.Value;
                }

                var maxDimension = MinDimension;
                maxDimension = Mathf.Max(maxDimension, GetMaxDimension(CornerSprite));
                maxDimension = Mathf.Max(maxDimension, GetMaxDimension(CrossSprite));
                maxDimension = Mathf.Max(maxDimension, GetMaxDimension(StraightSprite));
                maxDimension = Mathf.Max(maxDimension, GetMaxDimension(TeeSprite));

                var targetSize = TileWorldSize * TileInset;
                _pipelineUniformScale = targetSize / maxDimension;
                return _pipelineUniformScale.Value;
            }
        }

        private static float GetMaxDimension(Sprite sprite)
        {
            if (sprite == null)
            {
                return MinDimension;
            }

            var size = sprite.bounds.size;
            return Mathf.Max(size.x, size.y, MinDimension);
        }

        private static Sprite LoadPipeSprite(string spriteName)
        {
            var resourceSprite = Resources.Load<Sprite>($"Pipelines/{spriteName}");
            if (resourceSprite != null)
            {
                return resourceSprite;
            }

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Pipelines/{spriteName}.png");
#else
            return null;
#endif
        }
    }
}
