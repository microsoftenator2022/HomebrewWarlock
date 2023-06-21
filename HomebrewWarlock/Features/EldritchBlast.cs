using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.Visual.Animation.Kingmaker.Actions;

using Kingmaker.Visual.MaterialEffects.RimLighting;
using Kingmaker.Visual.MaterialEffects.Dissolve;

using MicroWrath;
using MicroWrath.Assets;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.GameActions;
using MicroWrath.Localization;
using MicroWrath.Util;
using MicroWrath.Util.Unity;

using Owlcat.Runtime.Core.Utils;

using UnityEngine;

namespace HomebrewWarlock.Features
{
    internal static partial class EldritchBlast
    {
        [LocalizedString]
        internal static readonly string DisplayName = "Eldritch Blast";

        [LocalizedString]
        internal static readonly string Description =
            "The first ability a warlock learns is eldritch blast. A warlock attacks his foes " +
            "with eldritch power, using baleful magical energy to deal damage and sometimes impart other " +
            $"debilitating effects.{Environment.NewLine}" +
            "An eldritch blast is a ray with a range of 60 feet. It is a ranged touch attack that affects a " +
            "single target, allowing no saving throw. An eldritch blast deals 1d6 points of damage at 1st level " +
            "and increases in power as the warlock rises in level. An eldritch blast is the equivalent of a " +
            "1st-level spell. If you apply a blast shape or eldritch essence invocation to your eldritch blast, " +
            $"your eldritch blast uses the level equivalent of the shape or essence.{Environment.NewLine}" +
            "An eldritch blast is subject to spell resistance, although the Spell Penetration feat and other " +
            "effects that improve caster level checks to overcome spell resistance also apply to eldritch " +
            "blast. An eldritch blast deals half damage to objects. Metamagic feats cannot improve a warlock's " +
            "eldritch blast (because it is a spell-like ability, not a spell). However, the feat Ability Focus " +
            "(eldritch blast) increases the DC for all saving throws (if any) associated with a warlock's " +
            "eldritch blast by 2.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> CreateFeature(BlueprintInitializationContext context)
        {
            Sprite getSprite() => AssetUtils.Direct.GetSprite("fdfbce1816665e74584c528faebcc381", 21300000);

            //var fx = context.NewBlueprint(() =>
            //    AssetUtils.CloneBlueprint(
            //        BlueprintsDb.Owlcat.BlueprintProjectile.Disintegrate00.GetBlueprint()!,
            //        GeneratedGuid.Get("EldritchBlastProjectile"),
            //        nameof(GeneratedGuid.EldritchBlastProjectile)))
            //    .Map((BlueprintProjectile projectile) =>
            //    {
            //        //var cast = projectile.CastFx;

            //        var view = UnityEngine.Object.Instantiate(projectile.View.Load());
            //        UnityEngine.Object.DontDestroyOnLoad(view);
                    
            //        //var hit = projectile.ProjectileHit.HitFx;
            //        //var hitSnap = projectile.ProjectileHit.HitSnapFx;
            //        //var miss = projectile.ProjectileHit.MissFx;
            //        //var missDecal = projectile.ProjectileHit.MissDecalFx;


            //        var gradients = view.gameObject.GetComponentsInChildren<Gradient>();
            //        foreach (var g in gradients)
            //        {
            //            for (var i = 0; i < g.colorKeys.Length; i++)
            //            {
            //                var c = g.colorKeys[i].color;
            //                var d = new Color { r = c.b, g = c.r, b = c.g, a = c.a };

            //                g.colorKeys[i].color = d;
            //            }
            //        }
            //    });

            var ability = context.NewBlueprint<BlueprintAbility>(GeneratedGuid.Get("EldritchBlastAbility"), nameof(GeneratedGuid.EldritchBlastAbility))
                .Map((BlueprintAbility ability) =>
                {
                    ability.m_DisplayName = LocalizedStrings.Features_EldritchBlast_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_EldritchBlast_Description;

                    ability.m_Icon = getSprite();

                    ability.Type = AbilityType.Special;
                    ability.Range = AbilityRange.Close;

                    ability.CanTargetEnemies = true;
                    ability.SpellResistance = true;
                    ability.EffectOnAlly = AbilityEffectOnUnit.None;
                    ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                    ability.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Directional;
                    ability.ActionType = Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard;

                    return ability;
                });

            var projectile = context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintProjectile.Disintegrate00)
                .Map((BlueprintProjectile bp) =>
                {
                    //static Color RotateColor(Color color)
                    //{
                    //    //if (color.r > 1 || color.g > 1 || color.b > 1 || color.a > 1)
                    //    //    return color;

                    //    //if (color.r < 0 || color.g < 0 || color.b < 0 || color.a < 0)
                    //    //    return color;

                    //    //if (color.a == 0)
                    //    //    return color;

                    //    if (color.r == color.g && color.g == color.b)
                    //        return color;

                    //    if (color.g < color.r || color.g < color.b)
                    //        return color;

                    //    var oldColor = color;

                    //    color.r = oldColor.g;
                    //    color.g = oldColor.b;
                    //    color.b = oldColor.r;

                    //    MicroLogger.Debug(() => $"{oldColor} -> {color}");

                    //    return color;
                    //}

                    //static Color RotateColorHue(Color color, double degrees)
                    //{
                    //    if (color.r == color.g && color.g == color.b)
                    //        return color;

                    //    var oldColor = color;

                    //    Color.RGBToHSV(color, out var h, out var s, out var v);

                    //    var oldH = h;

                    //    var hF64 = (double)h;

                    //    hF64 += degrees / 360.0;

                    //    // x.y -> (x.y - x.0) = 0.y
                    //    if (hF64 > 1) hF64 -= ((int)hF64);

                    //    // -x.y -> (-x.y + (-(-x.0) + 1) = (-x.y + (x.0 + 1)) = -0.y + 1 = (1 - 0.y)
                    //    if (hF64 < 0) hF64 += (-(int)hF64) + 1;

                    //    h = (float)hF64;

                    //    color = Color.HSVToRGB(h, s, v);

                    //    MicroLogger.Debug(() => $"{oldH}\u00b0 -> {oldH}\u00b0");
                    //    MicroLogger.Debug(() => $"{oldColor} -> {color}");

                    //    return color;
                    //}

                    //static Gradient? ChangeGradientColors(Gradient? g, Func<Color, Color> f)
                    //{
                    //    if (g is null || g.colorKeys is null) return g;

                    //    var colors = g.colorKeys;

                    //    for (var i = 0; i < colors.Length; i++)
                    //    {
                    //        var ck = colors[i];

                    //        ck.color = f(ck.color);

                    //        colors[i] = ck;
                    //    }

                    //    g.colorKeys = colors;

                    //    return g;
                    //}

                    //static ParticleSystem.MinMaxGradient ChangeMinMaxGradientColors(ParticleSystem.MinMaxGradient mmg, Func<Color, Color> f) =>
                    //    mmg.mode switch
                    //    {
                    //        ParticleSystemGradientMode.Color =>
                    //            new ParticleSystem.MinMaxGradient(f(mmg.color)),

                    //        ParticleSystemGradientMode.TwoColors =>
                    //            new ParticleSystem.MinMaxGradient(
                    //                f(mmg.colorMin),
                    //                f(mmg.colorMax)),

                    //        ParticleSystemGradientMode.Gradient => new
                    //            ParticleSystem.MinMaxGradient(ChangeGradientColors(mmg.gradient, f)),

                    //        ParticleSystemGradientMode.TwoGradients =>
                    //            new ParticleSystem.MinMaxGradient(
                    //                ChangeGradientColors(mmg.gradientMin, f),
                    //                ChangeGradientColors(mmg.gradientMax, f)),

                    //        _ => mmg,
                    //    };

                    static void ChangeAllColors(GameObject obj, Func<Color, Color> f)
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
                                        sb.AppendLine($"  {tName}: {tex.name}");
                                }

                                return sb.ToString();
                            });

                            const string rampTextureName = "_ColorAlphaRamp";

                            if (psr.material.GetTexture(rampTextureName) is { } caTexture)
                            {
                                var texCopy = new Texture2D(caTexture.width, caTexture.height);

                                Graphics.CopyTexture(caTexture, texCopy);

                                texCopy.SetPixels(texCopy.GetPixels().Select(p => f(p)).ToArray());
                                texCopy.Apply();

                                psr.material.SetTexture(rampTextureName, texCopy);
                            }

                            var cot = ps.colorOverLifetime;
                            cot.color = UnityUtil.ChangeMinMaxGradientColors(cot.color, f);

                            var main = ps.main;
                            var sc = main.startColor;

                            main.startColor = UnityUtil.ChangeMinMaxGradientColors(sc, f);
                        }

                        foreach (var lr in obj.GetComponentsInChildren<LineRenderer>() ?? Enumerable.Empty<LineRenderer>())
                        {
                            //var lra = lr.gameObject.activeSelf;
                            //lr.gameObject.SetActive(false);

                            MicroLogger.Debug(() => $"{lr.gameObject.name} :: {lr.GetType().ToString()}");

                            //lr.startColor = RotateColors(lr.startColor);
                            //lr.endColor = RotateColors(lr.endColor);

                            lr.colorGradient = UnityUtil.ChangeGradientColors(lr.colorGradient, f);

                            //lr.gameObject.SetActive(lra);
                        }

                        foreach (var al in obj.GetComponentsInChildren<AnimatedLight>() ?? Enumerable.Empty<AnimatedLight>())
                        {
                            MicroLogger.Debug(() => $"{al.gameObject.name} :: {al.GetType().ToString()}");

                            al.m_Color = f(al.m_Color);
                            al.m_ColorOverLifetime = UnityUtil.ChangeGradientColors(al.m_ColorOverLifetime, f);
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

                    static Color RotateColor(Color color) => UnityUtil.RotateColorHue(color, +160);

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

            var feature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("EldritchBlastFeature"), nameof(GeneratedGuid.EldritchBlastFeature))
                .Map((BlueprintFeature feature) =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_EldritchBlast_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_EldritchBlast_Description;

                    feature.m_Icon = getSprite();

                    feature.Ranks = 9;
                    
                    return feature;
                })
                .Combine(ability)
                .Combine(projectile)
                .Map(fa =>
                {
                    var (feature, ability, projectile) = fa.Flatten();

                    feature.AddAddFeatureIfHasFact(c =>
                    {
                        c.m_CheckedFact = ability.ToReference<BlueprintUnitFactReference>();
                        c.m_Feature = ability.ToReference<BlueprintUnitFactReference>();

                        c.Not = true;
                    });

                    ability.AddComponent<AbilityDeliverProjectile>(c =>
                    {
                        c.m_Projectiles = new[]
                        {
                            //BlueprintsDb.Owlcat.BlueprintProjectile.Disintegrate00.ToReference<BlueprintProjectile, BlueprintProjectileReference>()
                            projectile.ToReference<BlueprintProjectileReference>()
                        };

                        c.m_Length = new();
                        c.m_LineWidth = new() { m_Value = 5 };

                        c.NeedAttackRoll = true;

                        c.m_Weapon = BlueprintsDb.Owlcat.BlueprintItemWeapon.RayItem.ToReference<BlueprintItemWeapon, BlueprintItemWeaponReference>();
                    });

                    ability.AddComponent<AbilityEffectRunAction>(c =>
                    {
                        c.Actions.Add(
                            GameActions.ContextActionDealDamage(action =>
                            {
                                action.m_Type = ContextActionDealDamage.Type.Damage;
                               
                                action.DamageType.Type = DamageType.Energy;
                                action.DamageType.Energy = DamageEnergyType.Magic;

                                action.Value.DiceType = DiceType.D6;

                                action.Value.DiceCountValue.ValueType = ContextValueType.Rank;
                                action.Value.DiceCountValue.Value = 1;
                                action.Value.DiceCountValue.ValueRank = AbilityRankType.DamageDice;
                            }));
                    });

                    ability.AddContextRankConfig(c =>
                    {
                        c.m_Type = AbilityRankType.DamageDice;
                        c.m_BaseValueType = ContextRankBaseValueType.FeatureRank;
                        c.m_Feature = feature.ToReference<BlueprintFeatureReference>();
                        c.m_Progression = ContextRankProgression.AsIs;
                        c.m_StartLevel = 0;
                        c.m_StepLevel = 1;
                    });

                    return feature;
                });

            return feature;
        }
    }
}
