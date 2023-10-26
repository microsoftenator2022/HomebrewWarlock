using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Visual.MaterialEffects.RimLighting;
using Kingmaker.Visual.MaterialEffects.Dissolve;

using UnityEngine;
using UnityEngine.Rendering;
using Kingmaker.Visual.Particles;
using Kingmaker.Visual;

namespace HomebrewWarlock.Fx
{
    public static class FxColor
    {
        internal static Texture2D ChangeTextureColors(Texture2D texture, Func<Color, Color> f,
            TextureFormat textureFormat = TextureFormat.RGBA32)
        {
            var readOnly = !texture.isReadable;

            if (readOnly)
            {
                var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);

                Graphics.ConvertTexture(texture, copy);

                var request = AsyncGPUReadback.Request(copy, 0, TextureFormat.RGBA32);

                request.WaitForCompletion();

                var data = request.GetData<Color32>(0);

                var newTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);

                newTexture.LoadRawTextureData(data);
                newTexture.Apply();

                texture = newTexture;
            }
            else
            {
                var newTexture = new Texture2D(
                    texture.width,
                    texture.height,
                    textureFormat: textureFormat,
                    mipCount: texture.mipmapCount,
                    false);

                Graphics.CopyTexture(texture, newTexture);

                texture = newTexture;
            }

            //var logLevel = MicroLogger.LogLevel;

            //if (!debugLog)
            //{
            //    if (logLevel == MicroLogger.Severity.Debug)
            //        MicroLogger.LogLevel = MicroLogger.Severity.Info;
            //}

            var newPixels = texture.GetPixels().Select(f).ToArray();

            //MicroLogger.LogLevel = logLevel;

            if (textureFormat.SupportsSetPixel())
            {
                texture.SetPixels(newPixels);
                texture.Apply();
            }
            else
            {
                var tempRGBA = new Texture2D(
                    texture.width,
                    texture.height,
                    textureFormat: TextureFormat.RGBA32,
                    mipCount: texture.mipmapCount,
                    false);

                tempRGBA.SetPixels(newPixels);
                tempRGBA.Compress(false);
                tempRGBA.Apply(true, readOnly);

                Graphics.CopyTexture(tempRGBA, texture);
                UnityEngine.Object.Destroy(tempRGBA);
            }

            return texture;
        }

        internal static void ChangeAllColors(GameObject obj, Func<Color, Color> f, bool includeBaseMap = false, bool includeMaterialColor = false, bool debugLog = false)
        {
            var wasActive = obj.activeSelf;
            obj.SetActive(false);

            foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>())
            {
                if (debugLog)
                    MicroLogger.Debug(() => $"{ps.gameObject.name} :: {ps.GetType()}");

                var psr = ps.gameObject.GetComponent<ParticleSystemRenderer>();

                if (debugLog)
                    MicroLogger.Debug(() =>
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine($"Renderer {psr.name}");

                        sb.AppendLine($" Material: {psr.material.name}");

                        sb.AppendLine($" Shader: {psr.material.shader.name}");
                        sb.AppendLine($" Textures:");

                        foreach (var tName in psr.material.GetTexturePropertyNames())
                        {
                            var tex = psr.material.GetTexture(tName);

                            if (tex is not null)
                                sb.AppendLine($"  {tName}: [{(tex as Texture2D)?.format}] {tex.name}");
                        }

                        return sb.ToString();
                    });

                const string rampTextureName = "_ColorAlphaRamp";
                if (psr.material.GetTexturePropertyNames().Contains(rampTextureName) && 
                    psr.material.GetTexture(rampTextureName) is Texture2D caTexture)
                {
                    psr.material.SetTexture(rampTextureName, ChangeTextureColors(caTexture, f));
                }

                if (includeBaseMap)
                {
                    var bm = psr.material.GetTexture(ShaderProps._BaseMap) as Texture2D;
                    if (bm is not null)
                        psr.material.SetTexture(ShaderProps._BaseMap, ChangeTextureColors(bm, f));
                }

                if (includeMaterialColor)
                {
                    psr.material.color = f(psr.material.color);
                }

                var pmc = ps.gameObject.GetComponent<ParticlesMaterialController>();
                if (pmc is not null)
                {
                    if (pmc.TexColorAlphaRamp is not null)
                        pmc.TexColorAlphaRamp = ChangeTextureColors(pmc.TexColorAlphaRamp, f);

                    if (pmc.TexTrailColorRamp is not null)
                        pmc.TexTrailColorRamp = ChangeTextureColors(pmc.TexTrailColorRamp, f);

                    if (pmc.ColorAlphaRamp is not null)
                        pmc.ColorAlphaRamp = UnityUtil.ChangeGradientColors(pmc.ColorAlphaRamp, f);

                    if (pmc.TrailColorAlphaRamp is not null)
                        pmc.TrailColorAlphaRamp = UnityUtil.ChangeGradientColors(pmc.TrailColorAlphaRamp, f);
                }

                var cot = ps.colorOverLifetime;

                var main = ps.main;
                var sc = main.startColor;

                cot.color = UnityUtil.ChangeMinMaxGradientColors(cot.color, f);
                main.startColor = UnityUtil.ChangeMinMaxGradientColors(sc, f);
            }

            foreach (var lr in obj.GetComponentsInChildren<LineRenderer>() ?? Enumerable.Empty<LineRenderer>())
            {
                if (debugLog)
                    MicroLogger.Debug(() => $"{lr.gameObject.name} :: {lr.GetType()}");

                lr.colorGradient = UnityUtil.ChangeGradientColors(lr.colorGradient, f);
            }

            foreach (var al in obj.GetComponentsInChildren<AnimatedLight>() ?? Enumerable.Empty<AnimatedLight>())
            {
                if (debugLog)
                    MicroLogger.Debug(() => $"{al.gameObject.name} :: {al.GetType()}");

                var c = al.m_Color;
                var col = al.m_ColorOverLifetime;

                al.m_Color = f(c);
                al.m_ColorOverLifetime = UnityUtil.ChangeGradientColors(col, f);
            }

            foreach (var rl in obj.GetComponentsInChildren<RimLightingAnimationSetup>())
            {
                rl.Settings.ColorOverLifetime = UnityUtil.ChangeGradientColors(rl.Settings.ColorOverLifetime, f);
            }

            foreach (var d in obj.GetComponentsInChildren<DissolveSetup>())
            {
                d.Settings.ColorOverLifetime = UnityUtil.ChangeGradientColors(d.Settings.ColorOverLifetime, f);
            }

            obj.SetActive(wasActive);
        }
    }
}
