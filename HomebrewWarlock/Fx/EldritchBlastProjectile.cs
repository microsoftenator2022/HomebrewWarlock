using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static HomebrewWarlock.Fx.Fx;

using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Kingmaker.View;
using Kingmaker.Visual.MaterialEffects.RimLighting;
using Kingmaker.Visual.MaterialEffects.Dissolve;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

using UnityEngine;
using UnityEngine.Rendering;
using Kingmaker.Visual.Particles;

namespace HomebrewWarlock.Fx
{
    public static class Fx
    {
        internal static Texture2D ChangeTextureColors(Texture2D texture, Func<Color, Color> f)
        {
            var textureFormat = TextureFormat.RGBA32;

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

            var logLevel = MicroLogger.LogLevel;

            if (logLevel == MicroLogger.Severity.Debug)
                MicroLogger.LogLevel = MicroLogger.Severity.Info;

            var newPixels = texture.GetPixels().Select(f).ToArray();

            MicroLogger.LogLevel = logLevel;

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

        internal static void ChangeAllColors(GameObject obj, Func<Color, Color> f)
        {
            var wasActive = obj.activeSelf;
            obj.SetActive(false);

            foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>())
            {
                MicroLogger.Debug(() => $"{ps.gameObject.name} :: {ps.GetType()}");

                var psr = ps.gameObject.GetComponent<ParticleSystemRenderer>();

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
                if (psr.material.GetTexture(rampTextureName) is Texture2D caTexture)
                {
                    //var textureFormat = TextureFormat.RGBA32;

                    //var readOnly = !texture.isReadable;

                    //if (readOnly)
                    //{
                    //    var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);

                    //    Graphics.ConvertTexture(texture, copy);

                    //    var request = AsyncGPUReadback.Request(copy, 0, TextureFormat.RGBA32);

                    //    request.WaitForCompletion();

                    //    var data = request.GetData<Color32>(0);

                    //    var newTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);

                    //    newTexture.LoadRawTextureData(data);
                    //    newTexture.Apply();

                    //    texture = newTexture;
                    //}
                    //else
                    //{
                    //    var newTexture = new Texture2D(
                    //        texture.width,
                    //        texture.height,
                    //        textureFormat: textureFormat,
                    //        mipCount: texture.mipmapCount,
                    //        false);

                    //    Graphics.CopyTexture(texture, newTexture);

                    //    texture = newTexture;
                    //}

                    //var logLevel = MicroLogger.LogLevel;

                    //if (logLevel == MicroLogger.Severity.Debug)
                    //    MicroLogger.LogLevel = MicroLogger.Severity.Info;

                    //var newPixels = texture.GetPixels().Select(f).ToArray();

                    //MicroLogger.LogLevel = logLevel;

                    //if (textureFormat.SupportsSetPixel())
                    //{
                    //    texture.SetPixels(newPixels);
                    //    texture.Apply();
                    //}
                    //else
                    //{
                    //    var tempRGBA = new Texture2D(
                    //        texture.width,
                    //        texture.height,
                    //        textureFormat: TextureFormat.RGBA32,
                    //        mipCount: texture.mipmapCount,
                    //        false);

                    //    tempRGBA.SetPixels(newPixels);
                    //    tempRGBA.Compress(false);
                    //    tempRGBA.Apply(true, readOnly);

                    //    Graphics.CopyTexture(tempRGBA, texture);
                    //    UnityEngine.Object.Destroy(tempRGBA);
                    //}

                    psr.material.SetTexture(rampTextureName, ChangeTextureColors(caTexture, f));
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
                MicroLogger.Debug(() => $"{lr.gameObject.name} :: {lr.GetType()}");

                lr.colorGradient = UnityUtil.ChangeGradientColors(lr.colorGradient, f);
            }

            foreach (var al in obj.GetComponentsInChildren<AnimatedLight>() ?? Enumerable.Empty<AnimatedLight>())
            {
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

    public static class EldritchBlastProjectile
    {
        internal static BlueprintInitializationContext.ContextInitializer<BlueprintProjectile> CreateProjectile(BlueprintInitializationContext context)
        {
            var projectile = context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintProjectile.Disintegrate00)
                .Map((BlueprintProjectile bp) =>
                {
                    static Color RotateColor(Color color) => UnityUtil.RotateColorHue(color, 140);

                    bp = AssetUtils.CloneBlueprint(bp, GeneratedGuid.Get("EldritchBlastProjectile"), nameof(GeneratedGuid.EldritchBlastProjectile));

                    bp.View = bp.View.CreateDynamicMonobehaviourProxy<ProjectileView, ProjectileLink>(pv =>
                    {
                        pv.gameObject.name = "EldritchBlast_projectile";

                        MicroLogger.Debug(() => $"{UnityUtil.Debug.DumpGameObject(pv.gameObject)}");

                        ChangeAllColors(pv.gameObject, RotateColor);
                    });

                    bp.CastFx = bp.CastFx.CreateDynamicProxy(cfx =>
                    {
                        cfx.name = "EldritchBlast_CastFX";

                        MicroLogger.Debug(() => $"{UnityUtil.Debug.DumpGameObject(cfx)}");

                        ChangeAllColors(cfx, RotateColor);
                    });

                    bp.ProjectileHit.HitFx = bp.ProjectileHit.HitFx.CreateDynamicProxy(hfx =>
                    {
                        hfx.name = "EldritchBlast_HitFX";

                        MicroLogger.Debug(() => $"{UnityUtil.Debug.DumpGameObject(hfx)}");

                        ChangeAllColors(hfx, RotateColor);
                    });

                    bp.ProjectileHit.HitSnapFx = bp.ProjectileHit.HitSnapFx.CreateDynamicProxy(hsfx =>
                    {
                        hsfx.name = "EldritchBlast_HitSnapFX";

                        MicroLogger.Debug(() => $"{UnityUtil.Debug.DumpGameObject(hsfx)}");

                        ChangeAllColors(hsfx, RotateColor);
                    });

                    bp.ProjectileHit.MissFx = bp.ProjectileHit.MissFx.CreateDynamicProxy(mfx =>
                    {
                        mfx.name = "EldritchBlast_MissFX";

                        MicroLogger.Debug(() => $"{UnityUtil.Debug.DumpGameObject(mfx)}");

                        ChangeAllColors(mfx, RotateColor);
                    });

                    bp.ProjectileHit.MissDecalFx = bp.ProjectileHit.MissDecalFx.CreateDynamicProxy(mdfx =>
                    {
                        mdfx.name = "EldritchBlast_MissDecalFX";

                        MicroLogger.Debug(() => $"{UnityUtil.Debug.DumpGameObject(mdfx)}");

                        ChangeAllColors(mdfx, RotateColor);
                    });

                    return bp;
                });

            return projectile;
        }
    }
}
