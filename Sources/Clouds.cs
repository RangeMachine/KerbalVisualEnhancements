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
 * Copyright © 2013-2017 Ryan Bray, RangeMachine
 */

using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using Utilities;

namespace Clouds
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class Clouds : MonoBehaviour
    {
        static bool Loaded = false;
        static KeyCode GUI_KEYCODE = KeyCode.N;

        static bool useEditor = false;
        static bool AdvancedGUI = false;
        static Vector2 ScrollPosLayerList = Vector2.zero;
        static int SelectedLayer = 0;
        static int SelectedConfig = 0;
        //static bool CameraInitialized = false;
        static CloudGUI CloudGUI = new CloudGUI();
        static CelestialBody currentBody = null;
        static CelestialBody oldBody = null;
        static List<UrlDir.UrlConfig> ConfigNodeList = new List<UrlDir.UrlConfig>();

        private void loadCloudLayers(bool defaults)
        {
            foreach (CloudLayer cl in CloudLayer.Layers)
            {
                cl.Remove(false);
            }
            CloudLayer.Layers.Clear();

            UrlDir.UrlConfig[] packLayersConfigs = GameDatabase.Instance.GetConfigs("CLOUD_LAYER_PACK");
            foreach (UrlDir.UrlConfig node in packLayersConfigs)
            {
                ConfigNodeList.Add(node);
                bool useVolume = false;
                bool.TryParse(node.config.GetValue("volume"), out useVolume);
                foreach (ConfigNode configNode in node.config.nodes)
                {
                    LoadConfigNode(configNode, node.url, useVolume, defaults);
                }
                
            }
        }

        private void LoadConfigNode(ConfigNode node, string url, bool useVolume, bool defaults)
        {
            ConfigNode loadNode = node.GetNode("SAVED");
            if ((loadNode == null || defaults) && node.HasNode("DEFAULTS"))
            {
                loadNode = node.GetNode("DEFAULTS");
                loadNode.RemoveValue("REMOVED");
            }
            else if( node.HasValue("REMOVED") && bool.Parse(node.GetValue("REMOVED")))
            {
                return;
            }
            else if (defaults && !node.HasNode("DEFAULTS"))
            {
                node.AddValue("REMOVED", true);
                return;
            }

            string body = loadNode.GetValue("body");
            Transform bodyTransform = null;
            try
            {
                bodyTransform = PSystemManager.Instance.scaledBodies.Single(t => t.name == body).transform;
            }
            catch
            {

            }
            if (bodyTransform != null)
            {
                float altitude = float.Parse(loadNode.GetValue("altitude"));

                TextureSet mTexture = new TextureSet(loadNode.GetNode("main_texture"), false);
                TextureSet dTexture = new TextureSet(loadNode.GetNode("detail_texture"), false);
                string particleTop = loadNode.GetValue("particle_top_texture");
                string particleLeft = loadNode.GetValue("particle_left_texture");
                string particleFront = loadNode.GetValue("particle_front_texture");
                
                float particleDistance = 0;
                float.TryParse(loadNode.GetValue("particle_distance"), out particleDistance);
              
                ConfigNode floatsConfig = loadNode.GetNode("shader_floats");
                ShaderFloats shaderFloats = null;
                if (floatsConfig != null)
                {
                    shaderFloats = new ShaderFloats(floatsConfig);
                }
                ConfigNode scaledfloatsConfig = loadNode.GetNode("scaled_shader_floats");
                ShaderFloats scaledShaderFloats = null;
                if (scaledfloatsConfig != null)
                {
                    scaledShaderFloats = new ShaderFloats(scaledfloatsConfig);
                }
                ConfigNode colorNode = loadNode.GetNode("color");
                Color color = new Color(
                    float.Parse(colorNode.GetValue("r")),
                    float.Parse(colorNode.GetValue("g")),
                    float.Parse(colorNode.GetValue("b")),
                    float.Parse(colorNode.GetValue("a")));
                if (useVolume)
                {
                    bool.TryParse(loadNode.GetValue("volume"), out useVolume);
                }

                CloudLayer.Layers.Add(
                    new CloudLayer(url, node, body, color, altitude,
                    mTexture, dTexture, particleTop, particleLeft, particleFront, particleDistance, scaledShaderFloats, shaderFloats, useVolume));
            }
            else
            {
                CloudLayer.Log("body " + body + " does not exist!");
            }
        }

        private void saveCloudLayers()
        {
            foreach (KeyValuePair<string, List<CloudLayer>> cloudList in CloudLayer.BodyDatabase.ToArray())
            {
                string body = cloudList.Key;
                List<CloudLayer> list = cloudList.Value;
                foreach (CloudLayer cloudLayer in list)
                {
                    ConfigNode saveNode = cloudLayer.ConfigNode.GetNode("SAVED");
                    if(saveNode == null)
                    {
                        saveNode = cloudLayer.ConfigNode.AddNode("SAVED");
                    }
                    
                    saveNode.ClearData();
                    saveNode.AddValue("body", body);
                    saveNode.AddValue("altitude", cloudLayer.Altitude.ToString());
                    saveNode.AddValue("volume", cloudLayer.UseVolume);
                    ConfigNode colorNode = saveNode.AddNode("color");
                    colorNode.AddValue("r", cloudLayer.Color.r.ToString());
                    colorNode.AddValue("g", cloudLayer.Color.g.ToString());
                    colorNode.AddValue("b", cloudLayer.Color.b.ToString());
                    colorNode.AddValue("a", cloudLayer.Color.a.ToString());
                    saveNode.AddNode(cloudLayer.MainTexture.GetNode("main_texture"));
                    ConfigNode detailNode = cloudLayer.DetailTexture.GetNode("detail_texture");
                    if (detailNode != null)
                    {
                        saveNode.AddNode(detailNode);
                    }

                    saveNode.AddValue("particle_top_texture", cloudLayer.ParticleTopTexture);
                    saveNode.AddValue("particle_left_texture", cloudLayer.ParticleLeftTexture);
                    saveNode.AddValue("particle_front_texture", cloudLayer.ParticleFrontTexture);
                    saveNode.AddValue("particle_distance", cloudLayer.ParticleDistance);

                    ConfigNode scaledShaderFloatNode = cloudLayer.ScaledShaderFloats.GetNode("scaled_shader_floats");
                    if (!CloudLayer.IsDefaultShaderFloat(cloudLayer.ScaledShaderFloats, true))
                    {
                        saveNode.AddNode(scaledShaderFloatNode);
                    }
                    ConfigNode shaderFloatNode = cloudLayer.ShaderFloats.GetNode("shader_floats");
                    if (!CloudLayer.IsDefaultShaderFloat(cloudLayer.ShaderFloats, false))
                    {
                        saveNode.AddNode(shaderFloatNode);
                    }
                }
            }
            UrlDir.UrlConfig[] packLayersConfigs = GameDatabase.Instance.GetConfigs("CLOUD_LAYER_PACK");
            foreach (UrlDir.UrlConfig node in packLayersConfigs)
            {
                List<ConfigNode> remove = new List<ConfigNode>();
                foreach(ConfigNode config in node.config.nodes)
                {
                    if(config.HasValue("REMOVED") && bool.Parse(config.GetValue("REMOVED")) &&
                        !config.HasNode("DEFAULTS"))
                    {
                        remove.Add(config);
                    }
                }
                foreach(ConfigNode config in remove)
                {
                    node.config.nodes.Remove(config);
                }
                node.parent.SaveConfigs();
            }
        }
        
        protected void Awake()
        {
            if (HighLogic.LoadedScene == GameScenes.MAINMENU && !Loaded)
            {

                OverlayMgr.Init();
                loadCloudLayers(false);
                Loaded = true;
            }
        }

        protected void Update()
        {
            try
            {
                if (HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION ||
                    HighLogic.LoadedScene == GameScenes.MAINMENU || HighLogic.LoadedScene == GameScenes.SPACECENTER)
                {
                    foreach (CloudLayer layer in CloudLayer.Layers)
                    {
                        layer.PerformUpdate();
                    }

                    bool alt = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
                    if (alt && Input.GetKeyDown(GUI_KEYCODE))
                    {
                        useEditor = !useEditor;
                    }
                }

                if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                {
                    if (FlightGlobals.ActiveVessel != null && CloudLayer.BodyDatabase.ContainsKey(FlightGlobals.currentMainBody.name))
                    {
                        Vector3 COM = FlightGlobals.ActiveVessel.GetWorldPos3D();
                        foreach (CloudLayer layer in CloudLayer.BodyDatabase[FlightGlobals.currentMainBody.name])
                        {
                            layer.UpdateParticleClouds(COM);

                            layer.CloudMaterial.renderQueue = 3000;

                            if (!MapView.MapIsEnabled)
                                layer.CloudMaterial.renderQueue = 3001;
                        }
                    }
                }
                else if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
                {
                    if (CloudLayer.BodyDatabase.ContainsKey(FlightGlobals.currentMainBody.name))
                    {
                        foreach (CloudLayer layer in CloudLayer.BodyDatabase[FlightGlobals.currentMainBody.name])
                        {
                            layer.UpdateParticleClouds(GameObject.Find("KSC").transform.position);
                            layer.CloudMaterial.renderQueue = 3001;
                        }
                    }
                }
            }
            catch (NullReferenceException) { }
        }


        private Rect _mainWindowRect = new Rect(20, 20, 400, 600);

        private void OnGUI()
        {
            GUI.skin = HighLogic.Skin;
            CelestialBody current = null;
            if (MapView.MapIsEnabled)
            {
                current = OverlayMgr.GetMapBody();
            }
            else
            {
                current = FlightGlobals.currentMainBody;
            }
            if (useEditor && current != null)
            {

                if (AdvancedGUI)
                {
                    _mainWindowRect.width = 700;
                }
                else
                {
                    _mainWindowRect.width = 350;
                }
                if (CloudLayer.GetBodyLayerCount(ConfigNodeList[SelectedConfig].url, current.name) != 0)
                {
                    if (CloudGUI.DetailTexture.InUse && CloudGUI.UseVolume)
                        _mainWindowRect.height = 865;
                    else if (CloudGUI.DetailTexture.InUse)
                        _mainWindowRect.height = 740;
                    else if (CloudGUI.UseVolume)
                        _mainWindowRect.height = 755;
                    else
                        _mainWindowRect.height = 630;

                    _mainWindowRect = GUI.Window(0x8100, _mainWindowRect, DrawMainWindow, "Kerbal Visual Enhancements");
                }
                else
                {
                    _mainWindowRect.height = 115;
                    _mainWindowRect = GUI.Window(0x8100, _mainWindowRect, DrawMainWindow, "Kerbal Visual Enhancements");
                }
            }
            
        }

        private void DrawMainWindow(int windowID)
        {
            oldBody = currentBody;
            currentBody = null;
            if (MapView.MapIsEnabled)
            {
                currentBody = OverlayMgr.GetMapBody();
            }
            else
            {
                currentBody = FlightGlobals.currentMainBody;
            }
            if (currentBody != null)
            {
                GUIStyle gs = new GUIStyle(GUI.skin.label);
                gs.alignment = TextAnchor.MiddleCenter;

                AdvancedGUI = GUI.Toggle(
                        new Rect(10, 110, 125, 25), AdvancedGUI, "Advanced Settings");
                float itemFullWidth = AdvancedGUI ? (_mainWindowRect.width / 2) - 20 : _mainWindowRect.width - 20;

                GUI.Label(new Rect(35, 20, itemFullWidth - 50, 25), currentBody.name, gs);

                if (MapView.MapIsEnabled)
                {
                    if (GUI.Button(new Rect(10, 20, 25, 25), "<"))
                    {
                        MapView.MapCamera.SetTarget(OverlayMgr.GetPreviousBody(currentBody).name);
                    }
                    if (GUI.Button(new Rect(itemFullWidth - 15, 20, 25, 25), ">"))
                    {
                        MapView.MapCamera.SetTarget(OverlayMgr.GetNextBody(currentBody).name);
                    }
                }
                float halfWidth = (itemFullWidth / 2) - 5;

                if (GUI.Button(new Rect(10, 50, halfWidth, 25), "Reset to Save"))
                {
                    loadCloudLayers(false);
                    oldBody = null;
                }
                if (GUI.Button(new Rect(halfWidth + 20, 50, halfWidth, 25), "Reset to Default"))
                {
                    loadCloudLayers(true);
                    oldBody = null;
                }

                bool selectedConfigChanged = false;
                if (GUI.Button(new Rect(itemFullWidth - 15, 80, 25, 25), ">"))
                {
                    SelectedConfig++;
                    if(ConfigNodeList.Count <= SelectedConfig)
                    {
                        SelectedConfig = 0;
                    }
                    selectedConfigChanged = true;
                }

                string configUrl = ConfigNodeList[SelectedConfig].url;
                GUI.Button(new Rect(10, 80, itemFullWidth-30, 25), ConfigNodeList[SelectedConfig].parent.url);
                

                int layerCount = CloudLayer.GetBodyLayerCount(configUrl, currentBody.name);
                bool hasLayers = layerCount != 0;

                halfWidth = hasLayers ? (itemFullWidth / 2) - 5 : itemFullWidth;
                if (GUI.Button(new Rect(10, 140, halfWidth, 25), "Add"))
                {
                    ConfigNode newNode = new ConfigNode("CLOUD_LAYER");
                    ConfigNodeList.First(n => n.url == configUrl).config.AddNode(newNode);
                    CloudLayer.Layers.Add(
                    new CloudLayer(configUrl, newNode, currentBody.name, new Color(1, 1, 1, 1), 1000f,
                    new TextureSet(true), new TextureSet(), "", "", "", 0, null, null, false));
                }
                if (hasLayers)
                {

                    GUI.Box(new Rect(10, 170, itemFullWidth, 115), "");
                    string[] layerList = CloudLayer.GetBodyLayerStringList(configUrl, currentBody.name);
                    ScrollPosLayerList = GUI.BeginScrollView(new Rect(15, 175, itemFullWidth - 10, 100), ScrollPosLayerList, new Rect(0, 0, itemFullWidth - 30, 25 * layerList.Length));
                    float layerWidth = layerCount > 4 ? itemFullWidth - 30 : itemFullWidth - 10;
                    int OldSelectedLayer = SelectedLayer;
                    SelectedLayer = SelectedLayer >= layerCount || SelectedLayer< 0 ? 0 : SelectedLayer;
                    SelectedLayer = GUI.SelectionGrid(new Rect(0, 0, layerWidth, 25 * layerList.Length), SelectedLayer, layerList, 1);
                    GUI.EndScrollView();

                    if (GUI.Button(new Rect(halfWidth + 20, 140, halfWidth, 25), "Remove"))
                    {
                        CloudLayer.RemoveLayer(configUrl, currentBody.name, SelectedLayer);
                        SelectedLayer = -1;
                        return;
                    }

                    if (SelectedLayer != OldSelectedLayer || currentBody != oldBody || selectedConfigChanged)
                    {
                        if (CloudLayer.ConfigBodyDatabase[configUrl].ContainsKey(currentBody.name) && CloudLayer.ConfigBodyDatabase[configUrl][currentBody.name].Count > SelectedLayer)
                        {
                            CloudGUI.MainTexture.Clone(CloudLayer.ConfigBodyDatabase[configUrl][currentBody.name][SelectedLayer].MainTexture);
                            CloudGUI.DetailTexture.Clone(CloudLayer.ConfigBodyDatabase[configUrl][currentBody.name][SelectedLayer].DetailTexture);
                            CloudGUI.ParticleTopTexture = CloudLayer.ConfigBodyDatabase[configUrl][currentBody.name][SelectedLayer].ParticleTopTexture;
                            CloudGUI.ParticleLeftTexture = CloudLayer.ConfigBodyDatabase[configUrl][currentBody.name][SelectedLayer].ParticleLeftTexture;
                            CloudGUI.ParticleFrontTexture = CloudLayer.ConfigBodyDatabase[configUrl][currentBody.name][SelectedLayer].ParticleFrontTexture;
                            CloudGUI.ParticleDistance.Clone(CloudLayer.ConfigBodyDatabase[configUrl][currentBody.name][SelectedLayer].ParticleDistance);
                            CloudGUI.Color.Clone(CloudLayer.ConfigBodyDatabase[configUrl][currentBody.name][SelectedLayer].Color);
                            CloudGUI.Altitude.Clone(CloudLayer.ConfigBodyDatabase[configUrl][currentBody.name][SelectedLayer].Altitude);
                            CloudGUI.ScaledShaderFloats.Clone(CloudLayer.ConfigBodyDatabase[configUrl][currentBody.name][SelectedLayer].ScaledShaderFloats);
                            CloudGUI.ShaderFloats.Clone(CloudLayer.ConfigBodyDatabase[configUrl][currentBody.name][SelectedLayer].ShaderFloats);
                            CloudGUI.UseVolume = CloudLayer.ConfigBodyDatabase[configUrl][currentBody.name][SelectedLayer].UseVolume; 
                        }
                    }

                    if (CloudGUI.IsValid())
                    {
                        if (GUI.Button(new Rect(215, 110, 60, 25), "Apply"))
                        {
                            CloudLayer.ConfigBodyDatabase[configUrl][currentBody.name][SelectedLayer].ApplyGUIUpdate(CloudGUI);
                        }
                        if (GUI.Button(new Rect(280, 110, 60, 25), "Save"))
                        {
                            CloudLayer.ConfigBodyDatabase[configUrl][currentBody.name][SelectedLayer].ApplyGUIUpdate(CloudGUI);
                            saveCloudLayers();
                        }
                    }

                    
                    gs.alignment = TextAnchor.MiddleRight;
                    if (AdvancedGUI)
                    {
                        GUI.Label(new Rect((_mainWindowRect.width / 2) + 10, 20, itemFullWidth, 25), "Settings:");
                        int advancedNextLine = HandleAdvancedGUI(CloudGUI.ShaderFloats, 50, _mainWindowRect.width / 2);
                        GUI.Label(new Rect((_mainWindowRect.width / 2) + 10, advancedNextLine, itemFullWidth, 25), "Scaled Settings:");
                        HandleAdvancedGUI(CloudGUI.ScaledShaderFloats, advancedNextLine + 30, _mainWindowRect.width / 2);
                    }

                    int nextLine = 290;

                    nextLine = HandleAltitudeGUI(CloudGUI.Altitude, nextLine);
                    nextLine = HandleColorGUI(CloudGUI.Color, nextLine);


                    GUI.Label(
                        new Rect(10, nextLine, 80, 25), "MainTex: ", gs);
                    nextLine = HandleTextureGUI(CloudGUI.MainTexture, nextLine);

                    CloudGUI.DetailTexture.InUse = GUI.Toggle(
                        new Rect(10, nextLine, 25, 25), CloudGUI.DetailTexture.InUse, "Use Detail");
                    nextLine += 30;

                    if (CloudGUI.DetailTexture.InUse)
                    {
                        GUI.Label(
                            new Rect(10, nextLine, 80, 25), "DetailTex: ", gs);

                        nextLine = HandleTextureGUI(CloudGUI.DetailTexture, nextLine);
                    }

                    CloudGUI.UseVolume = GUI.Toggle(
                        new Rect(10, nextLine, 125, 25), CloudGUI.UseVolume, "Volumetric Clouds");

                    nextLine += 30;

                    if (CloudGUI.UseVolume)
                    {
                        GUI.Label(new Rect(10, nextLine, 80, 25), "TopTex: ", gs);
                        CloudGUI.ParticleTopTexture = GUI.TextField(new Rect(90, nextLine, _mainWindowRect.width - 100, 25), CloudGUI.ParticleTopTexture);
                        nextLine += 30;

                        GUI.Label(new Rect(10, nextLine, 80, 25), "LeftTex: ", gs);
                        CloudGUI.ParticleLeftTexture = GUI.TextField(new Rect(90, nextLine, _mainWindowRect.width - 100, 25), CloudGUI.ParticleLeftTexture);
                        nextLine += 30;

                        GUI.Label(new Rect(10, nextLine, 80, 25), "FrontTex: ", gs);
                        CloudGUI.ParticleFrontTexture = GUI.TextField(new Rect(90, nextLine, _mainWindowRect.width - 100, 25), CloudGUI.ParticleFrontTexture);
                        nextLine += 30;

                        nextLine = HandleParticleDistanceGUI(CloudGUI.ParticleDistance, nextLine);
                    }

                }
            }
            else
            {
                GUI.Label(new Rect(50, 50, 230, 25), "----");
            }
            GUI.DragWindow(new Rect(0, 0, 10000, 10000));

        }

        private int HandleAdvancedGUI(ShaderFloatsGUI floats, int y, float offset)
        {
            GUIStyle gs = new GUIStyle(GUI.skin.label);
            gs.alignment = TextAnchor.MiddleRight;
            GUIStyle texFieldGS = new GUIStyle(GUI.skin.textField);
            Color errorColor = new Color(1, 0, 0);
            Color normalColor = texFieldGS.normal.textColor;
            float dummyFloat;


            GUI.Label(
                new Rect(offset + 10, y, 75, 25), "RimPower: ", gs);
            if (float.TryParse(floats.FalloffPowerString, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            string SFalloffPower = GUI.TextField(new Rect(offset + 90, y, 75, 25), floats.FalloffPowerString, texFieldGS);
            float FFalloffPower = GUI.HorizontalSlider(new Rect(offset + 175, y + 5, 165, 25), floats.FalloffPower, 0, 3);
            y += 30;
            GUI.Label(
                new Rect(offset + 10, y, 75, 25), "RimScale: ", gs);
            if (float.TryParse(floats.FalloffScaleString, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            string SFalloffScale = GUI.TextField(new Rect(offset + 90, y, 75, 25), floats.FalloffScaleString, texFieldGS);
            float FFalloffScale = GUI.HorizontalSlider(new Rect(offset + 175, y + 5, 165, 25), floats.FalloffScale, 0, 20);
            y += 30;
            GUI.Label(
                new Rect(offset + 10, y, 75, 25), "DetailDist: ", gs);
            if (float.TryParse(floats.DetailDistanceString, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            string SDetailDistance = GUI.TextField(new Rect(offset + 90, y, 75, 25), floats.DetailDistanceString, texFieldGS);
            float FDetailDistance = GUI.HorizontalSlider(new Rect(offset + 175, y + 5, 165, 25), floats.DetailDistance, 0, 1);
            y += 30;
            GUI.Label(
                new Rect(offset + 10, y, 75, 25), "MinLight: ", gs);
            if (float.TryParse(floats.MinimumLightString, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            string SMinimumLight = GUI.TextField(new Rect(offset + 90, y, 75, 25), floats.MinimumLightString, texFieldGS);
            float FMinimumLight = GUI.HorizontalSlider(new Rect(offset + 175, y + 5, 165, 25), floats.MinimumLight, 0, 1);
            y += 30;
            GUI.Label(
                new Rect(offset + 10, y, 75, 25), "FadeDist: ", gs);
            if (float.TryParse(floats.FadeDistanceString, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            string SFadeDist = GUI.TextField(new Rect(offset + 90, y, 75, 25), floats.FadeDistanceString, texFieldGS);
            float FFadeDist = GUI.HorizontalSlider(new Rect(offset + 175, y + 5, 165, 25), floats.FadeDistance, 0, 100);

            y += 30;
            GUI.Label(
                new Rect(offset + 10, y, 75, 25), "RimDist: ", gs);
            if (float.TryParse(floats.RimDistanceString, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            string SRimDist = GUI.TextField(new Rect(offset + 90, y, 75, 25), floats.RimDistanceString, texFieldGS);
            float FRimDist = GUI.HorizontalSlider(new Rect(offset + 175, y + 5, 165, 25), floats.RimDistance, 0, 1);

            floats.Update(SFalloffPower, FFalloffPower, SFalloffScale, FFalloffScale, SDetailDistance, FDetailDistance, SMinimumLight, FMinimumLight, SFadeDist, FFadeDist, SRimDist, FRimDist);

            return y + 30;
        }

        private int HandleParticleDistanceGUI(FloatGUI particleDistance, int y)
        {
            GUIStyle gs = new GUIStyle(GUI.skin.label);
            gs.alignment = TextAnchor.MiddleRight;
            GUIStyle texFieldGS = new GUIStyle(GUI.skin.textField);
            Color errorColor = new Color(1, 0, 0);
            Color normalColor = texFieldGS.normal.textColor;
            float dummyFloat;

            GUI.Label(
                new Rect(10, y, 80, 25), "Distance: ", gs);
            if (float.TryParse(particleDistance.AltitudeS, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            string sDist = GUI.TextField(new Rect(90, y, 90, 25), particleDistance.AltitudeS, texFieldGS);
            float fDist = GUI.HorizontalSlider(new Rect(185, y + 5, _mainWindowRect.width - 195, 25), particleDistance.AltitudeF, 0.000001f, 0.0001f);
            particleDistance.Update(fDist, sDist);
            return y + 30;
        }

        private int HandleAltitudeGUI(FloatGUI altitude, int y)
        {
            GUIStyle gs = new GUIStyle(GUI.skin.label);
            gs.alignment = TextAnchor.MiddleRight;
            GUIStyle texFieldGS = new GUIStyle(GUI.skin.textField);
            Color errorColor = new Color(1, 0, 0);
            Color normalColor = texFieldGS.normal.textColor;
            float dummyFloat;

            GUI.Label(
                new Rect(10, y, 65, 25), "Altitude: ", gs);
            if (float.TryParse(altitude.AltitudeS, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            string sAltitude = GUI.TextField(new Rect(80, y, 80, 25), altitude.AltitudeS, texFieldGS);
            float fAltitude = GUI.HorizontalSlider(new Rect(165, y + 5, 175, 25), altitude.AltitudeF, 0, 22000);
            altitude.Update(fAltitude, sAltitude);
            return y + 30;
        }

        private int HandleColorGUI(ColorSetGUI color, int y)
        {
            GUIStyle gs = new GUIStyle(GUI.skin.label);
            gs.alignment = TextAnchor.MiddleRight;
            GUIStyle texFieldGS = new GUIStyle(GUI.skin.textField);
            Color errorColor = new Color(1, 0, 0);
            Color normalColor = texFieldGS.normal.textColor;
            float dummyFloat;

            GUI.Label(
                new Rect(10, y, 65, 25), "Color R: ", gs);
            if (float.TryParse(color.Red, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            string SRed = GUI.TextField(new Rect(80, y, 80, 25), color.Red, texFieldGS);
            float FRed = GUI.HorizontalSlider(new Rect(165, y + 5, 175, 25), color.Color.r, 0, 1);
            y += 30;
            GUI.Label(
                new Rect(10, y, 65, 25), "G: ", gs);
            if (float.TryParse(color.Green, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            string SGreen = GUI.TextField(new Rect(80, y, 80, 25), color.Green, texFieldGS);
            float FGreen = GUI.HorizontalSlider(new Rect(165, y + 5, 175, 25), color.Color.g, 0, 1);
            y += 30;
            GUI.Label(
                new Rect(10, y, 65, 25), "B: ", gs);
            if (float.TryParse(color.Blue, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            string SBlue = GUI.TextField(new Rect(80, y, 80, 25), color.Blue, texFieldGS);
            float FBlue = GUI.HorizontalSlider(new Rect(165, y + 5, 175, 25), color.Color.b, 0, 1);
            y += 30;
            GUI.Label(
                new Rect(10, y, 65, 25), "A: ", gs);
            if (float.TryParse(color.Alpha, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            string SAlpha = GUI.TextField(new Rect(80, y, 80, 25), color.Alpha, texFieldGS);
            float FAlpha = GUI.HorizontalSlider(new Rect(165, y + 5, 175, 25), color.Color.a, 0, 1);
            color.Update(SRed, FRed, SGreen, FGreen, SBlue, FBlue, SAlpha, FAlpha);
            return y += 30;
        }

        private int HandleTextureGUI(TextureSetGUI textureSet, int y)
        {

            GUIStyle labelGS = new GUIStyle(GUI.skin.label);
            labelGS.alignment = TextAnchor.MiddleRight;
            GUIStyle texFieldGS = new GUIStyle(GUI.skin.textField);
            Color errorColor = new Color(1, 0, 0);
            Color normalColor = texFieldGS.normal.textColor;
            float dummyFloat;

            float vectorWidth = (_mainWindowRect.width - 140) / 2;
            float vectorStart = 105 + (_mainWindowRect.width - 140) / 2;

            textureSet.TextureFile = GUI.TextField(
                new Rect(90, y, _mainWindowRect.width - 100, 25), textureSet.TextureFile);
            y += 30;
            GUI.Label(
                new Rect(10, y, 90, 25), "Scale: ", labelGS);
            if (float.TryParse(textureSet.Scale, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            textureSet.Scale = GUI.TextField(
                new Rect(100, y, vectorWidth, 25), textureSet.Scale, texFieldGS);
            y += 30;
            GUI.Label(
                new Rect(10, y, 90, 25), "Offset X: ", labelGS);
            if (float.TryParse(textureSet.StartOffsetX, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            textureSet.StartOffsetX = GUI.TextField(
                new Rect(100, y, vectorWidth, 25), textureSet.StartOffsetX, texFieldGS);
            GUI.Label(
                new Rect(vectorStart, y, 25, 25), "Y: ", labelGS);
            if (float.TryParse(textureSet.StartOffsetY, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            textureSet.StartOffsetY = GUI.TextField(
                new Rect(vectorStart + 25, y, vectorWidth, 25), textureSet.StartOffsetY, texFieldGS);
            y += 30;
            GUI.Label(
                new Rect(10, y, 90, 25), "Speed X: ", labelGS);
            if (float.TryParse(textureSet.SpeedX, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            textureSet.SpeedX = GUI.TextField(
                new Rect(100, y, vectorWidth, 25), textureSet.SpeedX, texFieldGS);
            GUI.Label(
                new Rect(vectorStart, y, 25, 25), "Y: ", labelGS);
            if (float.TryParse(textureSet.SpeedY, out dummyFloat))
            {
                texFieldGS.normal.textColor = normalColor;
                texFieldGS.hover.textColor = normalColor;
                texFieldGS.active.textColor = normalColor;
                texFieldGS.focused.textColor = normalColor;
            }
            else
            {
                texFieldGS.normal.textColor = errorColor;
                texFieldGS.hover.textColor = errorColor;
                texFieldGS.active.textColor = errorColor;
                texFieldGS.focused.textColor = errorColor;
            }
            textureSet.SpeedY = GUI.TextField(
                new Rect(vectorStart + 25, y, vectorWidth, 25), textureSet.SpeedY, texFieldGS);
            return y + 30;

        }

    }

    internal class ShaderFloats
    {
        public float FalloffPower;
        public float FalloffScale;
        public float DetailDistance;
        public float MinimumLight;
        public float FadeDistance;
        public float RimDistance;

        public ShaderFloats()
        {
            FalloffPower = 0;
            FalloffScale = 0;
            DetailDistance = 0;
            MinimumLight = 0;
            FadeDistance = 0;
            RimDistance = 0;
        }

        public ShaderFloats(float FalloffPower, float FalloffScale, float DetailDistance, float MinimumLight, float FadeDistance, float RimDistance)
        {
            this.FalloffPower = FalloffPower;
            this.FalloffScale = FalloffScale;
            this.DetailDistance = DetailDistance;
            this.MinimumLight = MinimumLight;
            this.FadeDistance = FadeDistance;
            this.RimDistance = RimDistance;
        }

        public ShaderFloats(ConfigNode configNode)
        {
            FalloffPower = float.Parse(configNode.GetValue("falloffPower"));
            FalloffScale = float.Parse(configNode.GetValue("falloffScale"));
            DetailDistance = float.Parse(configNode.GetValue("detailDistance"));
            MinimumLight = float.Parse(configNode.GetValue("minimumLight"));
            FadeDistance = float.Parse(configNode.GetValue("fadeDistance"));
            RimDistance = float.Parse(configNode.GetValue("rimDistance"));
        }

        public void Clone(ShaderFloatsGUI toClone)
        {
            FalloffPower = toClone.FalloffPower;
            FalloffScale = toClone.FalloffScale;
            DetailDistance = toClone.DetailDistance;
            MinimumLight = toClone.MinimumLight;
            FadeDistance = toClone.FadeDistance;
            RimDistance = toClone.RimDistance;
        }

        internal ConfigNode GetNode(string name)
        {
            ConfigNode newNode = new ConfigNode(name);
            newNode.AddValue("falloffPower", FalloffPower);
            newNode.AddValue("falloffScale", FalloffScale);
            newNode.AddValue("detailDistance", DetailDistance);
            newNode.AddValue("minimumLight", MinimumLight);
            newNode.AddValue("fadeDistance", FadeDistance);
            newNode.AddValue("rimDistance", RimDistance);
            return newNode;
        }
    }

    internal class ShaderFloatsGUI
    {
        public float FalloffPower;
        public float FalloffScale;
        public float DetailDistance;
        public float MinimumLight;
        public float FadeDistance;
        public float RimDistance;
        public string FalloffPowerString;
        public string FalloffScaleString;
        public string DetailDistanceString;
        public string MinimumLightString;
        public string FadeDistanceString;
        public string RimDistanceString;

        public ShaderFloatsGUI()
        {
            FalloffPower = 0;
            FalloffScale = 0;
            DetailDistance = 0;
            MinimumLight = 0;
            FadeDistance = 0;
            RimDistance = 0;
            FalloffPowerString = "0";
            FalloffScaleString = "0";
            DetailDistanceString = "0";
            MinimumLightString = "0";
            FadeDistanceString = "0";
            RimDistanceString = "0";
        }

        public ShaderFloatsGUI(float FalloffPower, float FalloffScale, float DetailDistance, float MinimumLight, float FadeDistance, float RimDistance)
        {
            this.FalloffPower = FalloffPower;
            this.FalloffScale = FalloffScale;
            this.DetailDistance = DetailDistance;
            this.MinimumLight = MinimumLight;
            this.FadeDistance = FadeDistance;
            this.RimDistance = RimDistance;
            FalloffPowerString = FalloffPower.ToString("R");
            FalloffScaleString = FalloffScale.ToString("R");
            DetailDistanceString = DetailDistance.ToString("R");
            MinimumLightString = MinimumLight.ToString("R");
            FadeDistanceString = FadeDistance.ToString("R");
            RimDistanceString = RimDistance.ToString("R");
        }

        public void Clone(ShaderFloats toClone)
        {
            FalloffPower = toClone.FalloffPower;
            FalloffScale = toClone.FalloffScale;
            DetailDistance = toClone.DetailDistance;
            MinimumLight = toClone.MinimumLight;
            FadeDistance = toClone.FadeDistance;
            RimDistance = toClone.RimDistance;
            FalloffPowerString = FalloffPower.ToString("R");
            FalloffScaleString = FalloffScale.ToString("R");
            DetailDistanceString = DetailDistance.ToString("R");
            MinimumLightString = MinimumLight.ToString("R");
            FadeDistanceString = FadeDistance.ToString("R");
            RimDistanceString = RimDistance.ToString("R");
        }

        internal void Update(string SFalloffPower, float FFalloffPower, string SFalloffScale, float FFalloffScale, string SDetailDistance, float FDetailDistance, string SMinimumLight, float FMinimumLight, string SFadeDist, float FFadeDist, string SRimDist, float FRimDist)
        {
            if (FalloffPowerString != SFalloffPower)
            {
                FalloffPowerString = SFalloffPower;
                float.TryParse(SFalloffPower, out FalloffPower);
            }
            else if (FalloffPower != FFalloffPower)
            {
                FalloffPower = FFalloffPower;
                FalloffPowerString = FFalloffPower.ToString("R");
            }
            if (FalloffScaleString != SFalloffScale)
            {
                FalloffScaleString = SFalloffScale;
                float.TryParse(SFalloffScale, out FalloffScale);
            }
            else if (FalloffScale != FFalloffScale)
            {
                FalloffScale = FFalloffScale;
                FalloffScaleString = FFalloffScale.ToString("R");
            }
            if (DetailDistanceString != SDetailDistance)
            {
                DetailDistanceString = SDetailDistance;
                float.TryParse(SDetailDistance, out DetailDistance);
            }
            else if (DetailDistance != FDetailDistance)
            {
                DetailDistance = FDetailDistance;
                DetailDistanceString = FDetailDistance.ToString("R");
            }
            if (MinimumLightString != SMinimumLight)
            {
                MinimumLightString = SMinimumLight;
                float.TryParse(SMinimumLight, out MinimumLight);
            }
            else if (MinimumLight != FMinimumLight)
            {
                MinimumLight = FMinimumLight;
                MinimumLightString = FMinimumLight.ToString("R");
            }
            if (FadeDistanceString != SFadeDist)
            {
                FadeDistanceString = SFadeDist;
                float.TryParse(SFadeDist, out FadeDistance);
            }
            else if (FadeDistance != FFadeDist)
            {
                FadeDistance = FFadeDist;
                FadeDistanceString = FFadeDist.ToString("R");
            }
            if (RimDistanceString != SRimDist)
            {
                RimDistanceString = SRimDist;
                float.TryParse(SRimDist, out RimDistance);
            }
            else if (RimDistance != FRimDist)
            {
                RimDistance = FRimDist;
                RimDistanceString = FRimDist.ToString("R");
            }
        }

        internal bool IsValid()
        {

            float dummy;
            if (float.TryParse(FalloffPowerString, out dummy) &&
                float.TryParse(FalloffScaleString, out dummy) &&
                float.TryParse(DetailDistanceString, out dummy) &&
                float.TryParse(MinimumLightString, out dummy) &&
                float.TryParse(FadeDistanceString, out dummy) &&
                float.TryParse(RimDistanceString, out dummy))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    internal class CloudGUI
    {
        public TextureSetGUI MainTexture = new TextureSetGUI();
        public TextureSetGUI DetailTexture = new TextureSetGUI();
        public string ParticleTopTexture = "";
        public string ParticleLeftTexture = "";
        public string ParticleFrontTexture = "";
        public FloatGUI ParticleDistance = new FloatGUI();
        public ColorSetGUI Color = new ColorSetGUI();
        public FloatGUI Altitude = new FloatGUI();
        public ShaderFloatsGUI ScaledShaderFloats = new ShaderFloatsGUI();
        public ShaderFloatsGUI ShaderFloats = new ShaderFloatsGUI();
        public Boolean UseVolume = false;

        internal bool IsValid()
        {
            if (MainTexture.IsValid() &&
                DetailTexture.IsValid() &&
                Color.IsValid() &&
                Altitude.IsValid() &&
                ParticleDistance.IsValid() &&
                ScaledShaderFloats.IsValid() &&
                ShaderFloats.IsValid())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    
}
