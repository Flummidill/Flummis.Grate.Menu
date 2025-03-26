using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using Fusion;
using GorillaLocomotion;
using GorillaLocomotion.Swimming;
using GorillaNetworking;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Modules;
using Grate.Modules.Multiplayer;
using Grate.Networking;
using Grate.Tools;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

namespace Grate
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]

    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        public static bool initialized, injected, inRoom;
        bool pluginEnabled = false;
        public static AssetBundle assetBundle;
        public static MenuController menuController;
        public static GameObject monkeMenuPrefab;
        public static ConfigFile configFile;

        public static bool IsSteam { get; protected set; }
        public static bool DebugMode { get; protected set; } = false;
        GestureTracker gt;
        NetworkPropertyHandler nph;
        public static GameObject Water;

        public void Setup()
        {
            if (menuController || !pluginEnabled || !inRoom)
            Logging.Debug("Menu:", menuController, "Plugin Enabled:", pluginEnabled, "InRoom:", inRoom);
            try
            {
                gt = this.gameObject.GetOrAddComponent<GestureTracker>();
                nph = this.gameObject.GetOrAddComponent<NetworkPropertyHandler>();  
                menuController = Instantiate(monkeMenuPrefab).AddComponent<MenuController>();
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        public void Cleanup()
        {
            try
            {
                Logging.Debug("Cleaning up");
                menuController?.gameObject?.Obliterate();
                gt?.Obliterate();
                nph?.Obliterate();
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }



        void Awake()
        {
            try
            {
                Instance = this;
                Logging.Init();
                configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "Grate.cfg"), true);
                Logging.Debug("Found", GrateModule.GetGrateModuleTypes().Count, "modules");
                foreach (Type moduleType in GrateModule.GetGrateModuleTypes())
                {
                    MethodInfo bindConfigs = moduleType.GetMethod("BindConfigEntries");
                    if (bindConfigs != null)
                    {
                        bindConfigs.Invoke(null, null);
                    }
                }
                MenuController.BindConfigEntries();
            }
            catch (Exception e) { Logging.Exception(e); }
        }

        void Start()
        {
            try
            {
                GorillaTagger.OnPlayerSpawned(OnGameInitialized);
                assetBundle = AssetUtils.LoadAssetBundle("Grate/Resources/gratebundle");
                monkeMenuPrefab = assetBundle.LoadAsset<GameObject>("Bark Menu");

            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        public static Text debugText;
        void CreateDebugGUI()
        {
            try
            {
                if (GTPlayer.Instance)
                {
                    var canvas = GTPlayer.Instance.headCollider.transform.GetComponentInChildren<Canvas>();
                    if (!canvas)
                    {
                        canvas = new GameObject("~~~Grate Debug Canvas").AddComponent<Canvas>();
                        canvas.renderMode = RenderMode.WorldSpace;
                        canvas.transform.SetParent(GTPlayer.Instance.headCollider.transform);
                        canvas.transform.localPosition = Vector3.forward * .35f;
                        canvas.transform.localRotation = Quaternion.identity;
                        canvas.transform.localScale = Vector3.one;
                        canvas.gameObject.AddComponent<CanvasScaler>();
                        canvas.gameObject.AddComponent<GraphicRaycaster>();
                        canvas.GetComponent<RectTransform>().localScale = Vector3.one * .035f;
                        var text = new GameObject("~~~Text").AddComponent<Text>();
                        text.transform.SetParent(canvas.transform);
                        text.transform.localPosition = Vector3.zero;
                        text.transform.localRotation = Quaternion.identity;
                        text.transform.localScale = Vector3.one;
                        text.color = Color.green;
                        //text.text = "Hello World";
                        text.fontSize = 24;
                        text.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
                        text.alignment = TextAnchor.MiddleCenter;
                        text.horizontalOverflow = HorizontalWrapMode.Overflow;
                        text.verticalOverflow = VerticalWrapMode.Overflow;
                        text.color = Color.white;
                        text.GetComponent<RectTransform>().localScale = Vector3.one * .02f;
                        debugText = text;
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        void OnEnable()
        {
            try
            {
                Logging.Debug("OnEnable");
                this.pluginEnabled = true;
                HarmonyPatches.ApplyHarmonyPatches();
                if (initialized)
                    Setup();
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        void OnDisable()
        {
            try
            {
                Logging.Debug("OnDisable");
                this.pluginEnabled = false;
                HarmonyPatches.RemoveHarmonyPatches();
                Cleanup();
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        void OnGameInitialized()
        {
            try
            {
                Logging.Debug("OnGameInitialized");
                initialized = true;
                PlatformTagJoin platform = (PlatformTagJoin)Traverse.Create(GorillaNetworking.PlayFabAuthenticator.instance).Field("platform").GetValue();
                Logging.Info("Platform: ", platform);
                IsSteam = platform.PlatformTag.Contains("Steam");

                NetworkSystem.Instance.OnJoinedRoomEvent += JoinRoom;

                if (DebugMode)
                    CreateDebugGUI();
            }
            catch (Exception ex)
            {
                Logging.Exception(ex);
            }
        }

        private void JoinRoom()
        {
            if (!inRoom)
            {
                inRoom = true;
                Setup();
            }
        }

        public void JoinLobby(string name)
        {
            StartCoroutine(JoinLobbyInternal(name));
        }

        IEnumerator JoinLobbyInternal(string name)
        {
            do
            {
                yield return new WaitForSeconds(1f);
                Logging.Debug("Waiting to disconnect");
            }
            while (PhotonNetwork.InRoom);

            if (!GorillaComputer.instance.currentGameMode.Value.Contains("MODDED"))
            {
                GorillaComputer.instance.currentGameMode.Value += "_MODDED";
            }
            PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(name,JoinType.Solo);
        }
    }
}