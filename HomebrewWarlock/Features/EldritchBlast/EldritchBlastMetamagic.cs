using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Util.Linq;

using Owlcat.Runtime.Core.Utils;

using UnityEngine;

namespace HomebrewWarlock.Features.EldritchBlast
{
    internal static class EldritchBlastMetamagic
    {

        [AllowedOn(typeof(BlueprintActivatableAbility))]
        internal class ForceResourceCount : ActivatableAbilityRestriction, IUnitAbilityResourceHandler
        {
            public virtual int RequiredAmount { get; set; } = 1;

            IEnumerable<BlueprintAbilityResource> Resources =>
                Fact.BlueprintComponents.OfType<ActivatableAbilityResourceLogic>().Select(aarl => aarl.RequiredResource);

            public override bool IsAvailable() =>
                this.Resources.Any(blueprint => base.Owner.Resources.HasEnoughResource(blueprint, RequiredAmount));

            public void HandleAbilityResourceChange(UnitEntityData unit, UnitAbilityResource resource, int oldAmount)
            {
                if (unit != base.Owner)
                    return;

                if (base.Fact.Blueprint is not { } blueprint) return;

                if (this.Resources.Any(r => resource.Blueprint == r) && !IsAvailable())
                    base.Fact.TurnOffImmediately();
            }

            [HarmonyPatch(typeof(ActivatableAbility), nameof(ActivatableAbility.ResourceCount), MethodType.Getter)]
            static class Patch
            {
                static int? Postfix(int? result, ActivatableAbility __instance)
                {
                    if (!__instance.BlueprintComponents.OfType<ForceResourceCount>().Any())
                        return result;

                    if (__instance.BlueprintComponents.OfType<ActivatableAbilityResourceLogic>().FirstOrDefault()
                        is not { } component)
                        return result;

                    return __instance.Owner.Resources.GetResourceAmount(component.RequiredResource);
                }
            }
        }

        internal class ContextCasterHasSwiftAction : ContextCondition
        {
            public override bool CheckCondition()
            {
                var result = base.Context.MaybeCaster?.HasSwiftAction() ?? false;

                MicroLogger.Debug(() => $"{base.Context.MaybeCaster} has swift action? {result}");

                return result;
            }

            public override string GetConditionCaption() => "Caster has swift action";
        }

        // Not currently needed
        //[HarmonyPatch]
        static class ContextActionCastSpell_Metamagic_Patch
        {
            static RuleCastSpell AddMetamagicFromContext(RuleCastSpell rule, MechanicsContext context, ContextActionCastSpell cast)
            {

                MicroLogger.Debug(sb =>
                {
                    sb.AppendLine($"Source Ability Blueprint: {context?.SourceAbilityContext?.AbilityBlueprint}");
                    sb.AppendLine($"Spell: {cast?.Spell}");
                    sb.AppendLine($"Mark as child? {cast?.MarkAsChild}");
                    sb.AppendLine($"Source context metamagic: {(int?)context?.SourceAbilityContext?.Params?.Metamagic ?? -1}");
                    sb.AppendLine($"This context metamagic: {(int?)context?.Params?.Metamagic ?? -1}");
                    sb.AppendLine($"Rule context metamagic: {(int?)rule?.Context?.Params?.Metamagic ?? -1}");
                });

                if (!cast.MarkAsChild)
                    return rule;

                if (rule is null)
                {
                    MicroLogger.Debug(() => "rule is null");
                    return rule!;
                }

                if (rule.Context is null)
                {
                    MicroLogger.Debug(() => "rule.Context is null");
                    return rule;
                }

                if (rule.Context.Ability is null)
                {
                    MicroLogger.Debug(() => "rule.Context.Ability is null");
                    return rule;
                }

                if (context is null)
                {
                    MicroLogger.Debug(() => "context is null");
                    return rule;
                }

                if (context.Params is null)
                {
                    MicroLogger.Debug(() => "context.Params is null");
                    return rule;
                }

                (rule.Context.Ability.MetamagicData ??= new()).MetamagicMask |= context.Params.Metamagic;

                return rule;
            }

            [HarmonyPatch(typeof(ContextActionCastSpell), nameof(ContextActionCastSpell.RunAction))]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var match = instructions
                    .FindInstructionsIndexed(new Func<CodeInstruction, bool>[]
                    {
                        ci =>
                        {
                            if (ci.opcode != OpCodes.Call)
                                return false;

                            if (ci.operand is not MethodInfo mi)
                                return false;

                            return mi.Name == nameof(Rulebook.Trigger) &&
                                mi.GetGenericArguments().Length == 1 &&
                                mi.GetGenericArguments()[0] == typeof(RuleCastSpell);
                        },
                        ci => ci.opcode == OpCodes.Pop,
                        ci => ci.opcode == OpCodes.Ret
                    })
                    .ToArray();

                if (match.Length != 3)
                    return instructions;

                var index = match[0].index;

                var iList = instructions.ToList();

                iList.InsertRange(index, new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ContextAction), nameof(ContextAction.Context))),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    CodeInstruction.Call(
                        typeof(ContextActionCastSpell_Metamagic_Patch),
                        nameof(AddMetamagicFromContext),
                        new[] { typeof(RuleCastSpell), typeof(AbilityExecutionContext), typeof(ContextActionCastSpell) })

                });

                //MicroLogger.Debug(sb =>
                //{
                //    foreach (var i in iList.Indexed().Skip(index))
                //    {
                //        sb.AppendLine();
                //        sb.Append($"{i.index}: {i.item}");
                //    }
                //});

                return iList;
            }
        }

        internal static BlueprintInitializationContext.ContextInitializer<(BlueprintFeature, BlueprintBuff)> EldritchBlastMetamagicFeature(
            Metamagic type,
            BlueprintInitializationContext.ContextInitializer<BlueprintAbilityResource> resource,
            BlueprintInitializationContext.ContextInitializer<BlueprintBuff> buff,
            BlueprintInitializationContext.ContextInitializer<BlueprintActivatableAbility> toggle,
            BlueprintInitializationContext.ContextInitializer<BlueprintActivatableAbility> parent,
            BlueprintInitializationContext.ContextInitializer<BlueprintFeature> feature,
            LocalizedString displayName,
            LocalizedString description,
            Func<Sprite> getIcon)
        {
            resource = resource.Map(resource => 
            {
                resource.m_MaxAmount = new() { BaseValue = 3 };

                return resource;
            });

            buff = buff
                .Combine(resource)
                .Map(bps =>
                {
                    (BlueprintBuff buff, var resource) = bps;

                    buff.m_Flags = BlueprintBuff.Flags.HiddenInUi;

                    buff.AddAutoMetamagic(amm =>
                    {
                        amm.Metamagic = type;
                        amm.Abilities = EldritchBlastFeatures.BlastAbilities.ToList();
                        amm.m_AllowedAbilities = AutoMetamagic.AllowedType.Any;
                    });

                    buff.AddAddAbilityUseTrigger(onAbilityUse =>
                    {
                        onAbilityUse.ForMultipleSpells = true;
                        onAbilityUse.Abilities = EldritchBlastFeatures.BlastAbilities.ToList();

                        //onAbilityUse.CheckAbilityType = true;
                        //onAbilityUse.Type = AbilityType.Special;

                        onAbilityUse.Action.Add(GameActions.ContextSpendResource(spendResource =>
                            spendResource.m_Resource = resource.ToReference()));

                        onAbilityUse.AfterCast = true;

                        onAbilityUse.OnlyOnce = true;
                    });

                    return buff;
                });

            toggle = toggle
                .Combine(buff)
                .Combine(resource)
                .Map(bps =>
                {
                    (BlueprintActivatableAbility toggle, var buff, var resource) = bps.Expand();

                    toggle.m_DisplayName = displayName;
                    toggle.m_Description = description;
                    toggle.m_Icon = getIcon();

                    toggle.AddActivatableAbilityResourceLogic(resourceLogic =>
                    {
                        resourceLogic.m_RequiredResource = resource.ToReference();
                        resourceLogic.SpendType = ActivatableAbilityResourceLogic.ResourceSpendType.None;
                    });

                    toggle.AddComponent<ForceResourceCount>();

                    toggle.m_Buff = buff.ToReference();

                    toggle.HiddenInUI = true;

                    toggle.DeactivateImmediately = true;

                    return toggle;
                });

            var featureAndBuff = feature
                .Combine(toggle)
                .Combine(resource)
                .Combine(parent)
                .Map(bps =>
                {
                    (BlueprintFeature feature, var toggle, var resource, var parent) = bps.Expand();

                    var variants = parent.EnsureComponent<ActivatableAbilityVariants>();
                    variants.m_Variants ??= Array.Empty<BlueprintActivatableAbilityReference>();
                    variants.m_Variants = variants.m_Variants.AppendValue(toggle.ToReference()).ToArray();

                    feature.m_DisplayName = toggle.m_DisplayName;
                    feature.m_Description = toggle.m_Description;
                    feature.m_Icon = toggle.m_Icon;

                    feature.AddAddAbilityResources(addResources =>
                    {
                        addResources.m_Resource = resource.ToReference();
                        addResources.Amount = 3;
                        addResources.RestoreAmount = true;
                    });

                    feature.AddAddFeatureIfHasFact(addIfHasFact =>
                    {
                        addIfHasFact.Not = true;

                        addIfHasFact.m_CheckedFact = parent.ToReference<BlueprintUnitFactReference>();
                        addIfHasFact.m_Feature = parent.ToReference<BlueprintUnitFactReference>();
                    });

                    feature.AddAddFacts(af => af.m_Facts = new[] { toggle.ToReference<BlueprintUnitFactReference>() });

                    feature.AddPrerequisiteFeature(EldritchBlast.FeatureRef);

                    return feature;
                })
                .Combine(buff);

            return featureAndBuff;
        }

        [LocalizedString]
        internal const string EmpowerDisplayName = "Empower Spell-Like Ability (Eldritch Blast)";

        [LocalizedString]
        internal static readonly string EmpowerDescription =
            "You can use Eldritch Blasts (including shape and essence invocations) as an empowered spell-like " +
            "ability three times per day." +
            Environment.NewLine +
            "When you use an empowered spell-like ability, all variable, numeric effects of the spell-like " +
            "ability are increased by half (+50%). Saving throws and opposed rolls are not affected. Spell-like " +
            "abilities without random variables are not affected.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> CreateEmpower(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BlueprintActivatableAbility> parent)
        {
            return EldritchBlastMetamagicFeature(Metamagic.Empower,
                context.NewBlueprint<BlueprintAbilityResource>(GeneratedGuid.Get("EmpowerEldritchBlastResource")),
                context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("EmpowerEldritchBlastBuff")),
                context.NewBlueprint<BlueprintActivatableAbility>(GeneratedGuid.Get("EmpowerEldritchBlastToggleAbility")),
                parent,
                context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("EmpowerEldritchBlastFeature")),
                LocalizedStrings.Features_EldritchBlast_EldritchBlastMetamagic_EmpowerDisplayName,
                LocalizedStrings.Features_EldritchBlast_EldritchBlastMetamagic_EmpowerDescription,
                () => Sprites.EmpowerSpell)
                .Map(bps =>
                {
                    (BlueprintFeature feature, var _) = bps;

                    feature.AddComponent<PrerequisiteClassLevel>(pcl =>
                    {
                        pcl.m_CharacterClass = WarlockClass.Blueprint.ToReference();
                        pcl.Level = 6;
                    });

                    return feature;
                });
        }

        [LocalizedString]
        internal const string QuickenDisplayName = "Quicken Spell-like Ability (Eldritch Blast)";

        [LocalizedString]
        internal static readonly string QuickenDescription =
            "You can use Eldritch Blasts (including shape and essence blasts) as a quickened spell-like ability " +
            "three times per day." +
            Environment.NewLine +
            "Using a quickened spell-like ability is a swift action that does not provoke an attack of opportunity. " +
            "You can perform another action—including the use of another spell-like ability (but not another swift " +
            "action)—in the same round that you use a quickened spell-like ability. You may use only one " +
            "quickened spell-like ability per round.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> CreateQuicken(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BlueprintActivatableAbility> parent)
        {

            var featureAndBuff = EldritchBlastMetamagicFeature(Metamagic.Quicken,
                context.NewBlueprint<BlueprintAbilityResource>(GeneratedGuid.Get("QuickenEldritchBlastResource")),
                context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("QuickenEldritchBlastBuff")),
                context.NewBlueprint<BlueprintActivatableAbility>(GeneratedGuid.Get("QuickenEldritchBlastToggleAbility")),
                parent,
                context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("QuickenEldritchBlastFeature")),
                LocalizedStrings.Features_EldritchBlast_EldritchBlastMetamagic_QuickenDisplayName,
                LocalizedStrings.Features_EldritchBlast_EldritchBlastMetamagic_QuickenDescription,
                () => Sprites.QuickenSpell);

            return featureAndBuff
                .Map(bps =>
                {
                    (BlueprintFeature feature, BlueprintBuff buff) = bps;

                    var trigger = buff.GetComponent<AddAbilityUseTrigger>();

                    var actions = trigger.Action;

                    trigger.Action = Default.ActionList;

                    trigger.Action.Add(GameActions.Conditional(c =>
                    {
                        c.ConditionsChecker.Add(new ContextCasterHasSwiftAction());
                        c.IfTrue = actions;
                    }));

                    trigger.AfterCast = false;

                    feature.AddComponent<PrerequisiteClassLevel>(pcl =>
                    {
                        pcl.m_CharacterClass = WarlockClass.Blueprint.ToReference();
                        pcl.Level = 10;
                    });

                    return feature;
                });
        }

        [LocalizedString]
        internal const string MaximizeDisplayName = "Maximize Spell-like Ability (Eldritch Blast)";

        [LocalizedString]
        internal static readonly string MaximizeDescription =
            "You can use Eldritch Blasts (including shape and essence invocations) as a maximized spell-like " +
            "ability three times per day." +
            Environment.NewLine +
            "All variable, numeric effects of the spell-like ability are maximized. Saving throws and opposed rolls " +
            "are not affected, nor are effects without random variables.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> CreateMaximize(BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BlueprintActivatableAbility> parent)
        {
            var featureAndBuff = EldritchBlastMetamagicFeature(Metamagic.Maximize,
                context.NewBlueprint<BlueprintAbilityResource>(GeneratedGuid.Get("MaximizeEldritchBlastResource")),
                context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("MaximizeEldritchBlastBuff")),
                context.NewBlueprint<BlueprintActivatableAbility>(GeneratedGuid.Get("MaximizeEldritchBlastToggleAbility")),
                parent,
                context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("MaximizeEldritchBlastFeature")),
                LocalizedStrings.Features_EldritchBlast_EldritchBlastMetamagic_MaximizeDisplayName,
                LocalizedStrings.Features_EldritchBlast_EldritchBlastMetamagic_MaximizeDescription,
                () => Sprites.MaximizeSpell);

            return featureAndBuff
                .Map(bps =>
                {
                    var (feature, _) = bps;

                    feature.AddComponent<PrerequisiteClassLevel>(pcl =>
                    {
                        pcl.m_CharacterClass = WarlockClass.Blueprint.ToReference();
                        pcl.Level = 8;
                    });

                    return feature;
                });
        }

        [LocalizedString]
        internal const string DisplayName = "Spell-like Ability Metamagic (Eldritch Blast)";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintActivatableAbility> CreateParentAbility(
            BlueprintInitializationContext context)
        {
            var ability = context.NewBlueprint<BlueprintActivatableAbility>(GeneratedGuid.Get("EldritchBlastMetadataParentAbility"))
                .Map(aa =>
                {
                    aa.m_DisplayName = LocalizedStrings.Features_EldritchBlast_EldritchBlastMetamagic_DisplayName;
                    aa.m_Icon = Sprites.MetamagicMastery;

                    aa.AddActivationDisable();

                    return aa;
                });

            return ability;
        }

        [Init]
        static void Init()
        {
            var context = new BlueprintInitializationContext(Triggers.BlueprintsCache_Init);
            var parent = CreateParentAbility(context);

            CreateEmpower(context, parent)
                .Combine(CreateQuicken(context, parent))
                .GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeatureSelection.BasicFeatSelection)
                .Map(bps =>
                {
                    var (empower, quicken, basicFeats) = bps.Expand();

                    basicFeats.AddFeatures(empower, quicken);
                })
                .Register();
        }
    }
}
