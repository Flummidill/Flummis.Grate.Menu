using HarmonyLib;
using GorillaLocomotion;
using System;
using Grate.Tools;
using Grate.Modules.Physics;
using UnityEngine;
using Grate.Gestures;
using Photon.Pun;
using System.Collections.Generic;
using System.Reflection;
using Grate.Extensions;

namespace Grate.Patches
{
    [HarmonyPatch]
    public class VRRigCachePatches
    {
        public static Action<NetPlayer, VRRig> OnRigCached;

        static IEnumerable<MethodBase> TargetMethods()
        {
            return new MethodBase[] {
                AccessTools.Method("VRRigCache:RemoveRigFromGorillaParent")
            };
        }

        private static void Prefix(NetPlayer player, VRRig vrrig)
        {
            OnRigCached?.Invoke(player, vrrig);
        }
    }
}
