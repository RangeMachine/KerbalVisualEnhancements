/**
 * Kerbal Visual Enhancements is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Kerbal Visual Enhancements is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 *
 * Copyright © 2013-2016 Ryan Bray, RangeMachine
 */

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Geometry;

namespace Utilities
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class OverlayMgr : MonoBehaviour
    {
        public static int MAP_LAYER = 10;
        public static int MACRO_LAYER = 15;

        static List<CelestialBody> CelestialBodyList = new List<CelestialBody>();
        static bool setup = false;
        static bool mainMenuOverlay = false;
        static bool setupCallbacks = false;
        static string CurrentBodyName = "Kerbin";
        static PQS CurrentPQS = null;
        static bool ScaledEnabled = true;

        static bool isScaledEnabled { get { return (CurrentPQS != null && CurrentPQS.isActive); } }

        public static bool MainMenuOverlay
        {
            get { return mainMenuOverlay; }
        }

        private static bool BundleLoaded = false;
        private static Dictionary<string, Shader> LoadedShaders = new Dictionary<string, Shader>();

        public static void LoadBundle()
        {
            if (BundleLoaded)
                return;

            string bundlePath = "";

            UnityEngine.Rendering.GraphicsDeviceType type = SystemInfo.graphicsDeviceType;

            if (type.ToString().Contains("Direct3D"))
                bundlePath = "DirectX.bundle";
            else if (type.ToString().Contains("OpenGL"))
                bundlePath = "OpenGL.bundle";
            else
            {
                Log("Unsupported renderer.");
                BundleLoaded = true;

                return;
            }
            
            if (!System.IO.File.Exists(KSPUtil.ApplicationRootPath + "GameData/KerbalVisualEnhancements/Shaders/" + bundlePath))
            {
                Log("Bundle '" + bundlePath + "' not found.");
                BundleLoaded = true;

                return;
            }

            using (WWW www = new WWW("file://" + KSPUtil.ApplicationRootPath + "GameData/KerbalVisualEnhancements/Shaders/" + bundlePath))
            {
                Log("Bundle '" + bundlePath + "' loaded.");
                BundleLoaded = true;

                AssetBundle bundle = www.assetBundle;
                Shader[] shaders = bundle.LoadAllAssets<Shader>();

                foreach (Shader shader in shaders)
                {
                    Log("Shader " + shader.name + " is loaded");
                    LoadedShaders.Add(shader.name, shader);
                }

                bundle.Unload(false);
                www.Dispose();
            }
        }

        public static Shader LoadShader(string name)
        {
            if (LoadedShaders.ContainsKey(name))
                return LoadedShaders[name];

            Log("Shader " + name + " not found!");

            return Shader.Find("Hidden/InternalErrorShader");
        }

        protected void Awake()
        {
            if (HighLogic.LoadedScene == GameScenes.MAINMENU)
            {
                Init();
                
            }
            else if (HighLogic.LoadedScene == GameScenes.FLIGHT && !setupCallbacks)
            {
                Log("Initializing Callbacks...");
                GameEvents.onDominantBodyChange.Add(OnDominantBodyChangeCallback);
                GameEvents.onFlightReady.Add(OnFlightReadyCallback);
                MapView.OnEnterMapView += new Callback(EnterMapView);
                MapView.OnExitMapView += new Callback(ExitMapView);
                Log("Initialized Callbacks");
                setupCallbacks = true;

            }
            else if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                EnableScaled();
            }
            else if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                UpdateRadii();
            }
        }

        private void UpdateRadii()
        {
            foreach(Overlay overlay in Overlay.OverlayList)
            {
                overlay.UpdateAltitude(true);
            }
        }

        public static void Init()
        {
            if (!setup)
            {
                UnityEngine.Object[] celestialBodies = CelestialBody.FindObjectsOfType(typeof(CelestialBody));
                foreach (CelestialBody cb in celestialBodies)
                {
                    CelestialBodyList.Add(cb);
                    if(cb.name == "Kerbin")
                    {
                        CurrentPQS = cb.pqsController;
                    }
                }

                setup = true;
            }

        }

        protected void Start()
        {
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                SetTerrainShadows();
            }

            if (HighLogic.LoadedScene == GameScenes.MAINMENU)
            {
                EnableMainOverlay();
            }
            else
            {
                 DisableMainOverlay();
            }
        }

        private void SetTerrainShadows()
        {
            bool terrainShadows = false;

            ConfigNode volumeConfig = GameDatabase.Instance.GetConfigNodes("KERBAL_VISUAL_ENHANCEMENTS")[0];
            bool.TryParse(volumeConfig.GetValue("terrainShadows"), out terrainShadows);

            if (terrainShadows)
            {
                foreach (CelestialBody body in FindObjectsOfType(typeof(CelestialBody)))
                {
                    if (body.pqsController)
                    {
                        body.pqsController.meshCastShadows = true;
                        body.pqsController.meshRecieveShadows = true;

                        QualitySettings.shadowDistance = 100000;

                        foreach (Light light in FindObjectsOfType(typeof(Light)))
                        {
                            if ((light.gameObject.name == "Scaledspace SunLight") || (light.gameObject.name == "SunLight"))
                            {
                                // light.shadowNormalBias = 0.4f;
                                // light.shadowBias = 0.125f;
                            }
                        }
                    }
                }
            }
        }

        private void OnDominantBodyChangeCallback(GameEvents.FromToAction<CelestialBody, CelestialBody> data)
        {
            UpdateCurrentBody(data.to.bodyName);
        }

        private void OnFlightReadyCallback()
        {
            UpdateCurrentBody(FlightGlobals.currentMainBody.bodyName);
        }

        private void UpdateCurrentBody(string body)
        {
            if (Overlay.OverlayDatabase.ContainsKey(CurrentBodyName))
            {
                foreach (Overlay overlay in Overlay.OverlayDatabase[CurrentBodyName])
                {
                    if (overlay.DominantCallback != null)
                    {
                        overlay.DominantCallback(false);
                    }
                    overlay.SwitchToScaled();
                }
            }
            CurrentBodyName = body;

            CelestialBody celestialBody = CelestialBodyList.First(n => n.bodyName == CurrentBodyName);
            CurrentPQS = celestialBody.pqsController;
            ScaledEnabled = !isScaledEnabled;

            if (Overlay.OverlayDatabase.ContainsKey(CurrentBodyName))
            {
                foreach (Overlay overlay in Overlay.OverlayDatabase[CurrentBodyName])
                {
                    if (overlay.DominantCallback != null)
                    {
                        bool hasPQS = (CurrentPQS != null);
                        overlay.DominantCallback(true);
                    }
                    overlay.UpdateTranform();
                }
            }
            
        }

        private void EnterMapView()
        {
            EnableScaled();
        }

        private void ExitMapView()
        {

        }

        private void EnableScaled()
        {
            foreach(Overlay overlay in Overlay.OverlayList)
            {
                overlay.SwitchToScaled();
            }
            ScaledEnabled = true;
        }

        private void DisableMainOverlay()
        {
            //nothing to do here...
            mainMenuOverlay = false;
        }

        private void EnableMainOverlay()
        {
            if (!mainMenuOverlay)
            {
                var objects = FindObjectsOfType(typeof(GameObject));
                if (objects.Any(o => o.name == "LoadingBuffer")) { return; }
                var kerbin = objects.OfType<GameObject>().Where(b => b.name == "Kerbin").LastOrDefault();
                if (kerbin != null && Overlay.OverlayDatabase.ContainsKey("Kerbin"))
                {
                    List<Overlay> overlayList = Overlay.OverlayDatabase["Kerbin"];
                    if (overlayList != null)
                    {
                        foreach (Overlay kerbinOverlay in overlayList)
                        {
                            kerbinOverlay.CloneForMainMenu();
                        }
                    }
                }
                mainMenuOverlay = true;
            }
        }

        public void Update()
        {
            if ((HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.FLIGHT ) && Overlay.OverlayDatabase.ContainsKey(CurrentBodyName))
            {
                bool inNeedOfUpdate = (isScaledEnabled && !MapView.MapIsEnabled) == ScaledEnabled;
                inNeedOfUpdate |= (!ScaledEnabled && MapView.MapIsEnabled);
                if (inNeedOfUpdate && isScaledEnabled)
                {
                    foreach (Overlay overlay in Overlay.OverlayDatabase[CurrentBodyName])
                    {
                        overlay.SwitchToMacro();
                    }
                    ScaledEnabled = false;
                }
                else if (inNeedOfUpdate)
                {
                    foreach (Overlay overlay in Overlay.OverlayDatabase[CurrentBodyName])
                    {
                        overlay.SwitchToScaled();
                    }
                    ScaledEnabled = true;
                }                
            }
        }

        public static void Log(string message)
        {
            UnityEngine.Debug.Log("Utils: " + message);
        }

        public static CelestialBody GetMapBody()
        {
            MapObject target = MapView.MapCamera.target;
            switch (target.type)
            {
                case MapObject.ObjectType.CelestialBody:
                    return target.celestialBody;
                case MapObject.ObjectType.ManeuverNode:
                    return target.maneuverNode.patch.referenceBody;
                case MapObject.ObjectType.Vessel:
                    return target.vessel.mainBody;
            }
            
            return null;
        }

        public static CelestialBody GetNextBody(CelestialBody body)
        {
            int index = CelestialBodyList.FindIndex(a => a.name == body.name);
            if (index == CelestialBodyList.Count - 1)
            {
                index = -1;
            }
            return CelestialBodyList[index + 1];
        }

        public static CelestialBody GetPreviousBody(CelestialBody body)
        {
            int index = CelestialBodyList.FindIndex(a => a.name == body.name);
            if (index == 0)
            {
                index = CelestialBodyList.Count;
            }
            return CelestialBodyList[index - 1];
        }
    }

    public class Overlay
    {
        public delegate void BoolCallback(bool value);

        public static Dictionary<string, List<Overlay>> OverlayDatabase = new Dictionary<string, List<Overlay>>();
        public static List<Overlay> OverlayList = new List<Overlay>();
        

        private GameObject OverlayGameObject;
        private string Body;
        private float altitude;
        private Material scaledMaterial;
        private Material macroMaterial;
        private int OriginalLayer;
        private bool MainMenu;
        private Vector2 Rotation;
        private Transform celestialTransform;
        private Overlay MainMenuClone;
        private CelestialBody celestialBody;
        private bool IsScaledSpace = true;
        private bool matchTerrain = false;

        public Transform Transform { get{return OverlayGameObject.transform;}}

        public float Radius { get { return altitude + (float)celestialBody.Radius; } }
        public float ScaledRadius { get { return (float)(1f + (altitude / celestialBody.Radius)); } }
        public BoolCallback MacroCallback;
        public BoolCallback DominantCallback;

        public Overlay(string planet, float altitude, Material scaledMaterial, Material macroMaterial, Vector2 rotation, int layer, Transform celestialTransform, bool mainMenu, bool matchTerrain)
        {
            MainMenu = mainMenu;
            OverlayGameObject = new GameObject();
            Body = planet;
            Rotation = rotation;
            this.scaledMaterial = scaledMaterial;
            this.macroMaterial = macroMaterial;
            OriginalLayer = layer;
            this.celestialTransform = celestialTransform;
            this.altitude = altitude;
            this.matchTerrain = matchTerrain;

            CelestialBody[] celestialBodies = (CelestialBody[])CelestialBody.FindObjectsOfType(typeof(CelestialBody));
            celestialBody = celestialBodies.First(n => n.bodyName == Body);
            
            if (!mainMenu && matchTerrain)
            {
                IsoSphere.Create(OverlayGameObject, this.altitude, celestialBody);
            }
            else
            {
                IsoSphere.Create(OverlayGameObject, Radius, null);
            }

            var mr = OverlayGameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = scaledMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;
            //mr.enabled = mainMenu;
            mr.enabled = true;

        }

        public void EnableMainMenu()
        {
            var objects = UnityEngine.Object.FindObjectsOfType(typeof(GameObject));
            if (objects.Any(o => o.name == "LoadingBuffer")) { return; }
            var body = objects.OfType<GameObject>().Where(b => b.name == Body).LastOrDefault();
            if (body != null)
            {
                OverlayGameObject.layer = body.layer;
                OverlayGameObject.transform.parent = body.transform;
                OverlayGameObject.transform.localPosition = Vector3.zero;
                OverlayGameObject.transform.localRotation = Quaternion.identity;
                OverlayGameObject.transform.localScale = (float)(1008f / celestialBody.Radius) * Vector3.one;
            }
          
        }

        public void CloneForMainMenu()
        {
            if (MainMenuClone == null)
            {
                MainMenuClone = GeneratePlanetOverlay(Body, altitude, scaledMaterial, macroMaterial, Rotation, true);
            }
        }

        public void RemoveOverlay()
        {
            OverlayDatabase[Body].Remove(this);
            OverlayList.Remove(this);
            GameObject.Destroy(OverlayGameObject);
        }

        public static Overlay GeneratePlanetOverlay(string planet, float altitude, Material scaledMaterial, Material macroMaterial, Vector2 rotation, bool mainMenu = false, bool matchTerrain = false)
        {
            Vector2 Rotation = new Vector2(rotation.x, rotation.y);
            Rotation.x += .25f;
            
            Transform celestialTransform = PSystemManager.Instance.scaledBodies.Single(t => t.name == planet).transform;

            Overlay overlay = new Overlay(planet, altitude, scaledMaterial, macroMaterial, Rotation, OverlayMgr.MAP_LAYER, celestialTransform, mainMenu, matchTerrain);
            if (!mainMenu)
            {
                if (!OverlayDatabase.ContainsKey(planet))
                {
                    OverlayDatabase.Add(planet, new List<Overlay>());
                }
                OverlayDatabase[planet].Add(overlay);
                OverlayList.Add(overlay);
            }

            if (mainMenu)
            {
                overlay.EnableMainMenu();
            }
            else 
            {
                overlay.UpdateTranform();
            }
            overlay.SetRotation(Rotation);
            return overlay;
        }

        public void UpdateTranform()
        {
            if ((celestialBody.pqsController != null && celestialBody.pqsController.isActive) && !MapView.MapIsEnabled)
            {
                SwitchToMacro();
            }
            else
            {
                SwitchToScaled();
            }
        }


        public void UpdateAltitude(bool meshUpdate, float altitude = -1)
        {
            if (altitude > 0)
            {
                this.altitude = altitude;
            }
            if (IsScaledSpace)
            {
                OverlayGameObject.transform.parent = celestialTransform;
                OverlayGameObject.transform.localPosition = Vector3.zero;
                OverlayGameObject.transform.localScale = (float)(1002f / celestialBody.Radius) * Vector3.one;
            }
            else
            {
                OverlayGameObject.transform.parent = celestialBody.transform;
                OverlayGameObject.transform.localPosition = Vector3.zero;
                OverlayGameObject.transform.localScale = Vector3.one;
            }
            if(!matchTerrain && meshUpdate)
            {
                IsoSphere.UpdateRadius(OverlayGameObject, Radius);
                OverlayMgr.Log(celestialBody.name+" radius+Altitude: " + Radius);
            }
        }

        public void UpdateRotation(Vector3 rotation)
        {
            float tmp = rotation.x;
            rotation.x = rotation.y;
            rotation.y = tmp;
            OverlayGameObject.transform.Rotate(360f * rotation);

            if(MainMenuClone != null)
            {
                if (MainMenuClone.OverlayGameObject != null)
                {
                    MainMenuClone.UpdateRotation(rotation);
                }
                else
                {
                    MainMenuClone = null;
                }
            }
        }

        public void SetRotation(Vector3 vector3)
        {
            Vector3 rot = new Vector3(vector3.y, vector3.x, vector3.z);
            OverlayGameObject.transform.localRotation = Quaternion.Euler(360f * rot);
        }

        internal void UpdateAlpha(float alpha)
        {
            scaledMaterial.SetFloat("_Opacity", alpha);
            macroMaterial.SetFloat("_Opacity", 1.0f - alpha);
        }

        internal void SwitchToScaled()
        {
            OverlayGameObject.GetComponent<MeshRenderer>().sharedMaterial = scaledMaterial;
            OverlayGameObject.layer = OverlayMgr.MAP_LAYER;
            IsScaledSpace = true;
            UpdateAltitude(false);
            if (MacroCallback != null)
            { MacroCallback(false); }
           
        }

        internal void SwitchToMacro()
        {
            OverlayGameObject.GetComponent<MeshRenderer>().sharedMaterial = macroMaterial;
            OverlayGameObject.layer = OverlayMgr.MACRO_LAYER;
            IsScaledSpace = false;
            UpdateAltitude(false);
            if (MacroCallback != null)
            { MacroCallback(true); }
        }

    }


}
