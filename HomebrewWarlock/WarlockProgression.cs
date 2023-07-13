using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features;
using HomebrewWarlock.Features.EldritchBlast;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Util;

namespace HomebrewWarlock
{
    internal static class WarlockProgression
    {
        record class ClassFeatures(
            BlueprintFeature? Proficiencies = null,
            EldritchBlastFeatures? EldritchBlastFeatures = null,
            BlueprintFeature? RayCalculateFeature = null,
            BlueprintFeature? DetectMagic = null,
            BlueprintFeature? InvocationSelection = null,
            BlueprintFeature? DamageReduction = null,
            (BlueprintFeature baseFeature, BlueprintFeatureSelection selection)? EnergyResistance = null,
            BlueprintFeature? FiendishResilience = null,
            BlueprintFeature? DeceiveItem = null,
            BlueprintFeature? LesserInvocationsPrerequisite = null,
            BlueprintFeature? GreaterInvocationsPrerequisite = null,
            BlueprintFeature? DarkInvocationsPrerequisite = null);

        internal static readonly IMicroBlueprint<BlueprintFeature> BasicInvocations = new MicroBlueprint<BlueprintFeature>(GeneratedGuid.InvocationsBasicFeature);

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintProgression> Create(BlueprintInitializationContext context)
        {
            var blastFeatures = EldritchBlastFeatures.CreateFeatures(context);
            
            var features = WarlockProficiencies.Create(context)
                .Map(p => new ClassFeatures() with { Proficiencies = p })
                .Combine(blastFeatures)
                .Map(fs => fs.Left with { EldritchBlastFeatures = fs.Right })
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeature.RayCalculateFeature))
                .Map(fs => fs.Left with { RayCalculateFeature = fs.Right })
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeature.DetectMagic))
                .Map(fs => fs.Left with { DetectMagic = fs.Right })
                //.Combine(InvocationSelection.CreateSelection(context, blastFeatures))
                //.Map(fs => fs.Left with { InvocationSelection = fs.Right })
                .Combine(EnergyResistance.Create(context))
                .Map(fs => fs.Left with { EnergyResistance = fs.Right })
                .Combine(WarlockDamageReduction.Create(context))
                .Map(fs => fs.Left with { DamageReduction = fs.Right })
                .Combine(FiendishResilience.Create(context))
                .Map(fs => fs.Left with { FiendishResilience = fs.Right })
                .Combine(DeceiveItem.Create(context))
                .Map(fs => fs.Left with { DeceiveItem = fs.Right })
                .Combine(InvocationSelection.CreateSelection(context, blastFeatures))
                .Map(fs =>
                {
                    var (features, (invocationSelection, lesserPrerequisite, greaterPrerequisite, darkPrerequisite)) = fs;
                    features = features with
                    {
                        InvocationSelection = invocationSelection,
                        LesserInvocationsPrerequisite = lesserPrerequisite,
                        GreaterInvocationsPrerequisite = greaterPrerequisite,
                        DarkInvocationsPrerequisite = darkPrerequisite
                    };

                    return features;
                });

            var progression = context.NewBlueprint<BlueprintProgression>(GeneratedGuid.Get(nameof(WarlockProgression)), nameof(WarlockProgression))
                .Combine(features)
                .Combine(context.NewBlueprint<BlueprintFeature>(
                    GeneratedGuid.Get("InvocationsBasicFeature"), nameof(GeneratedGuid.InvocationsBasicFeature)))
                .Map(progressionAndFeatures =>
                {
                    var (progression, features, invocationBase) = progressionAndFeatures.Expand();

                    var eldritchBlastFeatures = features.EldritchBlastFeatures!;
                    var eldritchBlastBase = eldritchBlastFeatures.EldritchBlastBase;
                    var eldritchBlastRank = eldritchBlastFeatures.EldritchBlastRank;

                    var proficiencies = features.Proficiencies!;
                    var rayCalculateFeature = features.RayCalculateFeature!;
                    var detectMagic = features.DetectMagic!;
                    
                    var energyResist  = features.EnergyResistance!.Value;
                    var damageReduction = features.DamageReduction!;
                    var fiendishResilience = features.FiendishResilience!;
                    var deceiveItem = features.DeceiveItem!;
                    
                    var invocationSelection = features.InvocationSelection!;

                    invocationBase.m_DisplayName = invocationSelection.m_DisplayName;
                    invocationBase.m_Description = invocationSelection.m_Description;
                    invocationBase.m_DescriptionShort = invocationSelection.m_DescriptionShort;
                    
                    invocationBase.m_Icon = Sprites.TouchOfChaos;

                    var lesserInvocations = features.LesserInvocationsPrerequisite!;
                    var greaterInvocations = features.GreaterInvocationsPrerequisite!;
                    var darkInvocations = features.DarkInvocationsPrerequisite!;

                    progression.AddFeatures(1,
                        proficiencies,
                        eldritchBlastBase,
                        eldritchBlastRank,
                        rayCalculateFeature,
                        invocationSelection,
                        invocationBase);

                    progression.AddFeatures(2, detectMagic, invocationSelection);
                    progression.AddFeatures(3, eldritchBlastRank, damageReduction);
                    progression.AddFeatures(4, invocationSelection, deceiveItem);
                    progression.AddFeatures(5, eldritchBlastRank);
                    progression.AddFeatures(6, invocationSelection, lesserInvocations);
                    progression.AddFeatures(7, eldritchBlastRank, damageReduction);
                    progression.AddFeatures(8, invocationSelection, fiendishResilience);
                    progression.AddFeatures(9, eldritchBlastRank);
                    progression.AddFeatures(10, invocationSelection, energyResist.baseFeature, energyResist.selection, energyResist.selection);
                    progression.AddFeatures(11, eldritchBlastRank, invocationSelection, damageReduction);
                    // progression.AddFeatures(12, imbueItem);
                    progression.AddFeatures(13, invocationSelection, fiendishResilience, greaterInvocations);
                    progression.AddFeatures(14, eldritchBlastRank);
                    progression.AddFeatures(15, invocationSelection, damageReduction);
                    progression.AddFeatures(16, invocationSelection, darkInvocations);
                    progression.AddFeatures(17, eldritchBlastRank);
                    progression.AddFeatures(18, invocationSelection, fiendishResilience);
                    progression.AddFeatures(19, damageReduction);
                    progression.AddFeatures(20, eldritchBlastRank, invocationSelection, energyResist.baseFeature);

                    progression.UIGroups = new UIGroup[]
                    {
                        new()
                        {
                            m_Features = new()
                            {
                                eldritchBlastBase.ToReference<BlueprintFeatureBaseReference>(),
                                eldritchBlastRank.ToReference<BlueprintFeatureBaseReference>()
                            }
                        },
                        new()
                        {
                            m_Features = new()
                            {
                                invocationBase.ToReference<BlueprintFeatureBaseReference>(),
                                lesserInvocations.ToReference<BlueprintFeatureBaseReference>(),
                                greaterInvocations.ToReference<BlueprintFeatureBaseReference>(),
                                darkInvocations.ToReference<BlueprintFeatureBaseReference>()
                            }

                        }
                    };

                    return progression;
                });

            return progression;
        }
    }
}
