using UnityEngine;

namespace SystemBreachOverdrive
{
    public static class GameBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var camera = EnsureCamera();
            EnsureBackground(camera);
            EnsureGameController();
        }

        private static Camera EnsureCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            camera.orthographic = true;
            camera.orthographicSize = 4.6f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.01f, 0.02f, 0.05f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            return camera;
        }

        private static void EnsureBackground(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            var sprite = RuntimeSpriteFactory.BgSprite;
            if (sprite == null)
            {
                return;
            }

            var existing = GameObject.Find("Background");
            if (existing == null)
            {
                existing = new GameObject("Background");
            }

            var renderer = existing.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = existing.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = sprite;
            renderer.sortingOrder = -1000;
            renderer.color = Color.white;

            existing.transform.position = new Vector3(0f, 0f, 5f);

            var spriteSize = sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            {
                existing.transform.localScale = Vector3.one;
                return;
            }

            var cameraHeight = camera.orthographicSize * 2f;
            var cameraWidth = cameraHeight * camera.aspect;
            var scale = Mathf.Max(cameraWidth / spriteSize.x, cameraHeight / spriteSize.y);
            existing.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static void EnsureGameController()
        {
            if (Object.FindFirstObjectByType<GameController>() != null)
            {
                return;
            }

            var root = new GameObject("SystemBreachOverdrive");
            root.AddComponent<GameController>();
        }
    }
}
