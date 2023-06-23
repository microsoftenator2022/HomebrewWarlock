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
using Kingmaker.View;
using Kingmaker.Visual.Animation.Kingmaker.Actions;

using Kingmaker.Visual.MaterialEffects.RimLighting;
using Kingmaker.Visual.MaterialEffects.Dissolve;

using MicroWrath;
using MicroWrath.Util.Assets;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.GameActions;
using MicroWrath.Localization;
using MicroWrath.Util;
using MicroWrath.Util.Unity;

using UnityEngine;
using Kingmaker.UnitLogic.Commands.Base;

namespace HomebrewWarlock.Features
{
    internal static partial class EldritchBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Eldritch Blast";

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

        [LocalizedString]
        internal const string ShortDescription =
            "A warlock attacks his foes with eldritch power, using baleful magical energy to deal damage and " +
            "sometimes impart other debilitating effects.";


        internal static readonly IMicroBlueprint<BlueprintFeature> Feature = new MicroBlueprint<BlueprintFeature>(GeneratedGuid.EldritchBlastFeature);

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> CreateFeature(BlueprintInitializationContext context)
        {
            static Sprite getSprite() => AssetUtils.Direct.GetSprite("fdfbce1816665e74584c528faebcc381", 21300000);

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
                    ability.ActionType = UnitCommand.CommandType.Standard;

                    return ability;
                });

            var projectile = context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintProjectile.Disintegrate00)
                .Map((BlueprintProjectile bp) =>
                {
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
                                        sb.AppendLine($"  {tName}: [{(tex as Texture2D)?.format}] {tex.name}");
                                }

                                return sb.ToString();
                            });

                            const string rampTextureName = "_ColorAlphaRamp";
                            
                            if (psr.material.GetTexture(rampTextureName) is { } caTexture)
                            {
                                var textureFormat = TextureFormat.RGBA32;

                                var hdr = psr.material.name.ToLowerInvariant().Contains("hdr");
                                
                                if (caTexture is Texture2D t2d)
                                {
                                    textureFormat = t2d.format;
                                }
                                else
                                {
                                    MicroLogger.Warning($"{caTexture.name} is not {typeof(Texture2D)}");
                                }
                                
                                var newTex = new Texture2D(
                                    caTexture.width,
                                    caTexture.height,
                                    textureFormat: textureFormat,
                                    mipCount: caTexture.mipmapCount,
                                    false);
                                Graphics.CopyTexture(caTexture, newTex);
                                
                                var logLevel = MicroLogger.LogLevel;
                                
                                if (logLevel == MicroLogger.Severity.Debug)
                                    MicroLogger.LogLevel = MicroLogger.Severity.Info;

                                var newPixels = newTex.GetPixels().Select(f).ToArray();

                                MicroLogger.LogLevel = logLevel;

                                if (textureFormat.SupportsSetPixel())
                                {
                                    newTex.SetPixels(newPixels);
                                }
                                else
                                {
                                    var tempRGBA = new Texture2D(
                                        caTexture.width,
                                        caTexture.height,
                                        textureFormat: TextureFormat.RGBA32,
                                        mipCount: caTexture.mipmapCount,
                                        false);

                                    tempRGBA.SetPixels(newPixels);
                                    tempRGBA.Compress(false);
                                    tempRGBA.Apply(true, true);

                                    Graphics.CopyTexture(tempRGBA, newTex);
                                }

                                newTex.Apply();

                                psr.material.SetTexture(rampTextureName, newTex);
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

            var feature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("EldritchBlastFeature"), nameof(GeneratedGuid.EldritchBlastFeature))
                .Map((BlueprintFeature feature) =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_EldritchBlast_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_EldritchBlast_Description;
                    feature.m_DescriptionShort = LocalizedStrings.Features_EldritchBlast_ShortDescription;

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
