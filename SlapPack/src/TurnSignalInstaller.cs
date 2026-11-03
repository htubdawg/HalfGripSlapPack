using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HalfGripTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HalfGripSlapPack
{
    public sealed class TurnSignalInstaller : MonoBehaviour
    {
        private ModContext context;
        private ConfigEntry<bool> enabledSignals;
        private ConfigEntry<int> shortFlashes;
        private ConfigEntry<float> halfPeriod, armSteering, centeredSteering, minimumHeading;
        private CarControllerNew car;
        private CarLink link;
        private HingedObject wheel;
        private TurnSignalStalk stalk;
        private TurnSignalState signal;
        private MeshRenderer leftArrow, rightArrow;
        private Material amber;
        private List<MeshRenderer> leftLamps = new List<MeshRenderer>(), rightLamps = new List<MeshRenderer>();
        private readonly List<Object> owned = new List<Object>(), lampOwned = new List<Object>();
        private Vector3 previousForward;
        private float nextScan;
        private int lampSignature;

        internal void Initialize(ModContext ctx)
        {
            context = ctx;
            enabledSignals = ctx.Config.Bind("TurnSignals", "Enabled", true, "Enable the turn-signal stalk, cockpit arrows and local exterior lamp overlays.");
            shortFlashes = ctx.Config.Bind("TurnSignals", "ShortFlashes", 3, new ConfigDescription("Flashes for a released halfway tap.", new AcceptableValueRange<int>(1, 6)));
            halfPeriod = ctx.Config.Bind("TurnSignals", "BlinkHalfPeriod", 0.4f, new ConfigDescription("Seconds per on/off phase.", new AcceptableValueRange<float>(0.2f, 1f)));
            armSteering = ctx.Config.Bind("TurnSignals", "CancelArmSteering", 12f, new ConfigDescription("Steering degrees into the signaled turn required to arm cancellation.", new AcceptableValueRange<float>(6, 45)));
            centeredSteering = ctx.Config.Bind("TurnSignals", "CancelCenteredSteering", 4f, new ConfigDescription("Steering within this many degrees of center completes cancellation.", new AcceptableValueRange<float>(1, 5)));
            minimumHeading = ctx.Config.Bind("TurnSignals", "CancelMinimumTurn", 15f, new ConfigDescription("Actual heading change into the turn required before cancellation. Parked wheel movement does not cancel.", new AcceptableValueRange<float>(5, 90)));
            ctx.OnSceneLoaded(scene => { RemoveSignals(); nextScan = Time.unscaledTime + 0.5f; });
        }

        private void Update()
        {
            if (context == null) return;
            if (!enabledSignals.Value) { RemoveSignals(); return; }
            if (car && link && wheel && stalk && link.sw && link.sw.wheel == wheel)
            {
                stalk.gameObject.SetActive(car.fpvCamera && car.fpvCamera.isActiveAndEnabled);
                stalk.CheckInteraction();
                if (Time.unscaledTime >= nextScan)
                {
                    nextScan = Time.unscaledTime + 1;
                    if (LampSignature() != lampSignature) RebuildLamps();
                }
                return;
            }
            if (owned.Count > 0) RemoveSignals();
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 1;
            foreach (CarLink candidate in Object.FindObjectsByType<CarLink>(FindObjectsSortMode.None))
            {
                if (!candidate.cc || !candidate.cc.isPlayer || !candidate.sw || !candidate.sw.wheel) continue;
                Grabber grabber = Object.FindObjectsByType<Grabber>(FindObjectsSortMode.None)
                    .FirstOrDefault(g => g.gameObject.scene == candidate.gameObject.scene && g.viewCamera);
                if (!grabber) continue;
                try { Attach(candidate, grabber); }
                catch (Exception error) { RemoveSignals(); context.Log.LogError("Turn signals could not initialize: " + error); nextScan = float.PositiveInfinity; }
                return;
            }
        }

        private void Attach(CarLink target, Grabber grabber)
        {
            link = target; car = target.cc; wheel = target.sw.wheel;
            signal = new TurnSignalState(shortFlashes.Value, halfPeriod.Value, armSteering.Value, centeredSteering.Value, minimumHeading.Value);
            previousForward = car.transform.forward;
            Transform parent = wheel.transform.parent;
            int layer = wheel.gameObject.layer;
            // Measure the wheel in its parent's coordinates without inheriting wheel rotation.
            float radius = 2.4f;
            SpriteRenderer sprite = wheel.GetComponentsInChildren<SpriteRenderer>(false).FirstOrDefault(s => s.sprite && s.name.IndexOf("SteeringWheel", StringComparison.OrdinalIgnoreCase) >= 0);
            if (sprite)
            {
                Vector3 axis = parent ? parent.InverseTransformVector(sprite.transform.TransformVector(Vector3.right * sprite.sprite.bounds.extents.x)) : sprite.transform.TransformVector(Vector3.right * sprite.sprite.bounds.extents.x);
                radius = Mathf.Clamp(axis.magnitude, 1.5f, 3.5f);
            }
            float length = radius + 0.55f;
            Vector3 normal = wheel.movementPlane.normalized;
            if (normal.sqrMagnitude < 0.5f) normal = Vector3.forward;
            var root = new GameObject("SlapPack.TurnSignalStalk");
            owned.Add(root); root.layer = layer;
            root.transform.SetParent(parent, false);
            root.transform.localPosition = wheel.transform.localPosition + normal * 0.15f;
            root.transform.localRotation = Quaternion.LookRotation(normal, Vector3.up);
            Material shaft = TurnSignalVisuals.Material(new Color(0.13f, 0.14f, 0.16f, 1), owned);
            Material knob = TurnSignalVisuals.Material(new Color(0.25f, 0.26f, 0.28f, 1), owned);
            TurnSignalVisuals.Polygon("SignalShaft", root.transform, new[] { new Vector3(-0.08f, -0.11f, 0), new Vector3(-length + 0.25f, -0.08f, 0),
                new Vector3(-length + 0.25f, 0.08f, 0), new Vector3(-0.08f, 0.11f, 0) }, shaft, layer, owned);
            TurnSignalVisuals.Rectangle("SignalKnob", root.transform, -length - 0.32f, -0.20f, -length + 0.32f, 0.20f, -0.02f, knob, layer, owned);
            TurnSignalVisuals.Rectangle("SignalKnobStripe", root.transform, -length - 0.23f, -0.025f, -length + 0.23f, 0.025f, -0.025f,
                TurnSignalVisuals.Material(new Color(0.6f, 0.62f, 0.65f, 1), owned), layer, owned);
            var hit = new GameObject("SignalGrip"); owned.Add(hit); hit.layer = layer;
            hit.transform.SetParent(root.transform, false); hit.transform.localPosition = new Vector3(-length, 0, -0.03f);
            InteriorCircleCollider collider = hit.AddComponent<InteriorCircleCollider>();
            collider.radius = 0.3f; collider.hitBoxExpand = 0.08f;
            stalk = root.AddComponent<TurnSignalStalk>();
            stalk.attachedCollider = collider;
            stalk.Initialize(signal, grabber, car.fpvCamera, normal, length);
            Material green = TurnSignalVisuals.Material(new Color(0.12f, 1f, 0.18f, 1), owned);
            Vector3 dash = wheel.transform.localPosition + new Vector3(0, radius * 0.62f, -0.12f);
            leftArrow = TurnSignalVisuals.Arrow(parent, dash + Vector3.left * 0.5f, false, green, layer, owned);
            rightArrow = TurnSignalVisuals.Arrow(parent, dash + Vector3.right * 0.5f, true, green, layer, owned);
            TurnSignalVisuals.SetArrow(leftArrow, false); TurnSignalVisuals.SetArrow(rightArrow, false);
            amber = TurnSignalVisuals.Material(new Color(1f, 0.4f, 0.015f, 1), owned);
            RebuildLamps();
            context.Log.LogInfo("Turn-signal stalk attached left of steering column. Down=left, up=right; halfway=tap/hold, full=latch. Cockpit arrows and amber lamps active.");
        }

        private void LateUpdate()
        {
            if (!car || signal == null) return;
            Vector3 forward = Vector3.ProjectOnPlane(car.transform.forward, Vector3.up).normalized;
            float delta = Vector3.SignedAngle(previousForward, forward, Vector3.up);
            previousForward = forward;
            if (Time.timeScale > 0)
            {
                signal.ObserveTurn(car.steeringAngleDeg, delta, car.forwardSpeed > 1f);
                signal.Tick(Time.unscaledDeltaTime);
            }
            if (stalk) stalk.RefreshPose();
            bool left = signal.Direction < 0 && signal.FlashOn, right = signal.Direction > 0 && signal.FlashOn;
            bool cockpit = car.fpvCamera && car.fpvCamera.isActiveAndEnabled;
            TurnSignalVisuals.SetArrow(leftArrow, left && cockpit); TurnSignalVisuals.SetArrow(rightArrow, right && cockpit);
            foreach (MeshRenderer lamp in leftLamps) if (lamp) lamp.enabled = left;
            foreach (MeshRenderer lamp in rightLamps) if (lamp) lamp.enabled = right;
        }

        private int LampSignature()
        {
            int signature = 17;
            foreach (MeshRenderer renderer in car.GetComponentsInChildren<MeshRenderer>(true))
                if (renderer.gameObject.activeInHierarchy && (renderer.name == "BecuriFata" || renderer.name == "BecuriSpate" || renderer.name == "HeadlightsCar2" || renderer.name == "TailLightsCar2"))
                    signature = unchecked(signature * 31 + renderer.GetInstanceID());
            return signature;
        }

        private void RebuildLamps()
        {
            DestroyOwned(lampOwned);
            leftLamps = TurnSignalVisuals.Exterior(car, true, amber, lampOwned);
            rightLamps = TurnSignalVisuals.Exterior(car, false, amber, lampOwned);
            lampSignature = LampSignature();
            if (leftLamps.Count < 2 || rightLamps.Count < 2)
                context.Log.LogWarning("Some exterior signal lamp locations were not found for this car body; cockpit indicators remain available.");
        }

        internal void RemoveSignals()
        {
            if (stalk) stalk.gameObject.SetActive(false);
            DestroyOwned(lampOwned); DestroyOwned(owned);
            leftLamps.Clear(); rightLamps.Clear();
            car = null; link = null; wheel = null; stalk = null; signal = null; leftArrow = null; rightArrow = null;
        }

        private static void DestroyOwned(List<Object> objects)
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i]) { if (objects[i] is GameObject go) go.SetActive(false); Object.Destroy(objects[i]); }
            objects.Clear();
        }

        private void OnDestroy() { RemoveSignals(); }
    }
}
