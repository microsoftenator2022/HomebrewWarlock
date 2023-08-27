using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Fx;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
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
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using Kingmaker.Visual;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using Kingmaker.Visual.Particles;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

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

            public static PrefabLink AoeFx => new PrefabLink() { AssetId = "aa448b28b377b1c49b136d88fa346600" }
                .CreateDynamicProxy<PrefabLink>(fx =>
                {
                    fx.transform.localScale = new(0.25f, 1.0f, 0.25f);

                    var ar = fx.transform.Find("AnimationRoot").gameObject;

                    //FxColor.ChangeAllColors(ar.transform.Find("GrowingRoots").gameObject,
                    //    c => c *= 0.1f);

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
                    FxColor.ChangeAllColors(waveAll.Ambient00, _ => Color.black);

                    FxColor.ChangeAllColors(waveAll.FireFliesGreen, c => UnityUtil.RotateColorHue(c, 110));

                    var fxVisualPrefab = fx.transform.Find("FxVisualPrefab_Decal00").gameObject;

                    FxColor.ChangeAllColors(fxVisualPrefab, _ => Color.black);

                    var rampTexture = AssetUtils.GetTextureAssemblyResource(Assembly.GetExecutingAssembly(),
                        $"{nameof(HomebrewWarlock)}.Resources.fogramp.png", TextureFormat.RGB24, false)!;

                    rampTexture = FxColor.ChangeTextureColors(rampTexture,
                        c => UnityUtil.ModifyHSV(c, hsv => hsv with { s = hsv.s * 0.2 }),
                        TextureFormat.RGBA32);

                    waveAll.StinkingSmoke00.GetComponent<ParticleSystemRenderer>().material.color =
                        //new Color(0.8443396f, 0.8443396f, 0.8443396f);
                        new Color(0.6f, 0.6f, 0.8f);

                    waveAll.StinkingSmoke00.GetComponent<ParticleSystemRenderer>().material
                        .SetTexture(ShaderProps._ColorAlphaRamp, rampTexture);

                    waveAll.StinkingSmoke00_RotatableCopy.GetComponent<ParticleSystemRenderer>().material.color =
                        //new Color(0.8443396f, 0.8443396f, 0.8443396f);
                        new Color(0.6f, 0.6f, 0.8f);

                    waveAll.StinkingSmoke00_RotatableCopy.GetComponent<ParticleSystemRenderer>().material
                        .SetTexture(ShaderProps._ColorAlphaRamp, rampTexture);

                    void SetRampColors(ParticlesMaterialController pmc)
                    {
                        var ramp = new[]
                        {
                            new Color(0, 0, 0),
                            new Color(0, 0, 0),
                            new Color(0, 0, 0),
                            new Color(0.066667f, 0, 0.250980f),
                            new Color(0.223529f, 0, 0.501960f),
                            new Color(0.149019f, 0, 0.345098f)
                        };

                        var car = pmc.ColorAlphaRamp;
                        var colorKeys = car.colorKeys;

                        for (var i = 0; i < ramp.Length; i++)
                        {
                            var ck = colorKeys[i];
                            ck.color = UnityUtil.ModifyHSV(ramp[i], hsv => hsv with { s = hsv.s * 0.2 });
                            colorKeys[i] = ck;
                        }

                        car.colorKeys = colorKeys;
                        pmc.ColorAlphaRamp = car;

                        pmc.TexColorAlphaRamp = rampTexture;
                    }

                    SetRampColors(waveAll.StinkingSmoke00.GetComponent<ParticlesMaterialController>());
                    SetRampColors(waveAll.StinkingSmoke00_RotatableCopy.GetComponent<ParticlesMaterialController>());

                    static void ChangeColorsAndMainTexture(GameObject go)
                    {
                        var psr = go.GetComponent<ParticleSystemRenderer>();

                        var texture = psr.material.GetTexture(ShaderProps._BaseMap) as Texture2D;
                        if (texture is not null)
                            psr.material.SetTexture(ShaderProps._BaseMap, FxColor.ChangeTextureColors(texture,
                                c => UnityUtil.ModifyHSV(new(0, 0, c.b, c.a),
                                    hsv => hsv with { s = Mathf.Clamp01((float)hsv.s * 5), v = 0.1 })));
                        //_ => Color.black));
                        else
                        {
                            MicroLogger.Debug(() => "{go} _BaseMap is null");
                        }

                        var color = psr.material.GetColor(ShaderProps._BaseColor);
                        psr.material.SetColor(ShaderProps._BaseColor, Color.black);

                        //psr.material.SetFloat("_Petrification", 0);
                        //psr.material.SetFloat("_DissolveEnabled", 0);

                        psr.material.SetColor("_PetrificationColor", new(0, 0, 0, 0));
                        psr.material.SetColor("_DissolveColor", new(0, 0, 0, 0));
                        psr.material.SetColor("_TintColor", Color.black);

                        psr.material.SetColor("_RimColor", new(0, 0, 0, 1));
                        psr.material.SetFloat("_RimLighting", 1);

                        //var aam = psr.material.GetTexture("_AdditionalAlbedoMap") as Texture2D;

                        //if (aam is not null)
                        //    psr.material.SetTexture("_AdditionalAlbedoMap", FxColor.ChangeTextureColors(aam, _ => Color.black));

                        var dm = psr.material.GetTexture("_DissolveMap") as Texture2D;

                        if (dm is not null)
                            psr.material.SetTexture("_DissolveMap", FxColor.ChangeTextureColors(dm, _ => Color.black));

                        var mm = psr.material.GetTexture("_MasksMap") as Texture2D;

                        if (mm is not null)
                            psr.material.SetTexture("_MasksMap",
                                FxColor.ChangeTextureColors(mm, c => new Color(1, 0, 0, 1)));
                    }

                    ChangeColorsAndMainTexture(wave00.LianaStart00);
                    ChangeColorsAndMainTexture(wave00.LianaStart00_RotatableCopy);
                    ChangeColorsAndMainTexture(wave00.Liana00);
                    ChangeColorsAndMainTexture(wave00.Liana00_RotatableCopy);

                    ChangeColorsAndMainTexture(wave01.LianaStart00);
                    ChangeColorsAndMainTexture(wave01.LianaStart00_RotatableCopy);
                    ChangeColorsAndMainTexture(wave01.Liana00);
                    ChangeColorsAndMainTexture(wave01.Liana00_RotatableCopy);

                    ChangeColorsAndMainTexture(wave02.LianaStart00);
                    ChangeColorsAndMainTexture(wave02.LianaStart00_RotatableCopy);
                    ChangeColorsAndMainTexture(wave02.Liana00);
                    ChangeColorsAndMainTexture(wave02.Liana00_RotatableCopy);
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
                .Map(buff =>
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
                    var (aoe, buff, difficultTerrain) = bps.Expand();

                    TentacleGrapple GrappleAction() => new()
                    {
                        OnSuccess = new()
                        {
                            Actions = new[] { GameActions.Conditional(conditional =>
                            {
                                conditional.AddCondition(Conditions.HasBuff(c =>
                                    c.m_Buff = buff.ToReference<BlueprintBuffReference>()));

                                conditional.IfFalse.Add(GameActions.ContextActionApplyBuff(a =>
                                {
                                    a.m_Buff = buff.ToReference<BlueprintBuffReference>();
                                    a.Permanent = true;
                                }));
                            }) }
                        },
                        OnFailure = new()
                        {
                            Actions = new[] { GameActions.ContextActionRemoveBuff(a =>
                                a.m_Buff = buff.ToReference<BlueprintBuffReference>()) }
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
                                c.m_Buff = difficultTerrain.ToReference<BlueprintBuffReference>();
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
                                c.m_Buff = buff.ToReference<BlueprintBuffReference>()),
                            GameActions.ContextActionRemoveBuff(c =>
                                c.m_Buff = difficultTerrain.ToReference<BlueprintBuffReference>()));
                    });

                    aoe.Shape = AreaEffectShape.Cylinder;
                    aoe.Fx = Fx.AoeFx;

                    return aoe;
                });

            var ability = context.NewBlueprint<BlueprintAbility>(
                GeneratedGuid.Get("ChillingTentaclesAbility"))
                .Combine(areaEffect)
                .Map(bps =>
                {
                    var (ability, aoe) = bps;

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Greater_ChillingTentacles_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_Invocations_Greater_ChillingTentacles_Description;
                    ability.m_Icon = Sprites.ChillingTentacles;

                    ability.AddComponent<AbilityEffectRunAction>(c =>
                    {
                        c.Actions.Add(GameActions.ContextActionSpawnAreaEffect(a =>
                        {
                            a.m_AreaEffect = aoe.ToReference<BlueprintAbilityAreaEffectReference>();
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
