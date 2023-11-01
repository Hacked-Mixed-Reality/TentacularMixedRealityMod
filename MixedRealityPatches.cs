using HarmonyLib;
using Il2CppValve.VR;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

namespace MixedRealityMod
{
    public class MixedRealityPatches : MelonMod
    {


        [HarmonyPatch(typeof(Il2CppValve.VR.SteamVR_ExternalCamera), "ReadConfig")]
        static class ReadConfigPatch
        {
            public static bool Prefix()
            {
                MelonLogger.Msg("Reading ExternalCamera.cfg");
                FixTransforms();
                //FixLayers();

                return true;
            }

        }


        [HarmonyPatch(typeof(Il2CppValve.VR.SteamVR_ExternalCamera), "OnEnable")]
        static class ExternalCameraOnEnablePatch
        {
            public static bool Prefix(ref SteamVR_ExternalCamera __instance)
            {
                MelonLogger.Msg("Fix Picasso");
                if (__instance == null) { MelonLogger.Msg(20); }
                __instance.AutoEnableActionSet();

                return false;
            }

        }
        

        [HarmonyPatch(typeof(Il2CppValve.VR.SteamVR_Render), "RenderExternalCamera")]
        static class SteamVR_Render_Patches
        {
            public static bool Prefix()
            {
                if (Camera.main == null)
                {
                    var cameras = GameObject.FindObjectsOfType<Camera>();
                    foreach (var camera in cameras)
                    {
                        if (camera.name == "Camera")
                        {
                            camera.tag = "MainCamera";
                            MelonLogger.Msg("Fixing Camera (Total {0})", cameras.Length);
                            break;
                        }
                    }
                    return false;
                }
                else return true;
            }
        }



        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            base.OnSceneWasInitialized(buildIndex, sceneName);
            FixTransforms();

            //ShowDiagnostics();
        }


        private static void FixTransforms()
        {
            MelonLogger.Msg("Finding Erroneous Transforms");

            var trackedPose = SteamVR_Render.instance.externalCamera.gameObject.GetComponentInChildren<UnityEngine.SpatialTracking.TrackedPoseDriver>();
            if (trackedPose != null)
            {
                MelonLogger.Msg("Removing Erroneous Transforms");
                UnityEngine.Object.Destroy(trackedPose);
            }
            var transforms = SteamVR_Render.instance.externalCamera.gameObject.GetComponentsInChildren<UnityEngine.Transform>();
            foreach (var transform in transforms)
            {
                transform.localPosition = Vector3.zero;
                transform.localEulerAngles = Vector3.zero;
            }
        }


        private static void FixLayers()
        {
            SteamVR_ExternalCamera? instance = SteamVR_Render.instance?.externalCamera;
            if (instance != null)
            {
                instance.cam.cullingMask = ModLayers.GetGameLayerMask();

            }
        }


        public override void OnUpdate()
        {
            //TODO: Following section allows for updating ExternalCamera.cfg during play
            //This is currently causing the external camera tracking to pick the headset as the tracked object somehow.

            /*
            if (Time.frameCount % 80 == 0)
            {

                if (File.Exists(MelonEnvironment.MelonBaseDirectory + "/updateexternalcamera"))
                {
                    try
                    {
                        SteamVR_Render.instance.externalCamera.ReadConfig();
                        File.Delete(MelonEnvironment.MelonBaseDirectory + "/updateexternalcamera");
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Msg(e.Message);
                    }
                }


            }
            */
            base.OnUpdate();
        }


        public enum GameLayer
        {

            Default = 0,
            TransparentFX = 1,
            IgnoreRaycast = 2,
            Water = 4,
            UI = 5,
            rig_legacy_steamVR_free = 8,
            LeftTentacle = 9,
            RightTentacle = 10,
            Energy = 11,
            noCollision = 12,
            onlyIgnoreExplosion = 13,
            unused_RightSuckers = 14,
            ignoreExplosion = 15,
            Tentaclofobic = 16,
            Vehicle = 17,
            Teleport = 18,
            NonGluey = 19,
            PhotoCameraInvisible = 20,
            PhotoCameraOnlyVisible = 21,
            Crystal = 22,
            Laser = 23,
            Mirror = 24,
            NoDamage = 25,
            AgainstEachOther = 26,
            Magnet = 27,
        }


        public class ModLayers
        {
            public static LayerMask GetGameLayerMask()
            {
                LayerMask layerMask = ~0;
                layerMask &= ~(1 << (int)GameLayer.IgnoreRaycast); //Some lighting
                //layerMask &= ~(1 << (int)GameLayer.FadeManager);
                //layerMask &= ~(1 << (int)GameLayer.VRRenderingOnly);
                return layerMask;
            }
        }



        private static void ShowDiagnostics()
        {
            Camera[] cameras = GameObject.FindObjectsOfType<Camera>();
            foreach (Camera cam in cameras)
            {
                if (cam != null)
                {
                    MelonLogger.Msg("Camera {0} has mask {1} and tag {2}", cam.name, cam.cullingMask, cam.tag);
                }
                else
                {
                    MelonLogger.Msg("There's a null camera...");
                }
            }
            for (int i = 0; i < 32; i++)
            {
                MelonLogger.Msg("Layer:{1} = {0},", i.ToString(), LayerMask.LayerToName(i));

            }
        }


        private static void PrintCameraHierarchy()
        {
            var cam = SteamVR_Render.instance.externalCamera.cam;
            MelonLogger.Msg("Camera {0} has parent {1}", cam.gameObject.name, cam.transform.parent.gameObject.name);

            var gameObject = cam.gameObject;
            while (gameObject != null)
            {
                MelonLogger.Msg("Object {0} has parent {1}", gameObject.gameObject.name, gameObject.transform.parent?.gameObject.name);
                gameObject = gameObject.transform.parent?.gameObject;
            }
        }
    }
}