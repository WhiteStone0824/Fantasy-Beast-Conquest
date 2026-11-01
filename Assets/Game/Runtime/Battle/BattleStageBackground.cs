using UnityEngine;

namespace FusionAnyMonster.Battle
{
    // A quiet, top-down terrain surface; generated once per battle and kept behind all actors.
    public sealed class BattleStageBackground : MonoBehaviour
    {
        private Camera targetCamera;
        private Texture2D texture;
        private Sprite sprite;

        public static void Create(Camera camera, StageDifficulty difficulty)
        {
            if (camera == null) return;
            var background = new GameObject("Stage Background " + difficulty);
            var component = background.AddComponent<BattleStageBackground>();
            component.targetCamera = camera;
            component.MakeTerrain(difficulty);
            component.FitCamera();
        }

        private void MakeTerrain(StageDifficulty difficulty)
        {
            Color dark, light, accent;
            switch (difficulty)
            {
                case StageDifficulty.Normal: // woodland
                    dark = new Color(.055f, .12f, .095f); light = new Color(.13f, .23f, .14f); accent = new Color(.29f, .32f, .16f); break;
                case StageDifficulty.Hard: // weathered stone
                    dark = new Color(.105f, .105f, .12f); light = new Color(.22f, .20f, .20f); accent = new Color(.34f, .25f, .19f); break;
                case StageDifficulty.VeryHard: // frozen ground
                    dark = new Color(.055f, .095f, .16f); light = new Color(.13f, .22f, .30f); accent = new Color(.30f, .43f, .49f); break;
                case StageDifficulty.Hell: // volcanic ground
                    dark = new Color(.105f, .035f, .045f); light = new Color(.24f, .075f, .055f); accent = new Color(.62f, .19f, .055f); break;
                default: // meadow
                    dark = new Color(.075f, .14f, .11f); light = new Color(.19f, .27f, .16f); accent = new Color(.36f, .38f, .20f); break;
            }

            const int size = 512;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Terrain " + difficulty;
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[size * size];
            float seed = 17f + (int)difficulty * 23f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float broad = Mathf.PerlinNoise(x * .013f + seed, y * .013f + seed);
                float fine = Mathf.PerlinNoise(x * .075f + seed, y * .075f + seed);
                Color color = Color.Lerp(dark, light, Mathf.Clamp01(broad * .72f + fine * .28f));
                // Each terrain has a characteristic, restrained mark without obscuring units.
                float mark = 0f;
                if (difficulty == StageDifficulty.Hell)
                    mark = Mathf.Clamp01((.045f - Mathf.Abs(broad - .50f)) * 8f) * .55f;
                else if (difficulty == StageDifficulty.VeryHard)
                    mark = Mathf.Clamp01((.035f - Mathf.Abs(broad - .52f)) * 8f) * .35f;
                else if (difficulty == StageDifficulty.Hard)
                    mark = Mathf.Clamp01((.028f - Mathf.Abs(broad - .48f)) * 8f) * .28f;
                else
                    mark = Mathf.Clamp01((fine - .70f) * 2f) * .35f;
                pixels[y * size + x] = Color.Lerp(color, accent, mark);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -100;
        }

        private void LateUpdate() { FitCamera(); }

        private void FitCamera()
        {
            if (targetCamera == null || !targetCamera.orthographic) return;
            Vector3 position = targetCamera.transform.position;
            transform.position = new Vector3(position.x, position.y, position.z + 10f);
            float height = targetCamera.orthographicSize * 2f;
            transform.localScale = new Vector3(height * targetCamera.aspect, height, 1f);
        }

        private void OnDestroy()
        {
            if (sprite != null) Destroy(sprite);
            if (texture != null) Destroy(texture);
        }
    }
}
