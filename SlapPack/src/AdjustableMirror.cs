using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace HalfGripSlapPack
{
    public sealed class AdjustableMirror : Grabbable
    {
        internal static AdjustableMirror Active { get; private set; }
        internal Grabber Grabber { get; private set; }
        private Camera rearCamera;
        private Quaternion originalRotation;
        private Quaternion neutralRotation;
        private Transform visualMirror;
        private Vector3 originalMirrorPosition;
        private Quaternion originalMirrorRotation;
        private Vector3 scaledMount;
        private Vector3 localGrip;
        private SpriteRenderer[] mirrorSprites;
        private SpriteRenderer[] handSprites;
        private MirrorSettings settings;
        private MirrorAdjustment adjustment;
        private Vector2 previousHand;
        private readonly List<SmartMirror> suspended = new List<SmartMirror>();
        private bool restored;

        internal bool IsBound => Grabber && rearCamera && visualMirror;

        internal bool CanInteract => Grabber && Grabber.isActiveAndEnabled && Grabber.viewCamera &&
            Grabber.viewCamera.isActiveAndEnabled && rearCamera && rearCamera.isActiveAndEnabled &&
            Application.isFocused && !GameInput.Muted && Time.timeScale > 0f;

        internal void Initialize(Grabber grabber, Camera camera, Transform housing, Vector3 mount,
            MirrorSettings config, ManualLogSource log)
        {
            Grabber = grabber;
            rearCamera = camera;
            settings = config;
            originalRotation = camera.transform.localRotation;
            neutralRotation = originalRotation;
            visualMirror = housing;
            originalMirrorPosition = housing.localPosition;
            originalMirrorRotation = housing.localRotation;
            scaledMount = Vector3.Scale(housing.localScale, mount);
            mirrorSprites = housing.GetComponentsInChildren<SpriteRenderer>(true);
            handSprites = new[] {
                grabber.idleSprite, grabber.interactibleSprite,
                grabber.handIndicator ? grabber.handIndicator.GetComponent<SpriteRenderer>() : null,
                grabber.idleInteractibleSprite, grabber.idleHeldSprite, grabber.interactibleHeldSprite
            };
            Active = this;
            adjustment = new MirrorAdjustment(config.Yaw.Value, config.Pitch.Value);
            adjustment.Move(0, 0, 1, config.YawLimit.Value, config.PitchLimit.Value);
            keepPhysicalCursorClose = false;
            // Input keeps travelling while the visible hand holds the mirror. On release, let the
            // native Grabber rebase its cursor at the grip, not at that accumulated input position.
            releaseCursorBlend = 0f;
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
            else
            {
                // Track the actual touched point on the moving housing for the native hand indicator.
                Ray ray = Grabber.viewCamera.ScreenPointToRay(Grabber.cursor);
                Vector3 grip = attachedCollider.RaycastPlane(ray, out Vector2 point, out float distance)
                    ? attachedCollider.PlaneToWorld(point) : GripWorldPoint();
                localGrip = visualMirror.InverseTransformPoint(grip);
                UpdateGrip();
            }
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
            // Pose comes from bounded adjustment, rather than Grabbable's physics/spring positioning.
            if (IsGrabbed && (!CanInteract || !GameInput.Hand(0))) CancelDrag();
        }

        private void LateUpdate() { ApplyView(); }
        protected override void StepPhysics(float dt) { }
        protected override void StepDirect(float dt) { }
        protected override void ApplyPose() { }

        private void ApplyView()
        {
            if (restored || adjustment == null) return;
            if (rearCamera)
                rearCamera.transform.localRotation = neutralRotation * Quaternion.Euler(-adjustment.Pitch, adjustment.Yaw, 0);
            if (visualMirror)
            {
                // A reflected view turns about twice as far as the mirror surface. Pivot the complete
                // housing/glass assembly about its top mount, translating its center as it tilts.
                Quaternion tilt = Quaternion.Euler(adjustment.Pitch * 0.5f, adjustment.Yaw * 0.5f, 0);
                Quaternion rotation = originalMirrorRotation * tilt;
                visualMirror.localRotation = rotation;
                visualMirror.localPosition = originalMirrorPosition + originalMirrorRotation * scaledMount - rotation * scaledMount;
                if (IsGrabbed) UpdateGrip();
            }
        }

        private void UpdateGrip()
        {
            grabbedPoint = WorldToPlane(visualMirror.TransformPoint(localGrip)) - position;
        }

        internal void KeepHandInFront(Grabber grabber)
        {
            if (restored || grabber != Grabber || !grabber.viewCamera || mirrorSprites == null) return;
            MirrorHandDepth.KeepInFront(grabber.viewCamera, mirrorSprites, handSprites);
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
            if (Active == this) Active = null;
            if (IsGrabbed) Release();
            if (rearCamera) rearCamera.transform.localRotation = originalRotation;
            if (visualMirror)
            {
                visualMirror.localPosition = originalMirrorPosition;
                visualMirror.localRotation = originalMirrorRotation;
            }
            foreach (SmartMirror smart in suspended) if (smart) smart.enabled = true;
            suspended.Clear();
        }

        private void OnDestroy() { Restore(); }
    }
}
