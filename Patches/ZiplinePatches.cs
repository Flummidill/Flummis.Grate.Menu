using HarmonyLib;
using GorillaLocomotion;
using System;
using Grate.Tools;
using Grate.Modules.Physics;
using UnityEngine;
using GorillaLocomotion.Gameplay;
using GorillaLocomotion.Climbing;
using Grate.Modules.Movement;

namespace Grate.Patches
{
    [HarmonyPatch(typeof(GorillaZipline))]
    [HarmonyPatch("Update", MethodType.Normal)]
    public class ZiplineUpdatePatch
    {
        private static void Postfix(GorillaZipline __instance, BezierSpline ___spline, float ___currentT, 
            GorillaHandClimber ___currentClimber)
        {
            if(!Plugin.inRoom) return;
            try
            {
                var rockets = Rockets.Instance;
                if (!rockets || !rockets.enabled || !___currentClimber) return;
                Vector3 curDir = __instance.GetCurrentDirection();
                Vector3 rocketDir = rockets.AddedVelocity();
                var currentSpeed = Traverse.Create(__instance).Property("currentSpeed");
                float speedDelta = Vector3.Dot(curDir, rocketDir) * Time.deltaTime * rocketDir.magnitude * 1000f;
                currentSpeed.SetValue(currentSpeed.GetValue<float>() + speedDelta);
            }
            catch (Exception e) { Logging.Exception(e); }
        }
    }
}
