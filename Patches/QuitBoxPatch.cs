using GorillaLocomotion;
using HarmonyLib;
using UnityEngine;
using Grate.Extensions;

namespace Grate.Patches
{
    [HarmonyPatch(typeof(GorillaQuitBox))]
    [HarmonyPatch("OnBoxTriggered", MethodType.Normal)]
    class QuitBoxPatch
    {
        private static bool Prefix()
        {
            GTPlayer.Instance.TeleportTo(new Vector3(-66.4845f, 11.7564f, -82.5688f), Quaternion.Euler(Vector3.zero));
            foreach (var wawa in GorillaNetworking.PhotonNetworkController.Instance.enableOnStartup)
            {
                wawa.SetActive(true);
            }
            foreach (var wawa2 in GorillaNetworking.PhotonNetworkController.Instance.disableOnStartup)
            {
                wawa2.SetActive(false);
            }
            return !Plugin.inRoom;
        }
    }
}