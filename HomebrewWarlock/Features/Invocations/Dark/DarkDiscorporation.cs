using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using HomebrewWarlock.Fx;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem.Converters;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.Visual;
using Kingmaker.Visual.Particles;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

using UnityEngine;

namespace HomebrewWarlock.Features.Invocations.Dark
{
    internal class SetMovementSpeed : UnitFactComponentDelegate
    {
        public override void OnTurnOn() =>
            base.Owner.Stats.Speed.AddModifierUnique(
                this.Value - base.Owner.Stats.Speed.BaseValue, base.Runtime, this.Descriptor);
        public override void OnTurnOff() => base.Owner.Stats.Speed.RemoveModifiersFrom(base.Runtime);

        public int Value = 30;

        public ModifierDescriptor Descriptor;
    }

    internal class DDPolymorph : Polymorph
    {
        public void SetupView(UnitEntityView view)
        {
            foreach (var smr in view.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.material.color = Color.black;
            }
        }

        public BlueprintBrainReference? Brain;

        public override void OnActivate()
        {
            base.OnActivate();

            if (this.Brain is not null)
            {
                base.Owner.Brain.SetBrain(Brain);
                base.Owner.Brain.RestoreAvailableActions();
            }
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();

            if (this.Brain is not null)
            {
                base.Owner.Brain.SetBrain(base.Owner.OriginalBlueprint.DefaultBrain);
                base.Owner.Brain.RestoreAvailableActions();
            }
        }

        [HarmonyPatch(typeof(Polymorph), nameof(Polymorph.CreatePolymorphView))]
        static class CreatePolymorphView_Patch
        {
            static UnitEntityView Postfix(UnitEntityView result, Polymorph __instance)
            {
                if (__instance is not DDPolymorph ddp)
                    return result;

                ddp.SetupView(result);

                return result;
            }
        }
    }

    internal class AlignDamageWithCaster : UnitFactComponentDelegate, IInitiatorRulebookHandler<RulePrepareDamage>
    {
        public void OnEventAboutToTrigger(RulePrepareDamage evt)
        {
            if (!(evt.Reason.Fact?.Blueprint?.Components?.OfType<AlignDamageWithCaster>()?.Any() ?? false))
                return;

            MicroLogger.Debug(sb =>
            {
                sb.AppendLine($"Damage Reason: {evt.Reason.Name}");
                sb.AppendLine($"  SourceEntity: {evt.Reason.SourceEntity}");
                sb.AppendLine($"  Caster: {evt.Reason.Caster}");
                sb.Append($"  Fact: {evt.Reason.Fact}");
            });

            if (evt.Reason.Fact.Blueprint != this.Fact.Blueprint)
                return;

            if (evt.Reason.Caster is not { } caster)
                return;

            var casterAlignment = caster.Alignment.ValueVisible;

            MicroLogger.Debug(() => $"Caster alignment: {casterAlignment}");

            foreach (var alignment in casterAlignment.ToDamageAlignments())
            {
                MicroLogger.Debug(() => $"Adding {alignment} to damage");

                foreach (var damage in evt.DamageBundle)
                {
                    damage.AddAlignment(alignment);
                }
            }
        }

        public void OnEventDidTrigger(RulePrepareDamage evt) { }
    }

    [HarmonyPatch]
    internal static class DarkDiscorporation
    {
        [HarmonyPatch(typeof(UnitCommands), nameof(UnitCommands.Run), typeof(UnitCommand))]
        [HarmonyPostfix]
        static void Patch(UnitCommand cmd)
        {
            if (cmd is UnitAttack)
                MicroLogger.Debug(() => $"Attack command {cmd}");
        }

        [LocalizedString]
        internal const string DisplayName = "Dark Discorporation";

        [LocalizedString]
        internal static readonly string Description =
            "One with the powers of darkness, you learn to abandon your body. When you use this ability, you become " +
            "Huge a swarm of Diminutive, batlike shadows." +
            Environment.NewLine +
            "In this swarmlike form, you gain the following characteristics and traits:" +
            Environment.NewLine +
            "Your Strength score drops to 1, but your Dexterity score increases by 6." +
            Environment.NewLine +
            "You gain a +4 size bonus to AC, and a deflection bonus to AC equal to your Charisma modifier." +
            Environment.NewLine +
            "You gain a fly speed of 40 feet" +
            Environment.NewLine +
            "You are immune to weapon damage, critical hits, and combat maneuvers. You cannot be flanked. You are " +
            "immune to any spell or effect that targets a specific number of creatures, except for mind-affecting " +
            "spells and abilities. You take half again as much damage (+50%) from spells or effects that affect an " +
            "area. If reduced to 0 hit points or less, or rendered unconscious by nonlethal damage, you instantly " +
            "return to your normal form." +
            Environment.NewLine +
            "You gain a swarm attack that deals 4d6 points of damage to any creature whose space you occupy at the " +
            //"end " +
            "start " +
            "of your turn. Your swarm attack strikes as a magic weapon of your alignment." +
            Environment.NewLine +
            "Any living creature vulnerable to your swarm attack that begins its turn in a square occupied by your " +
            "swarm must make a Fortitude save or be nauseated for 1 round. Spellcasting or concentrating on spells " +
            "within the area of your swarm requires a Concentration check (DC15 + 2 \u00d7 spell level)" +
            Environment.NewLine +
            "You can take only move actions (you cannot use other invocations) while under the effect of dark discorporation.";

        [LocalizedString]
        internal const string ShortDescription =
            "One with the powers of darkness, you learn to abandon your body. When you use this ability, you become " +
            "Huge a swarm of Diminutive, batlike shadows.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var areaBuff = context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("DarkDiscorporationAreaEffectBuff"))
                .Combine(BlueprintsDb.Owlcat.BlueprintBuff.Nauseated)
                .Map(bps =>
                {
                    var (buff, nauseated) = bps;

                    //buff.m_Flags = BlueprintBuff.Flags.HiddenInUi;

                    buff.AddComponent<AddFactContextActions>(c =>
                    {
                        c.Activated.Add(GameActions.ContextActionSwarmTarget());
                        c.Deactivated.Add(GameActions.ContextActionSwarmTarget(swarmTarget => swarmTarget.Remove = true));
                        c.NewRound.Add(
                            GameActions.ContextActionSavingThrow(st =>
                            {
                                st.Type = SavingThrowType.Fortitude;
                                st.FromBuff = true;
                                st.Actions.Add(GameActions.ContextActionConditionalSaved(save =>
                                {
                                    save.Failed.Add(GameActions.ContextActionApplyBuff(ab =>
                                    {
                                        ab.m_Buff = nauseated.ToReference<BlueprintBuffReference>();
                                        ab.DurationValue.BonusValue = 1;
                                        ab.IsNotDispelable = true;
                                        ab.AsChild = false;
                                    }));
                                }));
                            }));
                    });

                    buff.AddComponent<ContextCalculateAbilityParams>(c =>
                    {
                        c.StatType = StatType.Charisma;
                        c.ReplaceSpellLevel = true;
                        c.SpellLevel = 8;
                    });

                    buff.AddAddCondition(c => c.Condition = UnitCondition.SpellCastingIsVeryDifficult);

                    return buff;
                });

            var area = context.NewBlueprint<BlueprintAbilityAreaEffect>(GeneratedGuid.Get("DarkDiscorporationArea"))
                .Combine(areaBuff)
                .Map (bps =>
                {
                    var (area, areaBuff) = bps;

                    area.Shape = AreaEffectShape.Cylinder;
                    area.Size = 5.Feet();

                    area.AddComponent<AbilityAreaEffectBuff>(c =>
                    {
                        //c.Condition.Add(Conditions.ContextConditionIsEnemy());
                        c.Condition.Add(Conditions.ContextConditionIsCaster(isCaster => isCaster.Not = true));
                        c.m_Buff = areaBuff.ToReference<BlueprintBuffReference>();
                    });

                    area.Fx = new PrefabLink() { AssetId = "627d8b7231dd249418a21735d12d6a69" }
                        .CreateDynamicProxy(static prefab =>
                        {
                            //UnityEngine.GameObject.DestroyImmediate(prefab.transform.Find("Point light").gameObject);
                            UnityEngine.GameObject.DestroyImmediate(prefab.transform.Find("Root/DropsWithTrail")?.gameObject);
                            UnityEngine.GameObject.DestroyImmediate(prefab.transform.Find("Root/DropsWithTrail (1)")?.gameObject);

                            FxColor.ChangeAllColors(prefab, _ => Color.black);

                            prefab.transform.localScale *= 0.25f;

                            var stl = prefab.AddComponent<SnapToLocator>();
                            stl.BoneName = "Locator_GroundFX";
                            stl.DontRotate = true;
                            stl.DontScale = true;
                        });

                    return area;
                });

            var swarmBuff = context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("DarkDiscorporationSwarmDamageBuff"))
                .Combine(area)
                .Map(bps =>
                {
                    var (swarmBuff, area) = bps;

                    swarmBuff.m_AllowNonContextActions = true;
                    swarmBuff.m_Flags = BlueprintBuff.Flags.HiddenInUi;

                    swarmBuff.AddComponent<AddAreaEffect>(c =>
                    {
                        c.m_AreaEffect = area.ToReference<BlueprintAbilityAreaEffectReference>();
                    });

                    swarmBuff.AddComponent<AddFactContextActions>(c =>
                    {
                        c.NewRound.Add(GameActions.ContextActionOnSwarmTargets(swarmTargets =>
                        {
                            swarmTargets.Actions.Add(
                                GameActions.ContextActionDealDamage(a =>
                                {
                                    a.m_IsAOE = true;

                                    a.DamageType.Type = DamageType.Physical;

                                    a.DamageType.Physical.Form =
                                        PhysicalDamageForm.Bludgeoning |
                                        PhysicalDamageForm.Piercing |
                                        PhysicalDamageForm.Slashing;

                                    a.DamageType.Physical.EnhancementTotal = a.DamageType.Physical.Enhancement = 1;

                                    a.Value.DiceType = DiceType.D6;
                                    a.Value.DiceCountValue.Value = 4;
                                }),
                                GameActions.Conditional(swarmHasEnemies =>
                                {
                                    swarmHasEnemies.AddCondition(Conditions.ContextSwarmHasEnemiesInInnerCircle());
                                    swarmHasEnemies.IfTrue.Add(
                                        GameActions.PlayAnimationOneShot(animation =>
                                        {
                                            animation.m_ClipWrapper = new AnimationClipWrapperLink() { AssetId = "8e36e9ef86ac1884695f930a558314bf" };
                                            animation.Unit = new FactOwner();
                                            animation.TransitionIn = 0.25f;
                                            animation.TransitionOut = 0.25f;
                                        }));
                                })
                                );
                        }));
                    });

                    swarmBuff.AddComponent<AlignDamageWithCaster>();

                    swarmBuff.FxOnStart = new() { AssetId = "f1f41fef03cb5734e95db1342f0c605e" };

                    return swarmBuff;
                });

            var polymorphBuff = context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("DarkDiscorporationPolymorphBuff"))
                .Combine(BlueprintsDb.Owlcat.BlueprintUnitAsksList.SwarmCrows_Barks)
                .Combine(BlueprintsDb.Owlcat.BlueprintFeature.Airborne)
                .Combine(BlueprintsDb.Owlcat.BlueprintFeature.ImmunityToBleed)
                .Combine(BlueprintsDb.Owlcat.BlueprintBrain.SwarmBrain)
                .Combine(swarmBuff)
                .Map(bps =>
                {
                    var (buff, barks, airborne, bleedImmunity, swarmBrain, swarmBuff) = bps.Expand();

                    buff.m_Flags = BlueprintBuff.Flags.HiddenInUi | BlueprintBuff.Flags.RemoveOnRest;

                    buff.AddComponent<DDPolymorph>(c =>
                    {
                        c.m_Prefab = new UnitViewLink() { AssetId = "20b7cc201aa42464f90c99f050bc3a37" };

                        c.m_TransitionExternal =
                            UnityObjectConverter.AssetList.Get("5687f6801ca6ea848a8d48298d892a8e", 11400000)
                                as PolymorphTransitionSettings;

                        if (c.m_TransitionExternal is not null)
                        {
                            c.m_TransitionExternal.EnterTransition.OldPrefabFX =
                                c.m_TransitionExternal.EnterTransition.OldPrefabFX.CreateDynamicProxy(prefab =>
                                {
                                    FxColor.ChangeAllColors(prefab, _ => Color.black);
                                });

                            c.m_TransitionExternal.EnterTransition.NewPrefabFX =
                                c.m_TransitionExternal.EnterTransition.NewPrefabFX.CreateDynamicProxy(prefab =>
                                {
                                    FxColor.ChangeAllColors(prefab, _ => Color.black);
                                });

                            c.m_TransitionExternal.ExitTransition.OldPrefabFX =
                                c.m_TransitionExternal.EnterTransition.OldPrefabFX.CreateDynamicProxy(prefab =>
                                {
                                    FxColor.ChangeAllColors(prefab, _ => Color.black);
                                });

                            c.m_TransitionExternal.ExitTransition.NewPrefabFX =
                                c.m_TransitionExternal.EnterTransition.NewPrefabFX.CreateDynamicProxy(prefab =>
                                {
                                    FxColor.ChangeAllColors(prefab, _ => Color.black);
                                });
                        }

                        c.Brain = swarmBrain.ToReference<BlueprintBrainReference>();

                        c.m_Facts = new[]
                        {
                            airborne.ToReference<BlueprintUnitFactReference>(),
                            bleedImmunity.ToReference<BlueprintUnitFactReference>()
                        };
                        
                        c.Size = Size.Huge;

                        c.StrengthBonus = -99;
                        c.DexterityBonus = 6;
                        
                        //c.m_SilentCaster = true;
                    });

                    buff.AddReplaceAsksList(c => c.m_Asks = barks.ToReference<BlueprintUnitAsksListReference>());

                    buff.AddComponent<SetMovementSpeed>(c => c.Value = 40);
                    
                    buff.AddAddStatBonus(c =>
                    {
                        c.Stat = StatType.AC;
                        c.Descriptor = ModifierDescriptor.Size;
                        c.Value = 4;
                    });

                    buff.AddAddContextStatBonus(c =>
                    {
                        c.Stat = StatType.AC;
                        c.Descriptor = ModifierDescriptor.Deflection;
                        c.Value.ValueType = ContextValueType.Rank;
                        c.Value.ValueRank = AbilityRankType.StatBonus;
                    });

                    buff.AddContextRankConfig(c =>
                    {
                        c.m_BaseValueType = ContextRankBaseValueType.StatBonus;
                        c.m_Stat = StatType.Charisma;
                        c.m_Type = AbilityRankType.StatBonus;
                    });

                    buff.AddAddConditionImmunity(c => c.Condition = UnitCondition.DifficultTerrain);

                    buff.AddAddMechanicsFeature(c => c.m_Feature = AddMechanicsFeature.MechanicsFeatureType.CannotBeFlanked);

                    buff.AddAddImmunityToCriticalHits();
                    buff.AddAddImmunityToPrecisionDamage();
                    buff.AddComponent<SwarmDamageResistance>(c => c.DiminutiveOrLower = true);
                    buff.AddComponent<SwarmAoeVulnerability>();

                    buff.AddAddSpellImmunity(c =>
                    {
                        c.Type = SpellImmunityType.SingleTarget;
                        c.SpellDescriptor = SpellDescriptor.MindAffecting;
                        c.InvertedDescriptors = true;
                    });

                    //buff.AddManeuverImmunity(c => c.Type = CombatManeuver.Trip);
                    //buff.AddManeuverImmunity(c => c.Type = CombatManeuver.Grapple);
                    //buff.AddManeuverImmunity(c => c.Type = CombatManeuver.BullRush);
                    //buff.AddManeuverImmunity(c => c.Type = CombatManeuver.Overrun);
                    //buff.AddManeuverImmunity(c => c.Type = CombatManeuver.Disarm);
                    //buff.AddManeuverImmunity(c => c.Type = CombatManeuver.SunderArmor);
                    //buff.AddManeuverImmunity(c => c.Type = CombatManeuver.DirtyTrickBlind);
                    //buff.AddManeuverImmunity(c => c.Type = CombatManeuver.DirtyTrickEntangle);
                    //buff.AddManeuverImmunity(c => c.Type = CombatManeuver.DirtyTrickSickened);
                    //buff.AddManeuverImmunity(c => c.Type = CombatManeuver.Pull);

                    buff.AddAddCondition(c => c.Condition = UnitCondition.CanNotAttack);
                    buff.AddAddCondition(c => c.Condition = UnitCondition.CantAct);
                    buff.AddAddCondition(c => c.Condition = UnitCondition.DisableAttacksOfOpportunity);
                    buff.AddAddCondition(c => c.Condition = UnitCondition.UseAbilityForbidden);
                    buff.AddAddCondition(c => c.Condition = UnitCondition.ImmuneToCombatManeuvers);

                    buff.AddComponent<AddFactContextActions>(c =>
                    {
                        c.Activated.Add(GameActions.ContextActionApplyBuff(applyBuff =>
                        {
                            applyBuff.m_Buff = swarmBuff.ToReference<BlueprintBuffReference>();
                            applyBuff.IsNotDispelable = true;
                            applyBuff.AsChild = true;
                            applyBuff.ToCaster = true;
                            applyBuff.Permanent = true;
                        }));
                    });

                    return buff;
                });

            var ability = context.NewBlueprint<BlueprintActivatableAbility>(GeneratedGuid.Get("DarkDiscorporationPolymorphToggle"))
                .Combine(polymorphBuff)
                .Map(bps =>
                {
                    var (ability, buff) = bps;

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Dark_DarkDiscorporation_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_Invocations_Dark_DarkDiscorporation_Description;
                    ability.m_DescriptionShort = LocalizedStrings.Features_Invocations_Dark_DarkDiscorporation_ShortDescription;

                    ability.m_Buff = buff.ToReference<BlueprintBuffReference>();
                    ability.m_ActivateWithUnitCommand = UnitCommand.CommandType.Standard;

                    //ability.AddComponent<ActivatableAbilityUnitCommand>(c => c.Type = UnitCommand.CommandType.Standard);

                    ability.DeactivateImmediately = true;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("DarkDiscorporationFeature"))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName = ability.m_DisplayName;
                    feature.m_Description = ability.m_Description;
                    feature.m_DescriptionShort = ability.m_DescriptionShort;
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return feature;
                });

            return feature;
        }
    }
}
