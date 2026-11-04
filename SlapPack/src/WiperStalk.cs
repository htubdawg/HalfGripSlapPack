using UnityEngine;

namespace HalfGripSlapPack
{
    public sealed class WiperStalk : Grabbable
    {
        private WiperState state;
        private Grabber hand;
        private Camera drivingCamera;
        private MeshRenderer[] settings;
        private float startAngle, startPull;
        private const float TravelDegrees = 16;
        private bool CanInteract => hand && hand.isActiveAndEnabled && hand.viewCamera && hand.viewCamera.isActiveAndEnabled &&
            drivingCamera && drivingCamera.isActiveAndEnabled && Application.isFocused && !GameInput.Muted && Time.timeScale > 0;

        protected override void Awake() { }

        internal void Initialize(WiperState value, Grabber grabber, Camera camera, Vector3 normal, float length, MeshRenderer[] artwork)
        {
            state = value; hand = grabber; drivingCamera = camera; settings = artwork;
            movementPlane = normal;
            keepPhysicalCursorClose = true;
            keepPhysicalCursorRange = Mathf.Max(0.6f, length * 0.16f);
            handIndicatorCameraOffset = 0.2f;
            releaseCursorBlend = 0;
            ResetRestPose(transform.localPosition, transform.localRotation);
            RefreshPose();
        }

        internal void CheckInteraction()
        {
            if (!enabled && CanInteract && hand.Held != this) enabled = true;
            canBeGrabbed = CanInteract;
            if (IsGrabbed && !CanInteract) CancelHold();
        }

        protected override void OnGrab() { startAngle = HandAngle(); startPull = state.Pull; }
        protected override void OnHandMoved()
        {
            if (!CanInteract) { CancelHold(); return; }
            if ((constrainedPoint - position).sqrMagnitude < 0.01f) return;
            state.SetPull(startPull - Mathf.DeltaAngle(startAngle, HandAngle()) / TravelDegrees);
            RefreshPose();
        }

        private float HandAngle()
        {
            Vector2 offset = constrainedPoint - position;
            return Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        }

        protected override void OnRightClick()
        {
            if (!CanInteract) return;
            state.NextSpeed();
            RefreshPose();
        }

        protected override void OnRelease() { if (state != null) { state.Release(); RefreshPose(); } }
        internal void RefreshPose()
        {
            if (state == null) return;
            angle = -state.Pull * TravelDegrees * Mathf.Deg2Rad;
            base.ApplyPose();
            for (int i = 0; i < settings.Length; i++) if (settings[i]) settings[i].enabled = i == (int)state.Speed;
        }

        protected override void Update() { if (IsGrabbed && (!CanInteract || !GameInput.Hand(0))) CancelHold(); }
        protected override void StepPhysics(float dt) { }
        protected override void StepDirect(float dt) { }
        protected override void ApplyPose() { RefreshPose(); }
        private void CancelHold() { blockRelease = false; Release(); enabled = false; }
        private void OnApplicationFocus(bool focused) { if (!focused && IsGrabbed) CancelHold(); }
        private void OnApplicationPause(bool paused) { if (paused && IsGrabbed) CancelHold(); }
        private void OnDisable() { if (IsGrabbed) Release(); }
    }
}
