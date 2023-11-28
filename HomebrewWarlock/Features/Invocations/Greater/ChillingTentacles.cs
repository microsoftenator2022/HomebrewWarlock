using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using DG.Tweening;
using DG.Tweening.Core.Easing;

using HomebrewWarlock.Fx;
using HomebrewWarlock.Resources;

using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.Root.Fx;
using Kingmaker.BundlesLoading;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using Kingmaker.Visual;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using Kingmaker.Visual.MaterialEffects;
using Kingmaker.Visual.Particles;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Util.Linq;

using Owlcat.Runtime.Core.Utils;

using UnityEngine;

namespace HomebrewWarlock.Features.Invocations.Greater
{
    internal static class ChillingTentacles
    {
        internal static class Fx
        {
            readonly struct WaveAll(GameObject animationRoot)
            {
                GameObject GetChild(string namePart) => animationRoot.transform.Find($"{nameof(WaveAll)}_{namePart}").gameObject;

                public GameObject StartCracks00 => GetChild(nameof(StartCracks00));
                public GameObject StartCracks00_RotatableCopy => GetChild(nameof(StartCracks00_RotatableCopy));
                public GameObject StartTrailGround00 => GetChild(nameof(StartTrailGround00));
                public GameObject GrassLiana00 => GetChild(nameof(GrassLiana00));
                public GameObject GrassLiana00_RotatableCopy => GetChild(nameof(GrassLiana00_RotatableCopy));
                public GameObject GrassLianaSingle00 => GetChild(nameof(GrassLianaSingle00));
                public GameObject GrassLianaSingle00_RotatableCopy => GetChild(nameof(GrassLianaSingle00_RotatableCopy));
                public GameObject Ambient00 => GetChild(nameof(Ambient00));
                public GameObject StinkingSmoke00 => GetChild(nameof(StinkingSmoke00));
                public GameObject StinkingSmoke00_RotatableCopy => GetChild(nameof(StinkingSmoke00_RotatableCopy));
                public GameObject FireFliesGreen => GetChild(nameof(FireFliesGreen));
                public GameObject FireFliesViolet => GetChild(nameof(FireFliesViolet));
            }

            readonly struct Wave00(GameObject animationRoot)
            {
                GameObject GetChild(string namePart) => animationRoot.transform.Find($"{nameof(Wave00)}_{namePart}").gameObject;

                public GameObject Ground00 => GetChild(nameof(Ground00));
                public GameObject Ground00_RotatableCopy => GetChild(nameof(Ground00_RotatableCopy));
                public GameObject Liana00 => GetChild(nameof(Liana00));
                public GameObject Liana00_RotatableCopy => GetChild(nameof(Liana00_RotatableCopy));
                public GameObject LianaStart00 => GetChild(nameof(LianaStart00));
                public GameObject LianaStart00_RotatableCopy => GetChild(nameof(LianaStart00_RotatableCopy));
            }

            readonly struct Wave01(GameObject animationRoot)
            {
                GameObject GetChild(string namePart) => animationRoot.transform.Find($"{nameof(Wave01)}_{namePart}").gameObject;

                public GameObject Ground00 => GetChild(nameof(Ground00));
                public GameObject Ground00_RotatableCopy => GetChild(nameof(Ground00_RotatableCopy));
                public GameObject Liana00 => GetChild(nameof(Liana00));
                public GameObject Liana00_RotatableCopy => GetChild(nameof(Liana00_RotatableCopy));
                public GameObject LianaStart00 => GetChild(nameof(LianaStart00));
                public GameObject LianaStart00_RotatableCopy => GetChild(nameof(LianaStart00_RotatableCopy));
            }

            readonly struct Wave02(GameObject animationRoot)
            {
                GameObject GetChild(string namePart) => animationRoot.transform.Find($"{nameof(Wave02)}_{namePart}").gameObject;

                public GameObject Ground00 => GetChild(nameof(Ground00));
                public GameObject Ground00_RotatableCopy => GetChild(nameof(Ground00_RotatableCopy));
                public GameObject Liana00 => GetChild(nameof(Liana00));
                public GameObject Liana00_RotatableCopy => GetChild(nameof(Liana00_RotatableCopy));
                public GameObject LianaStart00 => GetChild(nameof(LianaStart00));
                public GameObject LianaStart00_RotatableCopy => GetChild(nameof(LianaStart00_RotatableCopy));
            }

            const string BundleName = "HomebrewWarlock_assets_all";

            static string BundlePath =>
#if DEBUG
                $@"D:\Poonity\WrathModificationTemplate-master\Build\HomebrewWarlock\Bundles\{BundleName}";
#else
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", $"{BundleName}");
#endif

            static AssetBundle? bundle = null;
            
            static bool reloading;
            internal static AssetBundle ReloadBundle()
            {
                if (reloading) return bundle!;

                reloading = true;

                if (bundle is not null)
                {
                    MicroLogger.Debug(() => "Unloading bundle");

                    bundle.Unload(true);
                }

                MicroLogger.Debug(() => $"Loading bundle from {BundlePath}");

                bundle = AssetBundle.LoadFromFile(BundlePath);

                ReloadMaterials(Material.name);

                reloading = false;

                return bundle;
            }

            static AssetBundle Bundle => bundle ?? ReloadBundle();

            // Lit shader
            const string materialId = "745833f55acae1f4bb23568a4e3ad376";

            // Particles shader
            //const string materialId = "8503d0bde8b88894798b00f3aacc7de5";

            static Material Material => Bundle.LoadAsset<Material>(materialId);

            static Material GetMaterial(Material oldMaterial)
            {
                var mat = UnityEngine.Object.Instantiate(Material);

                MicroLogger.Debug(sb =>
                {
                    sb.Append("Assets:");
                    foreach (var name in Bundle.GetAllAssetNames())
                    {
                        sb.AppendLine();
                        sb.Append($"  {name} {Bundle.LoadAsset(name).name}");
                    }
                });

                if (mat is null)
                {
                    MicroLogger.Debug(() => $"Failed to load material");
                    return null!;
                }

                //MicroLogger.Debug(sb =>
                //{
                //    sb.AppendLine($"{oldMaterial.name} keywords:");
                //    foreach (var kw in oldMaterial.shaderKeywords)
                //    {
                //        sb.AppendLine($"  {kw}");
                //    }
                //});

                //var shader = Shader.Find("Owlcat/Particles") ?? oldMaterial.shader;
                var shader = oldMaterial.shader;

                mat = UnityEngine.Object.Instantiate(mat);

                MicroLogger.Debug(() => $"Setting {mat.name} shader to {shader.name}");

                mat.shader = shader;

                for (var i = 0; i < oldMaterial.passCount; i++)
                {
                    var name = mat.GetPassName(i);

                    mat.SetShaderPassEnabled(name, oldMaterial.GetShaderPassEnabled(name));
                }

                //mat.EnableKeyword("PARTICLES_LIGHTING_ON");

                foreach (var kw in oldMaterial.shaderKeywords)
                {
                    mat.EnableKeyword(kw);
                }

                mat.SetOverrideTag("DisableBatching", "False");
                mat.SetOverrideTag("Reflection", "Cutout");
                mat.SetOverrideTag("RenderType", "Transparent");

                var enableKeywords = new[] { "_EMISSION", "_EMISSIONMAP", "_MASKSMAP", "_NORMALMAP" };
                var disableKeywords = new[] { "_RIMLIGHTING_OFF" };

                foreach (var kw in enableKeywords)
                {
                    mat.EnableKeyword(kw);
                }

                foreach (var kw in disableKeywords)
                {
                    mat.DisableKeyword(kw);
                }

                //MicroLogger.Debug(sb =>
                //{
                //    sb.AppendLine($"{mat.name} keywords:");
                //    foreach (var kw in mat.shaderKeywords)
                //    {
                //        sb.AppendLine($"  {kw}");
                //    }
                //});

                return mat;
            }

            static void ReloadMaterials(string name)
            {
                foreach (var psr in UnityEngine.Resources.FindObjectsOfTypeAll<ParticleSystemRenderer>())
                {
                    if (!psr.material.name.StartsWith(name)) continue;

                    psr.material = GetMaterial(psr.material);
                }
            }

            public static PrefabLink AoeFx => new PrefabLink() { AssetId = "aa448b28b377b1c49b136d88fa346600" }
                .CreateDynamicProxy<PrefabLink>(fx =>
                {
                    fx.transform.localScale = new(0.25f, 1.0f, 0.25f);

                    var ar = fx.transform.Find("AnimationRoot").gameObject;

                    //FxColor.ChangeAllColors(ar.transform.Find("GrowingRoots").gameObject,
                    //    c => c *= 0.1f);

                    //var groundFog = ResourcesLibrary.TryGetResource<GameObject>("8c4c90e58c04f814a8418a5926b4a212");

                    var waveAll = new WaveAll(ar);
                    var wave00 = new Wave00(ar);
                    var wave01 = new Wave01(ar);
                    var wave02 = new Wave02(ar);

                    FxColor.ChangeAllColors(ar.transform.Find("GrowingRoots").gameObject,
                        _ => Color.black, true);

                    foreach (var go in new[]
                    {
                        //ar.transform.Find("GrowingRoots").gameObject,

                        waveAll.GrassLiana00,
                        waveAll.GrassLiana00_RotatableCopy,
                        waveAll.GrassLianaSingle00,
                        waveAll.GrassLianaSingle00_RotatableCopy,
                        waveAll.StartTrailGround00,

                        wave00.Ground00,
                        wave00.Ground00_RotatableCopy,

                        wave01.Ground00,
                        wave01.Ground00_RotatableCopy,

                        wave02.Ground00,
                        wave02.Ground00_RotatableCopy,
                    })
                    {
                        if (go != null)
                        {
                            MicroLogger.Debug(() => $"Deleting {go.name}");
                            GameObject.DestroyImmediate(go);
                        }
                    }

                    //FxColor.ChangeAllColors(waveAll.Ambient00, c => UnityUtil.RotateColorHue(c, 140));
                    //FxColor.ChangeAllColors(waveAll.Ambient00, _ => Color.black);

                    //FxColor.ChangeAllColors(waveAll.FireFliesGreen, c => c.ModifyHSV(hsv => hsv with { s = 0 }));
                    //FxColor.ChangeAllColors(waveAll.FireFliesViolet, c => c.ModifyHSV(hsv => hsv with { s = 0 }));

                    UnityEngine.Object.DestroyImmediate(waveAll.FireFliesGreen);
                    UnityEngine.Object.DestroyImmediate(waveAll.FireFliesViolet);

                    //var fxVisualPrefab = fx.transform.Find("FxVisualPrefab_Decal00").gameObject;

                    //FxColor.ChangeAllColors(fxVisualPrefab, _ => Color.black);

                    //var rampTexture = AssetUtils.GetTextureAssemblyResource(Assembly.GetExecutingAssembly(),
                    //    $"{nameof(HomebrewWarlock)}.Resources.fogramp.png", TextureFormat.RGB24, false)!;

                    //rampTexture = FxColor.ChangeTextureColors(rampTexture,
                    //    c => UnityUtil.ModifyHSV(c, hsv => hsv with { s = hsv.s * 0.2 }),
                    //    TextureFormat.RGBA32);

                    //void SetRampColors(ParticlesMaterialController pmc)
                    //{
                    //    var ramp = new[]
                    //    {
                    //        new Color(0, 0, 0),
                    //        new Color(0, 0, 0),
                    //        new Color(0, 0, 0),
                    //        new Color(0.066667f, 0, 0.250980f),
                    //        new Color(0.223529f, 0, 0.501960f),
                    //        new Color(0.149019f, 0, 0.345098f)
                    //    };

                    //    var car = pmc.ColorAlphaRamp;
                    //    var colorKeys = car.colorKeys;

                    //    for (var i = 0; i < ramp.Length; i++)
                    //    {
                    //        var ck = colorKeys[i];
                    //        ck.color = UnityUtil.ModifyHSV(ramp[i], hsv => hsv with { s = hsv.s * 0.2 });
                    //        colorKeys[i] = ck;
                    //    }

                    //    car.colorKeys = colorKeys;
                    //    pmc.ColorAlphaRamp = car;

                    //    pmc.TexColorAlphaRamp = rampTexture;
                    //}

                    void SetSmokeProps(GameObject smokeObj)
                    {
                        smokeObj.SetActive(false);

                        //smokeObj.GetComponent<ParticleSystemRenderer>().material.color =
                        ////new Color(0.8443396f, 0.8443396f, 0.8443396f);
                        //new Color(0.6f, 0.6f, 0.8f);

                        //smokeObj.GetComponent<ParticleSystemRenderer>().material
                        //    .SetTexture(ShaderProps._ColorAlphaRamp, rampTexture);

                        //SetRampColors(smokeObj.GetComponent<ParticlesMaterialController>());

                        //var ps = smokeObj.GetComponent<ParticleSystem>();
                        //var main = ps.main;
                        //main.startSizeYMultiplier = 0.1f;

                        //var psc = smokeObj.GetComponent<ParticlesSnapController>();

                        //psc.Offset.Enabled = true;
                        //psc.Offset.OffsetY.AddKey(0, -0.9f);
                        //psc.Offset.OffsetY.AddKey(1, -0.9f);
                    }

                    SetSmokeProps(waveAll.StinkingSmoke00);
                    SetSmokeProps(waveAll.StinkingSmoke00_RotatableCopy);

                    //SetRampColors(waveAll.StinkingSmoke00.GetComponent<ParticlesMaterialController>());
                    //SetRampColors(waveAll.StinkingSmoke00_RotatableCopy.GetComponent<ParticlesMaterialController>());

                    //UnityEngine.Object.DestroyImmediate(waveAll.StinkingSmoke00);
                    //UnityEngine.Object.DestroyImmediate(waveAll.StinkingSmoke00_RotatableCopy);

                    void SetTentacleMaterial(GameObject go)
                    {
                        MicroLogger.Debug(() => $"Setting material for {go.name}");

                        var psr = go.GetComponent<ParticleSystemRenderer>();

                        var something = GetMaterial(psr.material);

                        psr.material = something;
                    }

                    //ChangeColorsAndMainTexture(wave00.LianaStart00);
                    //ChangeColorsAndMainTexture(wave00.LianaStart00_RotatableCopy);
                    SetTentacleMaterial(wave00.Liana00);
                    SetTentacleMaterial(wave00.Liana00_RotatableCopy);

                    //ChangeColorsAndMainTexture(wave01.LianaStart00);
                    //ChangeColorsAndMainTexture(wave01.LianaStart00_RotatableCopy);
                    SetTentacleMaterial(wave01.Liana00);
                    SetTentacleMaterial(wave01.Liana00_RotatableCopy);

                    //ChangeColorsAndMainTexture(wave02.LianaStart00);
                    //ChangeColorsAndMainTexture(wave02.LianaStart00_RotatableCopy);
                    SetTentacleMaterial(wave02.Liana00);
                    SetTentacleMaterial(wave02.Liana00_RotatableCopy);
                });

            internal class FadeIn : MonoBehaviour
            {
                public void OnEnable()
                {
                    var smoke = gameObject.transform.Find("Smoke00_Billboard").gameObject;

                    var psr = smoke.GetComponent<ParticleSystemRenderer>();

                    psr.material.color = new(1, 1, 1, 0);

                    DOTween.Sequence()
                        .Append(psr.material.DOFade(0.1f, 1).ChangeStartValue(0))
                        .Append(psr.material.DOFade(0.25f, 2).SetEase(Ease.InOutSine))
                        .Append(psr.material.DOFade(0.5f, 3).SetEase(Ease.InOutSine))
                        .Play();
                }

                void OnDisable()
                {
                }
            }

            internal static PrefabLink FogFx => new PrefabLink() { AssetId = "8c4c90e58c04f814a8418a5926b4a212" }
                .CreateDynamicProxy(fx =>
                {
                    const float scale = 0.6f;

                    fx.transform.localScale = new(1f / scale, 1f, 1f / scale);

                    var fxLocatorRoots = fx.GetComponentsInChildren<FxLocator>()
                        .Select(c => c.gameObject.transform.parent.parent.gameObject);

                    foreach (var lr in fxLocatorRoots)
                    {
                        //lr.transform.localPosition *= scale * scale;

                        var lpos = lr.transform.localPosition;

                        lpos.x *= scale * scale;
                        lpos.z *= scale * scale;

                        var d = Mathf.Sqrt(Mathf.Pow(lpos.x, 2) + Mathf.Pow(lpos.z, 2));

                        lpos.y = scale * (1f / (1f + d) - 1f);

                        lr.transform.localPosition = lpos;
                    }

                    var smoke = fx.transform.Find("Smoke00_Billboard").gameObject;

                    var ps = smoke.GetComponent<ParticleSystem>();

                    var main = ps.main;

                    main.startColor = new() { mode = ParticleSystemGradientMode.Color, color = new(1, 1, 1, 1) };

                    var startSizeX = main.startSizeX;

                    startSizeX.constant *= scale;

                    main.startSizeX = startSizeX;

                    var startSizeY = main.startSizeY;

                    startSizeY.constant *= scale;

                    main.startSizeY = startSizeY;

                    var pmc = smoke.GetComponent<ParticlesMaterialController>();

                    UnityEngine.Object.DestroyImmediate(pmc);

                    //var car = pmc.ColorAlphaRamp;
                    //var colorKeys = car.colorKeys;

                    //for (var i = 0; i < colorKeys.Length; i++)
                    //{
                    //    var ck = colorKeys[i];
                    //    ck.color = ck.color with { g = ck.color.r, b = ck.color.r };

                    //    colorKeys[i] = ck;
                    //}

                    //car.alphaKeys = [new(0, 0), new(0f, car.alphaKeys[1].time)];

                    //car.colorKeys = colorKeys;
                    //pmc.ColorAlphaRamp = car;

                    var psr = smoke.GetComponent<ParticleSystemRenderer>();
                    psr.material.SetColor(ShaderProps._TintColor, Color.white);

                    //psr.material.SetFloat("_RampAlbedoWeight", 1);
                    psr.material.SetFloat("_HdrColorClamp", 100);
                    psr.material.SetFloat("_HdrColorScale", 2f);
                    psr.maxParticleSize = 100;

                    var fadeIn = fx.AddComponent<FadeIn>();

                    if (fx.activeSelf)
                        fadeIn.OnEnable();
                });
        }

        internal class TentacleGrapple : ContextAction
        {
            public bool UseCasterLevel = true;
            public bool ReplaceCasterLevel;
            public ContextValue? CasterLevel;
            
            public ActionList OnSuccess = new();
            public ActionList OnFailure = new();

            public int Bonus = 5;

            public override string GetCaption() => "Tentacle grapple";
            public override void RunAction()
            {
                if (base.Target is null || base.Context is null)
                {
                    return;
                }

                var rule = new RuleCombatManeuver(base.Context.MaybeCaster, base.Target.Unit, CombatManeuver.Grapple)
                {
                    OverrideBonus = this.Bonus
                };

                if (this.UseCasterLevel)
                {
                    var casterLevel = base.Context.Params.CasterLevel;

                    if (this.ReplaceCasterLevel && this.CasterLevel is not null)
                        casterLevel = this.CasterLevel.Calculate(base.Context);

                    rule.ReplaceAttackBonus = casterLevel;
                }

                rule = base.Context.TriggerRule(rule);

                if (rule.Success)
                {
                    this.OnSuccess?.Run();
                    return;
                }

                this.OnFailure?.Run();
            }
        }

        [LocalizedString]
        internal const string DisplayName = "Chilling Tentacles";

        [LocalizedString]
        internal static readonly string Description =
            "<b>Equivalent spell level:</b> 5" +
            Environment.NewLine +
            "This invocation allows you to conjure forth a field of soul-chilling black tentacles that ooze from " +
            "the ground, groping for victims." + Environment.NewLine +
            "This invocation functions identically to the black tentacles spell, except that each creature within " +
            "the area of the invocation takes 2d6 points of cold damage each round. Creatures in the area take " +
            "this cold damage whether or not they are grappled by the tentacles.";

        [LocalizedString]
        internal const string LocalizedDuration = "1 round/level";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var buff = context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("ChillingTentaclesGrappleBuff"))
                .Map((BlueprintBuff buff) =>
                {
                    buff.m_DisplayName = LocalizedStrings.Features_Invocations_Greater_ChillingTentacles_DisplayName;
                    buff.m_Description = LocalizedStrings.Features_Invocations_Greater_ChillingTentacles_Description;
                    buff.m_Icon = Sprites.ChillingTentacles;

                    buff.AddContextRankConfig(c =>
                    {
                        c.m_BaseValueType = ContextRankBaseValueType.CasterLevel;
                    });

                    buff.AddAddCondition(c => c.Condition = UnitCondition.Entangled);
                    buff.AddAddCondition(c => c.Condition = UnitCondition.CantMove);

                    static GameAction GrappleDamage() =>
                        GameActions.ContextActionDealDamage(a =>
                        {
                            //a.DamageType.Type = DamageType.Untyped;
                            a.DamageType.Physical.Form = PhysicalDamageForm.Bludgeoning;
                            a.DamageType.Physical.Enhancement = 2;
                            a.DamageType.Physical.EnhancementTotal = 2;

                            a.Value.DiceType = DiceType.D6;
                            a.Value.DiceCountValue = 1;
                            a.Value.BonusValue = 4;
                        });

                    buff.AddComponent<AddFactContextActions>(c =>
                    {
                        c.Activated.Add(GrappleDamage());

                        c.NewRound.Add(GrappleDamage(),
                            GameActions.ContextActionBreakFree(a =>
                            {
                                a.UseCMB = true;
                                a.Success.Add(GameActions.ContextActionRemoveSelf());
                            }));
                    });

                    buff.m_Flags = BlueprintBuff.Flags.Harmful;

                    buff.Stacking = StackingType.Ignore;

                    return buff;
                });

            var areaEffect = context.NewBlueprint<BlueprintAbilityAreaEffect>(
                GeneratedGuid.Get("ChillingTentaclesAreaEffect"))
                .Combine(buff)
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.EntangleBuffDifficultTerrain))
                .Map(bps =>
                {
                    (BlueprintAbilityAreaEffect aoe, var buff, var difficultTerrain) = bps.Expand();

                    TentacleGrapple GrappleAction() => new()
                    {
                        OnSuccess = new()
                        {
                            Actions = new[] { GameActions.Conditional(conditional =>
                            {
                                conditional.AddCondition(Conditions.HasBuff(c =>
                                    c.m_Buff = buff.ToReference()));

                                conditional.IfFalse.Add(GameActions.ContextActionApplyBuff(a =>
                                {
                                    a.m_Buff = buff.ToReference();
                                    a.Permanent = true;
                                }));
                            }) }
                        },
                        OnFailure = new()
                        {
                            Actions = new[] { GameActions.ContextActionRemoveBuff(a =>
                                a.m_Buff = buff.ToReference()) }
                        }
                    };

                    aoe.AffectEnemies = true;
                    aoe.AggroEnemies = true;
                    aoe.m_TargetType = BlueprintAbilityAreaEffect.TargetType.Any;

                    aoe.Size = new Feet(20);

                    //aoe.AddContextRankConfig(c =>
                    //{
                    //    c.m_BaseValueType = ContextRankBaseValueType.CasterLevel;
                    //});

                    aoe.AddComponent<AbilityAreaEffectRunAction>(c =>
                    {
                        c.UnitEnter.Add(
                            GrappleAction(),
                            GameActions.ContextActionApplyBuff(c =>
                            {
                                c.m_Buff = difficultTerrain.ToReference();
                                c.Permanent = true;
                            }));

                        c.Round.Add(
                            GrappleAction(),
                            GameActions.ContextActionDealDamage(a =>
                            {
                                a.DamageType.Type = DamageType.Energy;
                                a.DamageType.Energy = DamageEnergyType.Cold;

                                a.Value.DiceType = DiceType.D6;
                                a.Value.DiceCountValue = 2;
                            }));

                        c.UnitExit.Add(
                            GameActions.ContextActionRemoveBuff(c =>
                                c.m_Buff = buff.ToReference()),
                            GameActions.ContextActionRemoveBuff(c =>
                                c.m_Buff = difficultTerrain.ToReference()));
                    });

                    aoe.Shape = AreaEffectShape.Cylinder;
                    aoe.Fx = Fx.AoeFx;

                    return aoe;
                });

            var fogAreaFx = context.NewBlueprint<BlueprintAbilityAreaEffect>(GeneratedGuid.Get("ChillingTentacleFogFxArea"))
                .Map(aoe =>
                {
                    aoe.Size = 20.Feet();
                    aoe.Shape = AreaEffectShape.Cylinder;

                    aoe.Fx = Fx.FogFx;

                    return aoe;
                });

            var ability = context.NewBlueprint<BlueprintAbility>(
                GeneratedGuid.Get("ChillingTentaclesAbility"))
                .Combine(areaEffect)
                .Combine(fogAreaFx)
                .Map(bps =>
                {
                    (BlueprintAbility ability, var aoe, var fog) = bps.Expand();

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Greater_ChillingTentacles_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_Invocations_Greater_ChillingTentacles_Description;
                    ability.m_Icon = Sprites.ChillingTentacles;

                    ability.AddComponent<AbilityEffectRunAction>(c =>
                    {
                        c.Actions.Add(GameActions.ContextActionSpawnAreaEffect(a =>
                        {
                            a.m_AreaEffect = aoe.ToReference();
                            a.DurationValue.BonusValue.ValueType = ContextValueType.Rank;
                        }));

                        c.Actions.Add(GameActions.ContextActionSpawnAreaEffect(a =>
                        {
                            a.m_AreaEffect = fog.ToReference();
                            a.DurationValue.BonusValue.ValueType = ContextValueType.Rank;
                        }));
                    });

                    ability.AddComponent<AbilityAoERadius>(c =>
                    {
                        c.m_Radius = new Feet(20);
                        c.m_TargetType = TargetType.Any;
                    });

                    ability.AddContextRankConfig(c =>
                    {
                        c.m_BaseValueType = ContextRankBaseValueType.CasterLevel;
                    });

                    ability.AddSpellDescriptorComponent(c =>
                    {
                        c.Descriptor = SpellDescriptor.Ground | SpellDescriptor.MovementImpairing;
                    });

                    ability.AddSpellComponent(c =>
                    {
                        c.School = SpellSchool.Transmutation;
                    });

                    ability.AddInvocationComponents(5);

                    ability.CanTargetEnemies = true;
                    ability.CanTargetFriends = true;
                    ability.CanTargetSelf = true;
                    ability.CanTargetPoint = true;
                    ability.EffectOnAlly = AbilityEffectOnUnit.Harmful;
                    ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;

                    ability.Type = AbilityType.SpellLike;

                    ability.Range = AbilityRange.Long;

                    ability.ActionType = UnitCommand.CommandType.Standard;

                    ability.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Directional;

                    ability.LocalizedDuration = LocalizedStrings.Features_Invocations_Greater_ChillingTentacles_LocalizedDuration;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("ChillingTentaclesFeature"))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName = ability.m_DisplayName;
                    feature.m_Description = ability.m_Description;
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return feature;
                });

            return feature;
        }
    }
}
