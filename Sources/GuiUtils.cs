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
 * Copyright © 2013-2018 Ryan Bray, RangeMachine
 */

using System;
using System.Collections.Generic;

using UnityEngine;

namespace Utilities
{
    public class FloatGUI
    {
        public float AltitudeF;
        public string AltitudeS;

        public void Clone(float altitude)
        {
            AltitudeF = altitude;
            AltitudeS = altitude.ToString("R");
        }

        public void Update(float altitudeF, string altitudeS)
        {
            if (AltitudeS != altitudeS)
            {
                AltitudeS = altitudeS;
                float.TryParse(altitudeS, out AltitudeF);
            }
            else if (AltitudeF != altitudeF)
            {
                AltitudeF = altitudeF;
                AltitudeS = altitudeF.ToString("R");
            }
        }

        public bool IsValid()
        {
            float dummy;
            return float.TryParse(AltitudeS, out dummy);
        }
    }

    public class ColorSetGUI
    {
        public Color Color;
        public string Red = "";
        public string Green = "";
        public string Blue = "";
        public string Alpha = "";

        public void Clone(Color color)
        {
            Color = color;
            Red = color.r.ToString("R");
            Green = color.g.ToString("R");
            Blue = color.b.ToString("R");
            Alpha = color.a.ToString("R");
        }

        public void Update(string SRed, float FRed, string SGreen, float FGreen, string SBlue, float FBlue, string SAlpha, float FAlpha)
        {
            if (Red != SRed)
            {
                Red = SRed;
                float.TryParse(SRed, out Color.r);
            }
            else if (Color.r != FRed)
            {
                Color.r = FRed;
                Red = FRed.ToString("R");
            }
            if (Green != SGreen)
            {
                Green = SGreen;
                float.TryParse(SGreen, out Color.g);
            }
            else if (Color.g != FGreen)
            {
                Color.g = FGreen;
                Green = FGreen.ToString("R");
            }
            if (Blue != SBlue)
            {
                Blue = SBlue;
                float.TryParse(SBlue, out Color.b);
            }
            else if (Color.b != FBlue)
            {
                Color.b = FBlue;
                Blue = FBlue.ToString("R");
            }
            if (Alpha != SAlpha)
            {
                Alpha = SAlpha;
                float.TryParse(SAlpha, out Color.a);
            }
            else if (Color.a != FAlpha)
            {
                Color.a = FAlpha;
                Alpha = FAlpha.ToString("R");
            }
        }

        public bool IsValid()
        {
            float dummy;
            if (float.TryParse(Red, out dummy) &&
                float.TryParse(Green, out dummy) &&
                float.TryParse(Blue, out dummy) &&
                float.TryParse(Alpha, out dummy))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class TextureSetGUI
    {
        public string StartOffsetX = "";
        public string SpeedX = "";
        public string Scale = "";
        public string StartOffsetY = "";
        public string SpeedY = "";
        public string TextureFile = "";
        public bool InUse;

        public void Clone(TextureSet textureSet)
        {
            InUse = textureSet.InUse;
            TextureFile = textureSet.TextureFile;
            if (TextureFile == null)
            {
                TextureFile = "";
            }
            StartOffsetX = textureSet.StartOffset.x.ToString("R");
            StartOffsetY = textureSet.StartOffset.y.ToString("R");
            Scale = textureSet.Scale.ToString("R");
            SpeedX = textureSet.Speed.x.ToString("R");
            SpeedY = textureSet.Speed.y.ToString("R");
        }

        public bool IsValid()
        {
            float dummy;
            if (float.TryParse(StartOffsetX, out dummy) &&
                float.TryParse(StartOffsetY, out dummy) &&
                float.TryParse(SpeedX, out dummy) &&
                float.TryParse(SpeedY, out dummy) &&
                float.TryParse(Scale, out dummy))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class TextureSet
    {
        private static Dictionary<string, Texture> TextureDictionary = new Dictionary<string, Texture>();
        public Vector2 Offset;
        public Vector2 StartOffset;
        public Vector2 Speed;
        public float Scale;
        private Texture texture;
        private string textureFile;
        private bool isBump;

        public bool InUse;
        public Texture Texture { get { return texture; } }
        public string TextureFile { get { return textureFile; } }


        private void initTexture()
        {
            try
            {
                if (isBump && !TextureDictionary.ContainsKey(textureFile + "_BUMP"))
                {
                    Texture2D tex = GameDatabase.Instance.GetTexture(textureFile, isBump);
                    TextureDictionary.Add(textureFile + "_BUMP", tex);
                }
                else if (!isBump && !TextureDictionary.ContainsKey(textureFile))
                {
                    Texture2D tex = GameDatabase.Instance.GetTexture(textureFile, isBump);

                    //////////////////////////
                    /// DDS BUG WORKAROUND ///
                    //////////////////////////
                    try
                    {
                        tex.GetPixel(0, 0);
                    }
                    catch (UnityException)
                    {
                        tex.filterMode = FilterMode.Point;

                        RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height);
                        rt.filterMode = FilterMode.Point;

                        RenderTexture.active = rt;
                        Graphics.Blit(tex, rt);

                        Texture2D img2 = new Texture2D(tex.width, tex.height);
                        img2.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                        img2.Apply();

                        RenderTexture.active = null;

                        tex = img2;
                    }
                    //////////////////////////

                    try { AddMipMaps(tex); }
                    catch { }

                    if (tex.format != TextureFormat.DXT1 && tex.format != TextureFormat.DXT5)
                    {
                        try { tex.GetPixel(0, 0); tex.Compress(true); }
                        catch { }
                    }

                    TextureDictionary.Add(textureFile, tex);
                }
                string textureName = isBump ? textureFile + "_BUMP" : textureFile;
                texture = TextureDictionary[textureName];
            }
            catch
            {
                texture = new Texture();
            }
        }

        private void AddMipMaps(Texture2D tex)
        {
            if (tex.mipmapCount == 1)
            {
                Color32[] pixels = tex.GetPixels32();
                int width = tex.width;
                int height = tex.height;
                tex.Resize(width, height, TextureFormat.RGBA32, true);
                tex.SetPixels32(pixels);
                tex.Apply(true);
            }
        }

        public TextureSet()
        {
            textureFile = "";
            texture = null;
            isBump = false;
            InUse = false;
            Offset = new Vector2(0, 0);
            StartOffset = new Vector2(0, 0);
            Speed = new Vector2(0, 0);
            Scale = 1;
        }

        public TextureSet(ConfigNode textureNode, bool bump)
            : this()
        {
            isBump = bump;

            if (textureNode != null)
            {
                textureFile = textureNode.GetValue("file");
                if (textureFile != null)
                {
                    initTexture();

                    ConfigNode offsetNode = textureNode.GetNode("offset");
                    if (offsetNode != null)
                    {
                        Offset = new Vector2(float.Parse(offsetNode.GetValue("x")), float.Parse(offsetNode.GetValue("y")));
                        StartOffset = new Vector2(Offset.x, Offset.y);
                    }
                    else
                    {
                        Offset = Vector2.zero;
                        StartOffset = Vector2.zero;
                    }
                    ConfigNode speedNode = textureNode.GetNode("speed");
                    if (speedNode != null)
                    {
                        Speed = new Vector2(float.Parse(speedNode.GetValue("x")), float.Parse(speedNode.GetValue("y")));
                    }
                    else
                    {
                        Speed = Vector2.zero;
                    }
                    string scale = textureNode.GetValue("scale");
                    if (scale != null)
                    {
                        Scale = float.Parse(scale);
                    }
                    else
                    {
                        Scale = 1;
                    }

                    InUse = true;
                }
                else
                {
                    textureFile = "";
                }
            }
        }

        public TextureSet(bool inUse)
            : this()
        {
            InUse = inUse;
            UnityEngine.Debug.Log("TextureSet: " + Scale);
        }

        public void SaturateOffset()
        {
            while (Offset.x > 1.0f)
            {
                Offset.x -= 1.0f;
            }
            while (Offset.x < 0.0f)
            {
                Offset.x += 1.0f;
            }
            while (Offset.y > 1.0f)
            {
                Offset.y -= 1.0f;
            }
            while (Offset.y < 0.0f)
            {
                Offset.y += 1.0f;
            }
        }

        public void UpdateOffset(float rateOffset, bool rotation)
        {
            if (rotation)
            {
                Offset.x = rateOffset * Speed.x;
                Offset.y = rateOffset * Speed.y;
            }
            else
            {
                Offset.x += rateOffset * Speed.x;
                Offset.y += rateOffset * Speed.y;
                SaturateOffset();
            }
            
        }

        public void Clone(TextureSetGUI textureSet)
        {
            InUse = textureSet.InUse;
            textureFile = textureSet.TextureFile;
            if (InUse)
            {
                initTexture();
            }
            StartOffset.x = float.Parse(textureSet.StartOffsetX);
            StartOffset.y = float.Parse(textureSet.StartOffsetY);
            Offset.x = StartOffset.x;
            Offset.y = StartOffset.y;
            Scale = float.Parse(textureSet.Scale);
            Speed.x = float.Parse(textureSet.SpeedX);
            Speed.y = float.Parse(textureSet.SpeedY);
        }

        public ConfigNode GetNode(string name)
        {
            if (!InUse)
            {
                return null;
            }
            ConfigNode newNode = new ConfigNode(name);
            newNode.AddValue("file", textureFile);
            ConfigNode offsetNode = newNode.AddNode("offset");
            offsetNode.AddValue("x", StartOffset.x);
            offsetNode.AddValue("y", StartOffset.y);
            ConfigNode speedNode = newNode.AddNode("speed");
            speedNode.AddValue("x", Speed.x);
            speedNode.AddValue("y", Speed.y);
            newNode.AddValue("scale", Scale);
            return newNode;
        }
    }

}
