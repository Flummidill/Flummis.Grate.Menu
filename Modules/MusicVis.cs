using GorillaLocomotion;
using Grate.Extensions;
using Grate.GUI;
using Grate.Modules.Misc;
using Grate.Networking;
using Grate.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Grate.Modules
{
    class MusicVis : GrateModule
    {
        public static GameObject visPrefab;
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

        void Awake()
        {
            if (!visPrefab)
            {
                visPrefab = Plugin.assetBundle.LoadAsset<GameObject>("musicVis");
            }
            NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
            Patches.VRRigCachePatches.OnRigCached += OnRigCached;
        }

        private void OnRigCached(Player player, VRRig rig)
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
        GameObject Vis;

        List<Transform> VisParts;
        Transform anc;
        GorillaSpeakerLoudness Speakerloudness;

        void Start()
        {
            Vis =  Instantiate(MusicVis.visPrefab);
            var rig = this.GetComponent<VRRig>();
            Vis.transform.SetParent(rig.headMesh.transform, false);
            anc = Vis.transform;
            Speakerloudness = rig.GetComponent<GorillaSpeakerLoudness>();
        }

        void FixedUpdate()
        {
            int count = VisParts.Count;
            float num = 360f / (float)count;
            float currentLoudness = Speakerloudness.SmoothedLoudness;
            Vector3 position = anc.transform.position;
            for (int i = 0; i < count; i++)
            {
                float num2 = (float)i * num;
                float x = currentLoudness * Mathf.Cos(num2 * 0.017453292f);
                float z = currentLoudness * Mathf.Sin(num2 * 0.017453292f);
                Vector3 vector = position + new Vector3(x, 0.2f, z);
                VisParts[i].transform.position = vector;
                VisParts[i].transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                float y = vector.y + currentLoudness;
                Vector3 position2 = new Vector3(vector.x, y, vector.z);
                VisParts[i].transform.position = position2;
            }
        }
        void OnDestory()
        {
            Vis.Obliterate();
        }
        void OnDisable()
        {
            Vis.Obliterate();
        }
    }
}
