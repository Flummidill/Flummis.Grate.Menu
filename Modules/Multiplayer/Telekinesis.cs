using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Tools;
using GorillaLocomotion;
using System;
using System.Collections.Generic;
using UnityEngine;
using ICSharpCode.SharpZipLib.GZip;
using BepInEx.Configuration;
using UnityEngine.Animations.Rigging;
using UnityEngine.UIElements;
using UnityEngine.Windows;
using UnityEngine.XR;
using Valve.VR;
using BepInEx;
using Pathfinding;

namespace Grate.Modules.Multiplayer
{
    public class Telekinesis : GrateModule
    {
        public static readonly string DisplayName = "Telekinesis";
        public static Telekinesis Instance;
        private List<TKMarker> markers = new List<TKMarker>();
        public SphereCollider tkCollider;
        ParticleSystem playerParticles, sithlordHandParticles;
        AudioSource sfx;
        TKMarker sithLord;
        void Awake() { Instance = this; }

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            try
            {
                ReloadConfiguration();
                var prefab = Plugin.assetBundle.LoadAsset<GameObject>("TK Hitbox");
                var hitbox = Instantiate(prefab);
                hitbox.name = "Grate TK Hitbox";
                hitbox.transform.SetParent(Player.Instance.bodyCollider.transform, false);
                hitbox.layer = GrateInteractor.InteractionLayer;
                tkCollider = hitbox.GetComponent<SphereCollider>();
                tkCollider.isTrigger = true;
                playerParticles = hitbox.GetComponent<ParticleSystem>();
                playerParticles.Stop();
                playerParticles.Clear();
                sfx = hitbox.GetComponent<AudioSource>();

                var sithlordEffect = Instantiate(prefab);
                sithlordEffect.name = "Grate Sithlord Particles";
                sithlordEffect.transform.SetParent(Player.Instance.bodyCollider.transform, false);
                sithlordEffect.layer = GrateInteractor.InteractionLayer;
                sithlordHandParticles = sithlordEffect.GetComponent<ParticleSystem>();
                var shape = sithlordHandParticles.shape;
                shape.radius = .2f;
                shape.position = Vector3.zero;
                Destroy(sithlordEffect.GetComponent<SphereCollider>());
                DistributeMidichlorians();
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }
        Joint joint;
        void FixedUpdate()
        {
            if (ControllerInputPoller.instance.rightGrab) { OnGrip(); } else { if (isCopying == true || whoCopy != null || skipRay == true || theRig != null) { isCopying = false; whoCopy = null; skipRay = false; theRig = null; } }
            if (ControllerInputPoller.TriggerFloat(XRNode.RightHand) > 0.5f) { trigR = true; } else { trigR = false; }

            if (Time.frameCount % 300 == 0)
                DistributeMidichlorians();

            if (!sithLord) TryGetSithLord();

            if (sithLord)
            {
                var rb = Player.Instance.bodyCollider.attachedRigidbody;
                if (!sithLord.IsGripping())
                {
                    sithLord = null;
                    sfx.Stop();
                    sithlordHandParticles.Stop();
                    sithlordHandParticles.Clear();
                    playerParticles.Stop();
                    playerParticles.Clear();
                    rb.velocity = Player.Instance.bodyVelocityTracker.GetAverageVelocity(true, 0.15f, false) * 2;
                    return;
                }

                Vector3 end = sithLord.controllingHand.position + sithLord.controllingHand.up * 3 * sithLord.rig.scaleFactor;
                Vector3 direction = end - Player.Instance.bodyCollider.transform.position;
                rb.AddForce(direction * 10, ForceMode.Impulse);
                float dampingThreshold = direction.magnitude * 10;
                //if (rb.velocity.magnitude > dampingThreshold)
                //if(direction.magnitude < 1)
                rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, .1f);
            }

        }

        bool trigR = false;
        VRRig ChosenSith = null;
        bool isCopying = false;
        VRRig whoCopy = null;
        bool skipRay = false;
        VRRig theRig = null;
        void OnGrip()
        {
            if (SelectSith)
            {
                UnityEngine.Physics.Raycast(GorillaTagger.Instance.rightHandTransform.position, GorillaTagger.Instance.rightHandTransform.forward, out var Ray, 512f);

                Vector3 StartPosition = GorillaTagger.Instance.rightHandTransform.position;
                Vector3 EndPosition = skipRay ? theRig.transform.position : isCopying ? whoCopy.transform.position : Ray.point;

                GameObject NewPointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                NewPointer.GetComponent<Renderer>().material.shader = Shader.Find("GUI/Text Shader");
                NewPointer.GetComponent<Renderer>().material.color = skipRay ? new Color(0f, 0f, 0f, 1f) : isCopying ? new Color(0f, 0f, 0f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
                NewPointer.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                NewPointer.transform.position = EndPosition;

                UnityEngine.Object.Destroy(NewPointer.GetComponent<BoxCollider>());
                UnityEngine.Object.Destroy(NewPointer.GetComponent<Rigidbody>());
                UnityEngine.Object.Destroy(NewPointer.GetComponent<Collider>());
                UnityEngine.Object.Destroy(NewPointer, Time.deltaTime);

                GameObject line = new GameObject("Line");
                LineRenderer liner = line.AddComponent<LineRenderer>();
                liner.material.shader = Shader.Find("GUI/Text Shader");
                liner.startColor = skipRay ? new Color(0f, 0f, 0f, 1f) : isCopying ? new Color(0f, 0f, 0f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
                liner.endColor = skipRay ? new Color(0f, 0f, 0f, 1f) : isCopying ? new Color(0f, 0f, 0f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
                liner.startWidth = 0.025f;
                liner.endWidth = 0.025f;
                liner.positionCount = 2;
                liner.useWorldSpace = true;
                liner.SetPosition(0, StartPosition);
                liner.SetPosition(1, EndPosition);
                UnityEngine.Object.Destroy(line, Time.deltaTime);

                if (trigR && Ray.collider.GetComponentInParent<VRRig>() != null)
                {
                    isCopying = true;
                    whoCopy = Ray.collider.GetComponentInParent<VRRig>();

                    if (ChosenSith != whoCopy && whoCopy != null)
                    {
                        ChosenSith = whoCopy;
                    }

                    skipRay = true;
                    theRig = whoCopy;
                }
                else
                {
                    isCopying = false;
                    whoCopy = null;
                }
            }
        }

        public static ConfigEntry<bool> SelectPlayer;
        public static void BindConfigEntries()
        {
            try
            {
                SelectPlayer = Plugin.configFile.Bind(
                    section: DisplayName,
                    key: "allow telekensis gun",
                    defaultValue: false,
                    description: "Whether or not only one selected Person can throw you around"
                );
            }
            catch (Exception e) { Logging.Exception(e); }
        }

        bool SelectSith = false;
        protected override void ReloadConfiguration()
        {
            SelectSith = SelectPlayer.Value;
        }

        void TryGetSithLord()
        {
            foreach (var tk in markers)
            {
                try
                {
                    if (tk && tk.IsGripping() && tk.PointingAtMe() && (SelectSith ? tk.rig == ChosenSith : true))
                    {
                        sithLord = tk;
                        playerParticles.Play();
                        sithlordHandParticles.transform.SetParent(tk.controllingHand);
                        sithlordHandParticles.transform.localPosition = Vector3.zero;
                        sithlordHandParticles.Play();
                        sfx.Play();
                        break;
                    }
                }
                catch (Exception e)
                {
                    Logging.Exception(e);
                }
            }
        }

        void DistributeMidichlorians()
        {

            foreach (var rig in GorillaParent.instance.vrrigs)
            {
                try
                {
                    if (rig.OwningNetPlayer.IsLocal ||
                        rig.gameObject.GetComponent<TKMarker>()) continue;

                    markers.Add(rig.gameObject.AddComponent<TKMarker>());
                }
                catch (Exception e)
                {
                    Logging.Exception(e);
                }
            }
        }

        protected override void Cleanup()
        {
            foreach (TKMarker m in markers)
            {
                m?.Obliterate();
            }
            tkCollider?.gameObject?.Obliterate();
            sithlordHandParticles?.gameObject?.Obliterate();
            joint?.Obliterate();
            sithLord = null;
            markers.Clear();
            tkCollider = null;
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return "Effect: If another player points their index finger at you, they can pick you up with telekinesis.";
        }

        public class TKMarker : MonoBehaviour
        {
            public VRRig rig;
            bool grippingRight, grippingLeft;
            public Transform leftHand, rightHand, controllingHand;
            public Rigidbody controllingBody;
            DebugRay dr;

            public static int count;
            int uuid;
            void Awake()
            {
                this.rig = GetComponent<VRRig>();
                this.uuid = count++;
                leftHand = SetupHand("L");
                rightHand = SetupHand("R");
                dr = new GameObject($"{uuid} (Debug Ray)").AddComponent<DebugRay>();
            }

            public Transform SetupHand(string hand)
            {
                var handTransform = transform.Find(
                    string.Format(GestureTracker.palmPath, hand).Substring(1)
                );
                var rb = handTransform.gameObject.AddComponent<Rigidbody>();

                rb.isKinematic = true;
                return handTransform;
            }

            public bool IsGripping()
            {
                grippingRight =
                    rig.rightIndex.calcT < .5f &&
                    rig.rightMiddle.calcT > .5f;
                //rig.rightThumb.calcT > .5f;

                grippingLeft =
                    rig.leftIndex.calcT < .5f &&
                    rig.leftMiddle.calcT > .5f;
                //rig.leftThumb.calcT > .5f;
                return grippingRight || grippingLeft;
            }

            public bool PointingAtMe()
            {
                try
                {
                    if (!(grippingRight || grippingLeft)) return false;
                    Transform hand = grippingRight ? rightHand : leftHand;
                    controllingHand = hand;
                    if (!hand) return false;
                    controllingBody = hand?.GetComponent<Rigidbody>();
                    if (!controllingBody) return false;
                    RaycastHit hit;
                    Ray ray = new Ray(hand.position, hand.up);
                    Logging.Debug("DOING THE THING WITH THE COLLIDER");
                    var collider = Instance.tkCollider;
                    UnityEngine.Physics.SphereCast(ray, .2f * Player.Instance.scale, out hit, collider.gameObject.layer);
                    return hit.collider == collider;
                }
                catch (Exception e) { Logging.Exception(e); }
                return false;
            }

            void OnDestroy()
            {
                dr?.gameObject?.Obliterate();
                leftHand?.GetComponent<Rigidbody>()?.Obliterate();
                rightHand?.GetComponent<Rigidbody>()?.Obliterate();
            }
        }
    }
}
