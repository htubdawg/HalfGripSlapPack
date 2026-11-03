using HarmonyLib;
using UnityEngine;

namespace HalfGripSlapPack
{
    public sealed class WindowCrank : Grabbable
    {
        private Grabber grabber;
        private Camera worldCamera;
        private CrankFace face;
        private Transform visual;
        private WindowPane pane;
        private float turns, previousHandAngle;
        private readonly WindowTravel travel = new WindowTravel();
        internal Grabber Grabber => grabber;

        // Initialize the plane only after the camera-space proxy has been configured.
        protected override void Awake() { }

        internal void Initialize(Grabber hand, Camera world, CrankFace source, Transform art, WindowPane glass, float fullTurns)
        {
            grabber = hand; worldCamera = world; face = source; visual = art; pane = glass; turns = fullTurns;
            keepPhysicalCursorClose = true;
            keepPhysicalCursorRange = 0.4f;
            releaseCursorBlend = 0;
            handIndicatorCameraOffset = 0.05f;
            movementPlane = Vector3.forward;
            Project();
            ResetRestPose(transform.localPosition, Quaternion.identity);
        }

        internal void Project()
        {
            if (!grabber || !grabber.viewCamera || !worldCamera || !face.Source) return;
            Vector3 point = worldCamera.WorldToViewportPoint(face.WorldPivot);
            bool visible = point.z > 0 && point.x >= -0.05f && point.x <= 1.05f && point.y >= -0.05f && point.y <= 1.05f;
            bool usable = visible && face.Source.gameObject.activeInHierarchy && worldCamera.isActiveAndEnabled && grabber.viewCamera.isActiveAndEnabled &&
                Application.isFocused && !GameInput.Muted && Time.timeScale > 0;
            if (!enabled && grabber.Held != this && usable) enabled = true;
            canBeGrabbed = usable;
            attachedCollider.enabled = usable;
            if (IsGrabbed && !usable) Cancel();
            point.z = 6f;
            transform.position = grabber.viewCamera.ViewportToWorldPoint(point);
            position = new Vector2(transform.localPosition.x, transform.localPosition.y);
            planeOffset = transform.localPosition.z;
        }

        protected override void OnGrab() { previousHandAngle = HandAngle(); }

        private float HandAngle()
        {
            Vector2 offset = constrainedPoint - position;
            return Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        }

        protected override void OnHandMoved()
        {
            if (!canBeGrabbed) { Cancel(); return; }
            Vector2 offset = constrainedPoint - position;
            if (offset.sqrMagnitude < 0.0025f) return;
            float current = HandAngle();
            travel.Turn(-WindowTravel.AngularDelta(previousHandAngle, current), turns);
            previousHandAngle = current;
            angle = -travel.OpenFraction * turns * Mathf.PI * 2;
            // The artwork normal faces the camera, opposite the hand plane's forward normal.
            // Reverse its axis-angle sign so both rotate in the same on-screen direction.
            visual.localRotation = Quaternion.AngleAxis(-angle * Mathf.Rad2Deg, face.Normal);
            pane.SetOpen(travel.OpenFraction);
        }

        protected override void Update() { if (IsGrabbed && (!canBeGrabbed || !GameInput.Hand(0))) Cancel(); }
        protected override void StepPhysics(float dt) { }
        protected override void StepDirect(float dt) { }
        protected override void ApplyPose() { }
        private void Cancel() { blockRelease = false; Release(); enabled = false; }
        private void OnApplicationFocus(bool focused) { if (!focused && IsGrabbed) Cancel(); }
        private void OnApplicationPause(bool paused) { if (paused && IsGrabbed) Cancel(); }
        private void OnDisable() { if (IsGrabbed) Release(); }
    }

    // These proxies already follow the player's view; the game's zero-yaw workaround is for fixed
    // cockpit objects and would misproject a camera-space crank while looking at either door.
    [HarmonyPatch(typeof(Grabber), "BeginHeldLook")]
    internal static class WindowCrankLook
    {
        [HarmonyPrefix]
        private static bool Prefix(Grabber __instance, ref bool __result)
        {
            if (!(__instance.Held is WindowCrank)) return true;
            __result = false;
            return false;
        }
    }
}
