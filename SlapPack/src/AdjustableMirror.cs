using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace HalfGripSlapPack
{
    public sealed class AdjustableMirror : Grabbable
    {
        internal Grabber Grabber { get; private set; }
        private Camera rearCamera;
        private Quaternion originalRotation;
        private Quaternion neutralRotation;
        private MirrorSettings settings;
        private MirrorAdjustment adjustment;
        private Vector2 previousHand;
        private readonly List<SmartMirror> suspended = new List<SmartMirror>();
        private bool restored;

        internal bool IsBound => Grabber && rearCamera;

        internal bool CanInteract => Grabber && Grabber.isActiveAndEnabled && Grabber.viewCamera &&
            Grabber.viewCamera.isActiveAndEnabled && rearCamera && rearCamera.isActiveAndEnabled &&
            Application.isFocused && !GameInput.Muted && Time.timeScale > 0f;

        internal void Initialize(Grabber grabber, Camera camera, MirrorSettings config, ManualLogSource log)
        {
            Grabber = grabber;
            rearCamera = camera;
            settings = config;
            originalRotation = camera.transform.localRotation;
            neutralRotation = originalRotation;
            adjustment = new MirrorAdjustment(config.Yaw.Value, config.Pitch.Value);
            adjustment.Move(0, 0, 1, config.YawLimit.Value, config.PitchLimit.Value);
            keepPhysicalCursorClose = false;
            releaseCursorBlend = 1f;
            directSpeedKeptOnRelease = 0f;
            foreach (SmartMirror smart in Object.FindObjectsByType<SmartMirror>(FindObjectsSortMode.None))
            {
                if (smart.gameObject.scene != camera.gameObject.scene || smart.rearCameraName != camera.name || !smart.enabled) continue;
                // SmartMirror may already be tracking an enemy. Use its cached rest pose for reset,
                // while retaining the current pose separately for a faithful unload.
                object rest = AccessTools.Field(typeof(SmartMirror), "baseLocalRotation")?.GetValue(smart);
                if (rest is Quaternion q && Quaternion.Dot(q, q) > 0.9f) neutralRotation = q;
                smart.enabled = false;
                suspended.Add(smart);
            }
            if (suspended.Count > 0) log.LogInfo("Smart Mirror auto-aim suspended while manual mirror adjustment is active.");
            ApplyView();
        }

        protected override void OnGrab()
        {
            previousHand = constrainedPoint;
            if (!CanInteract) CancelDrag();
        }

        protected override void OnHandMoved()
        {
            if (!CanInteract) { CancelDrag(); return; }
            Vector2 delta = constrainedPoint - previousHand;
            // Consume movement at the stops too, so reversing direction responds immediately.
            previousHand = constrainedPoint;
            adjustment.Move(delta.x, delta.y, settings.Sensitivity.Value, settings.YawLimit.Value, settings.PitchLimit.Value);
            ApplyView();
        }

        protected override void OnRelease() { if (settings != null) settings.Save(adjustment); }

        protected override void OnRightClick()
        {
            if (!CanInteract) return;
            adjustment.Reset();
            previousHand = constrainedPoint;
            ApplyView();
            settings.Save(adjustment);
        }

        protected override void Update()
        {
            // Do not run Grabbable's physics: the mirror housing stays fixed in this prototype.
            if (IsGrabbed && (!CanInteract || !GameInput.Hand(0))) CancelDrag();
        }

        private void LateUpdate() { ApplyView(); }
        protected override void StepPhysics(float dt) { }
        protected override void StepDirect(float dt) { }
        protected override void ApplyPose() { }

        private void ApplyView()
        {
            if (!restored && rearCamera && adjustment != null)
                rearCamera.transform.localRotation = neutralRotation * Quaternion.Euler(-adjustment.Pitch, adjustment.Yaw, 0);
        }

        private void CancelDrag()
        {
            blockRelease = false;
            Release();
            canBeGrabbed = false;
            enabled = false;
        }

        private void OnApplicationFocus(bool focused) { if (!focused && IsGrabbed) CancelDrag(); }
        private void OnApplicationPause(bool paused) { if (paused && IsGrabbed) CancelDrag(); }
        private void OnDisable() { if (IsGrabbed) Release(); }

        internal void Restore()
        {
            if (restored) return;
            restored = true;
            if (IsGrabbed) Release();
            if (rearCamera) rearCamera.transform.localRotation = originalRotation;
            foreach (SmartMirror smart in suspended) if (smart) smart.enabled = true;
            suspended.Clear();
        }

        private void OnDestroy() { Restore(); }
    }
}
