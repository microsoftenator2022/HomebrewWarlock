using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.ResourceLinks;
using Kingmaker.Visual.MaterialEffects.ColorTint;
using Kingmaker.Visual.MaterialEffects.RimLighting;

using MicroWrath.BlueprintInitializationContext;

using UnityEngine;

namespace HomebrewWarlock.Fx
{
    internal static class WeaponEnchant
    {
        internal static BlueprintInitializationContext.ContextInitializer<BlueprintWeaponEnchantment> CreateWeaponEnchant(
            BlueprintInitializationContext context) =>
            context.NewBlueprint<BlueprintWeaponEnchantment>(GeneratedGuid.Get("EldritchBlastWeaponEnchantFx"))
                .Map(enchant =>
                {
                    enchant.m_EnchantName = LocalizedStrings.Features_EldritchBlast_EldritchBlast_DisplayName;

                    enchant.WeaponFxPrefab = WeaponFxPrefabs.Standard;

                    return enchant;
                });
    }

    internal static class WeaponFxPrefabs
    {
        internal static PrefabLink Standard =>
            new PrefabLink() { AssetId = "10e570e1da0d99f4ab69893791b17af4" }.CreateDynamicProxy(fx =>
            {
                void changeColor(GameObject fx, int hueRotation, Func<float, float>? sf = null, bool includeMaterialColor = false)
                {
                    sf ??= Functional.Identity;

                    FxColor.ChangeAllColors(fx, c =>
                    {
                        var d = UnityUtil.RotateColorHue(c, hueRotation);

                        Color.RGBToHSV(d, out var h, out var s, out var v);

                        d = Color.HSVToRGB(h, sf(s), Mathf.Lerp(v, 1, v));

                        d.a = c.a;

                        return d;
                    }, includeMaterialColor: includeMaterialColor);
                }

                var ek = fx.transform.Find("ElectroKatyshki")?.gameObject;
                UnityEngine.Object.DestroyImmediate(ek);

                var sparks = fx.transform.Find("Sparks")?.gameObject;
                UnityEngine.Object.DestroyImmediate(sparks);

                var lightning = fx.transform.Find("Lightning")?.gameObject;
                //UnityEngine.Object.DestroyImmediate(lightning);
                if (lightning is not null) FxColor.ChangeAllColors(lightning, c => UnityUtil.RotateColorHue(c, 16));

                var eod = fx.transform.Find("ElectricityOverDistance")?.gameObject;
                //UnityEngine.Object.DestroyImmediate(eod);
                if (eod is not null) 
                {
                    changeColor(eod, 38, sf: s => Mathf.Lerp(s, 0f, 1f - s), includeMaterialColor: true);
                }

                var electricity = fx.transform.Find("Electricity")?.gameObject;
                //UnityEngine.Object.DestroyImmediate(electricity);
                if (electricity is not null)
                {
                    var ps = electricity.GetComponent<ParticleSystem>();
                    var col = ps.colorOverLifetime;
                    var mmGradient = col.color;
                    var gradient = mmGradient.gradient;

                    gradient.SetKeys(gradient.colorKeys, gradient.alphaKeys.Select(value => new GradientAlphaKey() { time = value.time, alpha = value.alpha / 10f }).ToArray());

                    mmGradient.gradient = gradient;
                    col.color = mmGradient;

                    changeColor(electricity, 30, sf: s => Mathf.Lerp(s, 0f, 1f - s), includeMaterialColor: true);
                }

                var fillGlow = fx.transform.Find("FillGlow")?.gameObject;

                if (fillGlow is not null && fillGlow.GetComponent<ParticleSystem>() is not null)
                {
                    var ps = fillGlow.GetComponent<ParticleSystem>();
                    var col = ps.colorOverLifetime;
                    var mmg = col.color;

                    var gradient = mmg.gradient;

                    gradient.mode = GradientMode.Blend;

                    var alphaValues = gradient.alphaKeys
                        .Select(ak => ak.alpha)
                        .Distinct()
                        .OrderByDescending(Functional.Identity)
                        .ToArray();

                    MicroLogger.Debug(sb =>
                    {
                        for (var i = 0; i < alphaValues.Length; i++)
                            sb.AppendLine($"alphaValues[{i}] = {alphaValues[i]}");
                    });

                    var aks = gradient.alphaKeys;

                    for (var i = 0; i < aks.Length; i++)
                    {
                        //aks[i].alpha = alphaValues[0];

                        aks[i].alpha = Mathf.Max(aks[i].alpha, alphaValues[1]);

                        if (aks[i].alpha < alphaValues[0])
                            aks[i].alpha = alphaValues[0];
                        else
                            aks[i].alpha = alphaValues[1];
                    }

                    //for (var i = aks.Length / 2; i < aks.Length; i++)
                    //{
                    //    aks[i].alpha = alphaValues[1];
                    //}

                    gradient.alphaKeys = aks;
                    mmg.gradient = gradient;
                    col.color = mmg;

                    //foreach (var i in col.color.gradient.alphaKeys)
                    //{
                    //    MicroLogger.Debug(() => i.alpha.ToString());
                    //}

                    changeColor(fillGlow, 18);
                }

                var fl = fx.transform.Find("Fresnel_loop (1)")?.gameObject;

                if (fl is not null)
                {
                    var rlas = fl.GetComponent<RimLightingAnimationSetup>();
                    rlas.Settings.ColorOverLifetime = UnityUtil.ChangeGradientColors(rlas.Settings.ColorOverLifetime, _ => new Color(0, 0, 0));

                    var ctas = fl.GetComponent<ColorTintAnimationSetup>();
                    UnityEngine.Object.DestroyImmediate(ctas);
                    //ctas.Settings.ColorOverLifetime = UnityUtil.ChangeGradientColors(ctas.Settings.ColorOverLifetime, _ => new Color(0, 0, 0));
                }
            });
    }
}
