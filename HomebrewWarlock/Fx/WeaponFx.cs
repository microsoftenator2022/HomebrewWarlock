using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.ResourceLinks;
using Kingmaker.Visual.MaterialEffects.ColorTint;
using Kingmaker.Visual.MaterialEffects.RimLighting;

using UnityEngine;

namespace HomebrewWarlock.Fx
{
    internal static class WeaponFxPrefabs
    {
        internal static PrefabLink Standard =>
            new PrefabLink() { AssetId = "10e570e1da0d99f4ab69893791b17af4" }.CreateDynamicProxy(fx =>
            {
                var lightning = fx.transform.Find("Lightning")?.gameObject;
                UnityEngine.Object.DestroyImmediate(lightning);

                var sparks = fx.transform.Find("Sparks")?.gameObject;
                UnityEngine.Object.DestroyImmediate(sparks);

                var eod = fx.transform.Find("ElectricityOverDistance")?.gameObject;
                UnityEngine.Object.DestroyImmediate(eod);

                var ek = fx.transform.Find("ElectroKatyshki")?.gameObject;
                UnityEngine.Object.DestroyImmediate(ek);

                var electricity = fx.transform.Find("Electricity")?.gameObject;
                UnityEngine.Object.DestroyImmediate(electricity);

                var fillGlow = fx.transform.Find("FillGlow")?.gameObject;

                if (fillGlow != null && fillGlow.GetComponent<ParticleSystem>() is { } ps)
                {
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
                }

                FxColor.ChangeAllColors(fx, c =>
                {
                    var d = UnityUtil.RotateColorHue(c, 22);

                    Color.RGBToHSV(d, out var h, out var s, out var v);

                    d = Color.HSVToRGB(h, Mathf.Lerp(s, 1, s), Mathf.Lerp(v, 1, v));
                    d.a = c.a;

                    return d;
                });

                var fl = fx.transform.Find("Fresnel_loop (1)")?.gameObject;

                if (fl != null)
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
