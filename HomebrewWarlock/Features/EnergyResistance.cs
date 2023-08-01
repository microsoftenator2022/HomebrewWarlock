using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;

using MicroWrath.BlueprintInitializationContext;

namespace HomebrewWarlock.Features
{
    internal static class EnergyResistance
    {
        [LocalizedString]
        internal static readonly string DisplayName = "Energy Resistance";

        [LocalizedString]
        internal static readonly string DisplayNameAcid = "Energy Resistance (Acid)";

        [LocalizedString]
        internal static readonly string DisplayNameCold = "Energy Resistance (Cold)";

        [LocalizedString]
        internal static readonly string DisplayNameElectricity = "Energy Resistance (Electricity)";

        [LocalizedString]
        internal static readonly string DisplayNameFire = "Energy Resistance (Fire)";

        [LocalizedString]
        internal static readonly string DisplayNameSonic = "Energy Resistance (Sonic)";

        [LocalizedString]
        internal static readonly string Description =
            "At 10th level and higher, a warlock has resistance 5 against any two of the " +
            "following energy types: acid, cold, electricity, fire, and sonic. Once the types are chosen, this " +
            "energy resistance cannot be changed. At 20th level, a warlock gains resistance 10 against the two " +
            "selected types of energy.";

        internal static BlueprintInitializationContext.ContextInitializer<(BlueprintFeature, BlueprintFeatureSelection)>
            Create(BlueprintInitializationContext context)
        {
            var selection = context.NewBlueprint<BlueprintFeatureSelection>(
                GeneratedGuid.Get("WarlockEnergyResistanceSelection"),
                nameof(GeneratedGuid.WarlockEnergyResistanceSelection))
                .Map((BlueprintFeatureSelection selection) =>
                {
                    selection.m_DisplayName = LocalizedStrings.Features_EnergyResistance_DisplayName;
                    selection.m_Description = LocalizedStrings.Features_EnergyResistance_Description;

                    return selection;
                })
                .Combine(context.NewBlueprint<BlueprintFeature>(
                    GeneratedGuid.Get("WarlockEnergyResistance"),
                    nameof(GeneratedGuid.WarlockEnergyResistance))
                    .Map((BlueprintFeature bp) =>
                    {
                        bp.m_DisplayName = LocalizedStrings.Features_EnergyResistance_DisplayName;
                        bp.m_Icon = AssetUtils.Direct.GetSprite("fb6503d798896b54ba759aa405833c69", 21300000);

                        return bp;
                    }))
                .Combine(context.NewBlueprint<BlueprintFeature>(
                    GeneratedGuid.Get("WarlockEnergyResistanceAcid"),
                    nameof(GeneratedGuid.WarlockEnergyResistanceAcid))
                    .Map(f =>
                    {
                        f.m_DisplayName = LocalizedStrings.Features_EnergyResistance_DisplayNameAcid;
                        AssetUtils.Direct.GetSprite("9858c74cdfbee1d46a07b26c95d6ad99", 21300000);
                        return f;
                    }))
                .Combine(context.NewBlueprint<BlueprintFeature>(
                    GeneratedGuid.Get("WarlockEnergyResistanceCold"),
                    nameof(GeneratedGuid.WarlockEnergyResistanceCold))
                    .Map(f =>
                    {
                        f.m_DisplayName = LocalizedStrings.Features_EnergyResistance_DisplayNameCold;
                        AssetUtils.Direct.GetSprite("d82abb0dfe5ada145b9a6c560fbc1efb", 21300000);
                        return f;
                    }))
                .Combine(context.NewBlueprint<BlueprintFeature>(
                    GeneratedGuid.Get("WarlockEnergyResistanceElectricity"),
                    nameof(GeneratedGuid.WarlockEnergyResistanceElectricity))
                    .Map(f =>
                    {
                        f.m_DisplayName = LocalizedStrings.Features_EnergyResistance_DisplayNameElectricity;
                        AssetUtils.Direct.GetSprite("e4f46fe964f7e2d4b87d92bfdd5bec18", 21300000);
                        return f;
                    }))
                .Combine(context.NewBlueprint<BlueprintFeature>(
                    GeneratedGuid.Get("WarlockEnergyResistanceFire"),
                    nameof(GeneratedGuid.WarlockEnergyResistanceFire))
                    .Map(f =>
                    {
                        f.m_DisplayName = LocalizedStrings.Features_EnergyResistance_DisplayNameFire;
                        AssetUtils.Direct.GetSprite("8044829fbaea2654ea08d7fa0f9fd98a", 21300000);
                        return f;
                    }))
                .Combine(context.NewBlueprint<BlueprintFeature>(
                    GeneratedGuid.Get("WarlockEnergyResistanceSonic"),
                    nameof(GeneratedGuid.WarlockEnergyResistanceSonic))
                    .Map(f =>
                    {
                        f.m_DisplayName = LocalizedStrings.Features_EnergyResistance_DisplayNameSonic;
                        AssetUtils.Direct.GetSprite("c1c44cca1868e9a40bef853ef190ede5", 21300000);
                        return f;
                    }))
                .Map(sf =>
                {
                    var (selection, baseFeature, acid, cold, electricity, fire, sonic) = sf.Expand();

                    selection.HideInCharacterSheetAndLevelUp = true;
                    selection.HideInUI = true;

                    baseFeature.m_Description = LocalizedStrings.Features_EnergyResistance_Description;

                    baseFeature.Ranks = 2;

                    foreach (var (element, feature) in
                        new[]
                        {
                            (DamageEnergyType.Acid, acid),
                            (DamageEnergyType.Cold, cold),
                            (DamageEnergyType.Electricity, electricity),
                            (DamageEnergyType.Fire, fire),
                            (DamageEnergyType.Sonic, sonic)
                        })
                    {
                        feature.m_Description = LocalizedStrings.Features_EnergyResistance_Description;

                        feature.AddAddDamageResistanceEnergy(c =>
                        {
                            c.Type = element;
                            c.Value = new()
                            {
                                ValueType = ContextValueType.Rank
                            };
                        });
                        feature.AddContextRankConfig(c =>
                        {
                            c.m_BaseValueType = ContextRankBaseValueType.FeatureRank;
                            c.m_Feature = baseFeature.ToReference<BlueprintFeatureReference>();
                            c.m_Progression = ContextRankProgression.MultiplyByModifier;
                            c.m_StepLevel = 5;
                        });
                    }

                    selection.AddFeatures(
                        acid.ToMicroBlueprint(),
                        cold.ToMicroBlueprint(),
                        electricity.ToMicroBlueprint(),
                        fire.ToMicroBlueprint(),
                        sonic.ToMicroBlueprint());

                    return (baseFeature, selection);
                });

            return selection;
        }
    }
}
