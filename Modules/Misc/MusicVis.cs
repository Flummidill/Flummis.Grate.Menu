using Fusion;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.GUI;
using Grate.Networking;
using Grate.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Grate.Modules.Misc
{
    class MusicVis : GrateModule
    {
        VisMarker Marker;
        public override string GetDisplayName()
        {
            return "Music Vis";
        }

        public override string Tutorial()
        {
            return "Graze Proof, I love music visualising";
        }

        protected override void Cleanup()
        {
            Marker.Obliterate();
        }

        protected override void OnDisable()
        {
            Destroy(Marker);
        }

        void Awake()
        {   
            NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
            Patches.VRRigCachePatches.OnRigCached += OnRigCached;
        }

        private void OnRigCached(NetPlayer player, VRRig rig)
        {
            rig?.gameObject?.GetComponent<VisMarker>()?.Obliterate();
        }

        private void OnPlayerModStatusChanged(NetPlayer player, string mod, bool enabled)
        {
            if (mod == GetDisplayName() && player.UserId == "E5F14084F14ED3CE")
            {
                if (enabled)
                {
                    player.Rig().gameObject.GetOrAddComponent<VisMarker>();
                }
                else
                {
                    Destroy(player.Rig().gameObject.GetComponent<VisMarker>());

                }
            }
        }

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            try
            {
                Marker = GorillaTagger.Instance.offlineVRRig.gameObject.AddComponent<VisMarker>();
            }
            catch (Exception e) { Logging.Exception(e); }
        }
    }

    class VisMarker : MonoBehaviour
    {
        List<Transform> VisParts;
        Transform anc;
        GorillaSpeakerLoudness Speakerloudness;
        VRRig rig;

        void Start()
        {
            rig = GetComponent<VRRig>();
            anc = new GameObject("Vis").transform;
            VisParts = new List<Transform>();
            for (int i = 0; i < 50; i++)
            {
                GameObject wawa = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                wawa.GetComponent<Collider>().Obliterate();
                wawa.GetComponent<Renderer>().material = MenuController.Instance.grate[1];
                wawa.transform.SetParent(anc, false);
                wawa.transform.localScale = new Vector3(0.11612f, 0.11612f, 0.11612f);
                VisParts.Add(wawa.transform);
                Debug.Log($"{i} shperes made");
            }
        }

        void FixedUpdate()
        {
            if (Speakerloudness == null)
            {
                Speakerloudness = rig.GetComponent<GorillaSpeakerLoudness>();
            }
            if (anc.parent == null)
            { anc.SetParent(rig.transform, false); }
            else if (VisParts.Count == 50)
            {
                int count = VisParts.Count;
                float num = 360f / count;
                float currentLoudness = Speakerloudness.SmoothedLoudness;
                Vector3 position = anc.transform.position;
                for (int i = 0; i < count; i++)
                {
                    float num2 = i * num;
                    float x = currentLoudness * Mathf.Cos(num2 * 0.017453292f);
                    float z = currentLoudness * Mathf.Sin(num2 * 0.017453292f);
                    Vector3 vector = position + new Vector3(x, 0.2f, z);
                    float y = vector.y + currentLoudness;
                    Vector3 position2 = new Vector3(vector.x, y, vector.z);
                    VisParts[i].transform.position = position2;
                    VisParts[i].transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                }
            }
        }
        void OnDestory()
        {
            anc.Obliterate();
        }
        void OnDisable()
        {
            anc.Obliterate();
        }
    }
}
