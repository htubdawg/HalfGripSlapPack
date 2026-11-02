using HalfGripTools;
using UnityEngine;

namespace HalfGripSlapPack
{
    public sealed class MirrorInstaller : MonoBehaviour
    {
        private ModContext context;
        private MirrorSettings settings;
        private AdjustableMirror mirror;
        private GameObject hitTarget;
        private float nextScan;

        internal void Initialize(ModContext ctx)
        {
            context = ctx;
            settings = new MirrorSettings(ctx.Config);
            ctx.OnSceneLoaded(scene => nextScan = Time.unscaledTime + 0.5f);
        }

        private void Update()
        {
            if (context == null) return;
            if (mirror && !mirror.IsBound) RemoveMirror();
            if (mirror)
            {
                // A disabled handle gives Grabber.Update a chance to clear its private Held reference.
                if (!mirror.enabled && mirror.CanInteract && mirror.Grabber.Held != mirror)
                    mirror.enabled = true;
                mirror.canBeGrabbed = mirror.CanInteract;
                return;
            }
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 1f;
            TryAttach();
        }

        private void TryAttach()
        {
            foreach (Grabber grabber in Object.FindObjectsByType<Grabber>(FindObjectsSortMode.None))
            {
                if (!grabber.viewCamera) continue;
                var scene = grabber.gameObject.scene;
                Camera rearCamera = null;
                SpriteRenderer casing = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                    {
                        CarControllerNew car = camera.GetComponentInParent<CarControllerNew>();
                        if (camera.name == "RearViewCamera" && car && car.isPlayer) rearCamera = camera;
                    }
                    foreach (SpriteRenderer sprite in root.GetComponentsInChildren<SpriteRenderer>(true))
                        if (sprite.name == "rearviewcase_0" && sprite.sprite && sprite.transform.parent &&
                            sprite.transform.parent.name == "RearViewMirror") casing = sprite;
                }
                if (!rearCamera || !casing) continue;
                // A separate child keeps the authored mirror sprites and their components untouched.
                hitTarget = new GameObject("SlapPack.MirrorGrip");
                hitTarget.layer = casing.gameObject.layer;
                hitTarget.transform.SetParent(casing.transform, false);
                hitTarget.transform.localPosition = casing.sprite.bounds.center;
                var collider = hitTarget.AddComponent<InteriorSquareCollider>();
                collider.width = casing.sprite.bounds.size.x;
                collider.height = casing.sprite.bounds.size.y;
                collider.hitBoxExpand = 0.05f;
                mirror = hitTarget.AddComponent<AdjustableMirror>();
                mirror.attachedCollider = collider;
                mirror.Initialize(grabber, rearCamera, settings, context.Log);
                context.Log.LogInfo("Adjustable mirror attached. Grab and drag to aim; right-click to reset.");
                return;
            }
        }

        internal void RemoveMirror()
        {
            if (mirror) mirror.Restore();
            // Remove the hitbox from the game's registry immediately, before deferred destruction.
            if (hitTarget) { hitTarget.SetActive(false); Object.Destroy(hitTarget); }
            mirror = null;
            hitTarget = null;
        }

        private void OnDestroy() { RemoveMirror(); }
    }
}
