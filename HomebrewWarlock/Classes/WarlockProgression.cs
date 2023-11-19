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

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

namespace HomebrewWarlock
{
    internal static class WarlockProgression
    {
        record class ClassFeatures(
            BlueprintFeature? Proficiencies = null,
            EldritchBlastFeatures? EldritchBlastFeatures = null,
            BlueprintProgression? EldritchBlastProgression = null,
            BlueprintFeature? RayCalculateFeature = null,
            BlueprintFeature? DetectMagic = null,
            //BlueprintFeature? InvocationSelection = null,
            BlueprintFeature? InvocationsProgression = null,
            BlueprintFeature? DamageReduction = null,
            (BlueprintFeature baseFeature, BlueprintFeatureSelection selection)? EnergyResistance = null,
            BlueprintFeature? FiendishResilience = null,
            BlueprintFeature? DeceiveItem = null,
            //BlueprintFeature? LesserInvocationsPrerequisite = null,
            //BlueprintFeature? GreaterInvocationsPrerequisite = null,
            //BlueprintFeature? DarkInvocationsPrerequisite = null,
            BlueprintFeature? ImbueItem = null);

        internal static readonly IMicroBlueprint<BlueprintFeature> BasicInvocations = new MicroBlueprint<BlueprintFeature>(GeneratedGuid.InvocationsBasicFeature);

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintProgression> Create(BlueprintInitializationContext context)
        {
            var blastFeatures = EldritchBlastFeatures.CreateFeatures(context);

            var eldritchBlastProgression = EldritchBlastProgression.Create(context, blastFeatures);

            var invocations = InvocationSelection.CreateSelection(context, blastFeatures);

            var invocationsBase = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("InvocationsBasicFeature"))
                .Combine(invocations)
                .Map(bps =>
                {
                    var (invocationsBase, (invocationsSelection, _, _, _)) = bps;

                    invocationsBase.m_DisplayName = invocationsSelection.m_DisplayName;
                    invocationsBase.m_Description = invocationsSelection.m_Description;
                    invocationsBase.m_DescriptionShort = invocationsSelection.m_DescriptionShort;

                    invocationsBase.m_Icon = Sprites.TouchOfChaos;

                    return invocationsBase;
                });

            var invocationsProgression = InvocationsProgression.Create(context, invocations);

            var features = WarlockProficiencies.Create(context)
                .Map(p => new ClassFeatures() with { Proficiencies = p })
                .Combine(blastFeatures)
                .Map(fs => fs.Left with { EldritchBlastFeatures = fs.Right })
                .Combine(eldritchBlastProgression)
                .Map(fs => fs.Left with { EldritchBlastProgression = fs.Right })
                .Combine(invocationsProgression)
                .Map(fs => fs.Left with { InvocationsProgression = fs.Right })
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeature.RayCalculateFeature))
                .Map(fs => fs.Left with { RayCalculateFeature = fs.Right })
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeature.DetectMagic))
                .Map(fs => fs.Left with { DetectMagic = fs.Right })
                .Combine(EnergyResistance.Create(context))
                .Map(fs => fs.Left with { EnergyResistance = fs.Right })
                .Combine(WarlockDamageReduction.Create(context))
                .Map(fs => fs.Left with { DamageReduction = fs.Right })
                .Combine(FiendishResilience.Create(context))
                .Map(fs => fs.Left with { FiendishResilience = fs.Right })
                .Combine(DeceiveItem.Create(context))
                .Map(fs => fs.Left with { DeceiveItem = fs.Right })
                .Combine(ImbueItem.Create(context))
                .Map(fs => fs.Left with { ImbueItem = fs.Right });

                //.Combine(InvocationSelection.CreateSelection(context, blastFeatures))
                //.Map(fs =>
                //{
                //    var (features, (invocationSelection, lesserPrerequisite, greaterPrerequisite, darkPrerequisite)) = fs;
                //    features = features with
                //    {
                //        InvocationSelection = invocationSelection,
                //        LesserInvocationsPrerequisite = lesserPrerequisite,
                //        GreaterInvocationsPrerequisite = greaterPrerequisite,
                //        DarkInvocationsPrerequisite = darkPrerequisite
                //    };

            //    return features;
            //});

            var progression = context.NewBlueprint<BlueprintProgression>(GeneratedGuid.Get(nameof(WarlockProgression)), nameof(WarlockProgression))
                .Combine(features)
                .Combine(invocationsBase)
                .Map(progressionAndFeatures =>
                {
                    var (progression, features, invocationsBase) = progressionAndFeatures.Expand();

                    var eldritchBlastFeatures = features.EldritchBlastFeatures!;
                    var eldritchBlastBase = eldritchBlastFeatures.EldritchBlastBase;
                    //var eldritchBlastRank = eldritchBlastFeatures.EldritchBlastRank;

                    var proficiencies = features.Proficiencies!;
                    var rayCalculateFeature = features.RayCalculateFeature!;
                    var detectMagic = features.DetectMagic!;
                    
                    var energyResist  = features.EnergyResistance!.Value;
                    var damageReduction = features.DamageReduction!;
                    var fiendishResilience = features.FiendishResilience!;
                    var deceiveItem = features.DeceiveItem!;
                    var imbueItem = features.ImbueItem!;
                    
                    //var invocationSelection = features.InvocationSelection!;

                    //var lesserInvocations = features.LesserInvocationsPrerequisite!;
                    //var greaterInvocations = features.GreaterInvocationsPrerequisite!;
                    //var darkInvocations = features.DarkInvocationsPrerequisite!;

                    invocationsBase.HideInUI = true;
                    features.InvocationsProgression!.m_Icon = invocationsBase.m_Icon;
                    features.InvocationsProgression!.m_Description = invocationsBase.m_Description;

                    eldritchBlastBase.HideInUI = true;
                    features.EldritchBlastProgression!.m_Icon = eldritchBlastBase.m_Icon;
                    features.EldritchBlastProgression!.m_Description = eldritchBlastBase.m_Description;

                    progression.AddFeatures(1,
                        eldritchBlastBase,
                        //eldritchBlastRank,
                        features.EldritchBlastProgression!,
                        features.InvocationsProgression!,
                        invocationsBase,
                        proficiencies,
                        rayCalculateFeature
                        //invocations,
                        );

                    progression.AddFeatures(2, detectMagic);

                    progression.AddFeatures(3, damageReduction);

                    progression.AddFeatures(4, deceiveItem);

                    //progression.AddFeatures(5, eldritchBlastRank);

                    //progression.AddFeatures(6, invocations, lesserInvocations);

                    progression.AddFeatures(7, damageReduction);

                    progression.AddFeatures(8, fiendishResilience);

                    //progression.AddFeatures(9, eldritchBlastRank);

                    progression.AddFeatures(10, energyResist.baseFeature, energyResist.selection, energyResist.selection);

                    progression.AddFeatures(11, damageReduction);

                    progression.AddFeatures(12, imbueItem);

                    progression.AddFeatures(13, fiendishResilience);

                    //progression.AddFeatures(14, eldritchBlastRank);

                    progression.AddFeatures(15, damageReduction);

                    //progression.AddFeatures(16, invocations, darkInvocations);

                    //progression.AddFeatures(17, eldritchBlastRank);

                    progression.AddFeatures(18, fiendishResilience);

                    progression.AddFeatures(19, damageReduction);

                    progression.AddFeatures(20, energyResist.baseFeature);

                    //progression.UIGroups = new UIGroup[]
                    //{
                    //    new()
                    //    {
                    //        m_Features = new()
                    //        {
                    //            eldritchBlastBase.ToReference<BlueprintFeatureBaseReference>(),
                    //            eldritchBlastRank.ToReference<BlueprintFeatureBaseReference>()
                    //        }
                    //    },
                    //    new()
                    //    {
                    //        m_Features = new()
                    //        {
                    //            invocationBase.ToReference<BlueprintFeatureBaseReference>(),
                    //            lesserInvocations.ToReference<BlueprintFeatureBaseReference>(),
                    //            greaterInvocations.ToReference<BlueprintFeatureBaseReference>(),
                    //            darkInvocations.ToReference<BlueprintFeatureBaseReference>()
                    //        }

                    //    }
                    //};

                    return progression;
                });

            return progression;
        }
    }
}
