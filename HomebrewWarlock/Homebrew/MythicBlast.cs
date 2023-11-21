using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using HomebrewWarlock.Features.EldritchBlast;
using HomebrewWarlock.NewComponents;
using HomebrewWarlock.Features.Invocations;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Util.Linq;

using UniRx;

namespace HomebrewWarlock.Homebrew
{
    internal static class MythicBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Mythic Eldritch Blast";

        [LocalizedString]
        internal const string Description =
            "As a full-round action, you can cast an eldritch blast that deals an additional 1d6 + 1 divine damage per mythic rank.";

        internal static readonly IMicroBlueprint<BlueprintBuff> BuffRef = GeneratedGuid.MythicEldritchBlastBuff.ToMicroBlueprint<BlueprintBuff>();
        internal static readonly IMicroBlueprint<BlueprintBuff> CastBuffRef = GeneratedGuid.MythicEldritchBlastCastBuff.ToMicroBlueprint<BlueprintBuff>();

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var castBuff = context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("MythicEldritchBlastCastBuff"))
                .Map(buff =>
                {
                    buff.m_DisplayName = LocalizedStrings.Homebrew_MythicBlast_DisplayName;
                    buff.m_Description = LocalizedStrings.Homebrew_MythicBlast_Description;
                    buff.m_Icon = Sprites.EldritchBlastMythic;

                    buff.AddContextRankConfig(crc =>
                    {
                        crc.m_BaseValueType = ContextRankBaseValueType.MythicLevel;
                    });

                    buff.AddComponent<AddDamageToEldritchBlast>(c =>
                    {
                        c.DamageType.Type = DamageType.Energy;
                        c.DamageType.Energy = DamageEnergyType.Divine;

                        c.Value.DiceType = DiceType.D6;
                        c.Value.DiceCountValue.ValueType = ContextValueType.Rank;

                        c.Value.BonusValue.ValueType = ContextValueType.Rank;
                    });

                    return buff;
                });

            var buff = context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("MythicEldritchBlastBuff"))
                .Combine(castBuff)
                .Map(bps =>
                {
                    (BlueprintBuff buff, var castBuff) = bps;

                    buff.m_Flags = BlueprintBuff.Flags.HiddenInUi;

                    buff.AddComponent<ChangeAbilityCommandType>(changeCT => 
                    {
                        changeCT.RequireCommandType = UnitCommand.CommandType.Standard;

                        changeCT.ToFullRoundAction = true;
                        
                        changeCT.Abilities = EldritchBlastFeatures.BlastAbilities.ToArray();
                    });

                    buff.AddAddAbilityUseTrigger(onAbilityUse =>
                    {
                        onAbilityUse.ForMultipleSpells = true;
                        onAbilityUse.Abilities = EldritchBlastFeatures.BlastAbilities.ToList();

                        onAbilityUse.Action.Add(GameActions.ContextActionApplyBuff(ab =>
                        {
                            ab.ToCaster = true;

                            ab.m_Buff = castBuff.ToReference();

                            ab.AsChild = true;
                            ab.DurationValue.Rate = DurationRate.Rounds;
                            ab.DurationValue.BonusValue = 1;
                        }));

                        onAbilityUse.OnlyOnce = true;
                    });

                    buff.AddAddAbilityUseTrigger(onAbilityUse =>
                    {
                        onAbilityUse.ForMultipleSpells = true;
                        onAbilityUse.Abilities = EldritchBlastFeatures.BlastAbilities.ToList();

                        onAbilityUse.AfterCast = true;
                    });

                    return buff;
                });

            var toggle = context.NewBlueprint<BlueprintActivatableAbility>(GeneratedGuid.Get("MythicEldritchBlastToggleAbility"))
                .Combine(buff)
                .Map(bps =>
                {
                    (BlueprintActivatableAbility toggle, var buff) = bps;

                    toggle.m_DisplayName = LocalizedStrings.Homebrew_MythicBlast_DisplayName;
                    toggle.m_Description = LocalizedStrings.Homebrew_MythicBlast_Description;
                    toggle.m_Icon = Sprites.EldritchBlastMythic;

                    toggle.m_Buff = buff.ToReference();

                    toggle.DeactivateImmediately = true;

                    return toggle;
                });
                

            var feature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("MythicEldritchBlastFeature"))
                .Combine(toggle)
                .Map(bps =>
                {
                    var (feature, toggle) = bps;

                    feature.m_DisplayName = toggle.m_DisplayName;
                    feature.m_Description = toggle.m_Description;
                    feature.m_Icon = toggle.m_Icon;

                    feature.AddAddFacts(af => af.m_Facts = new[] { toggle.ToReference<BlueprintUnitFactReference>() });

                    //feature.AddComponent<AddFactContextActions>(actions =>
                    //{
                    //    actions.Activated.Add(GameActions.ContextActionApplyBuff(ab =>
                    //    {
                    //        ab.m_Buff = buff.ToReference();
                    //        ab.Permanent = true;
                    //    }));
                    //    actions.Deactivated.Add(GameActions.ContextActionRemoveBuff(rb => rb.m_Buff = buff.ToReference()));
                    //});

                    feature.AddPrerequisiteFeature(EldritchBlast.FeatureRef);

                    return feature;
                });

            return feature;
        }


        [LocalizedString]
        internal const string ToggleSettingDisplayName = "Mythic Eldritch Blast";

        static Settings.Setting<bool> enabled = null!;

        [Init]
        internal static void InitSettings()
        {
            var settings = new Settings.SettingsGroup("Mythic");

            (settings, enabled) = settings.AddToggle("MythicEldritchBlastToggle",
                LocalizedStrings.Homebrew_MythicBlast_ToggleSettingDisplayName);

            Settings.Instance.AddGroup(settings);
        }

        [Init]
        internal static void Init()
        {
            var context = new BlueprintInitializationContext(Triggers.BlueprintsCache_Init);

            Create(context)
                .GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeatureSelection.MythicFeatSelection)
                .Map(bps =>
                {
                    (BlueprintFeature feature, BlueprintFeatureSelection mythicFeats) = bps;

                    void addAbility(bool value)
                    {
                        MicroLogger.Debug(() => $"Enable Mythic Blast? {value}");

                        if (value)
                        {
                            mythicFeats.AddFeatures(feature);
                        }
                        else if (mythicFeats.AllFeatures.Contains(feature))
                        {
                            mythicFeats.m_Features = mythicFeats.m_Features.Where(f => f.deserializedGuid != feature.AssetGuid).ToArray();
                            mythicFeats.m_AllFeatures = mythicFeats.m_AllFeatures.Where(f => f.deserializedGuid != feature.AssetGuid).ToArray();
                        }
                    }

                    enabled.Changed.Subscribe(addAbility);

                    addAbility(enabled.Value);

                })
                .Register();
        }
    }
}
