using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features;

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
            BlueprintFeature? EldritchBlast = null,
            BlueprintFeature? RayCalculateFeature = null,
            BlueprintFeature? DetectMagic = null,
            BlueprintFeature? InvocationSelection = null,
            BlueprintFeature? DamageReduction = null,
            (BlueprintFeature baseFeature, BlueprintFeatureSelection selection)? EnergyResistance = null,
            BlueprintFeature? FiendishResilience = null,
            BlueprintFeature? DeceiveItem = null);

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintProgression> Create(BlueprintInitializationContext context)
        {
            var features = WarlockProficiencies.Create(context)
                .Map(p => new ClassFeatures() with { Proficiencies = p })
                .Combine(EldritchBlast.CreateFeature(context))
                .Map(fs => fs.Left with { EldritchBlast = fs.Right })
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeature.RayCalculateFeature))
                .Map(fs => fs.Left with { RayCalculateFeature = fs.Right })
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeature.DetectMagic))
                .Map(fs => fs.Left with { DetectMagic = fs.Right })
                .Combine(InvocationSelection.CreateSelection(context))
                .Map(fs => fs.Left with { InvocationSelection = fs.Right })
                .Combine(EnergyResistance.Create(context))
                .Map(fs => fs.Left with { EnergyResistance = fs.Right })
                .Combine(WarlockDamageReduction.Create(context))
                .Map(fs => fs.Left with { DamageReduction = fs.Right })
                .Combine(FiendishResilience.Create(context))
                .Map(fs => fs.Left with { FiendishResilience = fs.Right })
                .Combine(DeceiveItem.Create(context))
                .Map(fs => fs.Left with { DeceiveItem = fs.Right });

            var progression = context.NewBlueprint<BlueprintProgression>(GeneratedGuid.Get(nameof(WarlockProgression)), nameof(WarlockProgression))
                .Combine(features)
                .Map(progressionAndFeatures =>
                {
                    var (progression, features) = progressionAndFeatures;

                    var proficiencies = features.Proficiencies!;
                    var eldritchBlast = features.EldritchBlast!;
                    var rayCalculateFeature = features.RayCalculateFeature!;
                    var detectMagic = features.DetectMagic!;
                    var invocationSelection = features.InvocationSelection!;
                    var energyResist  = features.EnergyResistance!.Value;
                    var damageReduction = features.DamageReduction!;
                    var fiendishResilience = features.FiendishResilience!;
                    var deceiveItem = features.DeceiveItem!;

                    progression.AddFeatures(1,
                        proficiencies,
                        eldritchBlast,
                        rayCalculateFeature,
                        invocationSelection);

                    progression.AddFeatures(2, detectMagic, invocationSelection);
                    progression.AddFeatures(3, eldritchBlast, damageReduction);
                    progression.AddFeatures(4, invocationSelection, deceiveItem);
                    progression.AddFeatures(5, eldritchBlast);
                    progression.AddFeatures(6, invocationSelection);
                    progression.AddFeatures(7, eldritchBlast, damageReduction);
                    progression.AddFeatures(8, invocationSelection, fiendishResilience);
                    progression.AddFeatures(9, eldritchBlast);
                    progression.AddFeatures(10, invocationSelection, energyResist.baseFeature, energyResist.selection, energyResist.selection);
                    progression.AddFeatures(11, eldritchBlast, invocationSelection, damageReduction);
                    progression.AddFeatures(13, invocationSelection, fiendishResilience);
                    progression.AddFeatures(14, eldritchBlast);
                    progression.AddFeatures(15, invocationSelection, damageReduction);
                    progression.AddFeatures(16, invocationSelection);
                    progression.AddFeatures(17, eldritchBlast);
                    progression.AddFeatures(18, invocationSelection, fiendishResilience);
                    progression.AddFeatures(19, damageReduction);
                    progression.AddFeatures(20, eldritchBlast, invocationSelection, energyResist.baseFeature);

                    return progression;
                });

            return progression;
        }
    }
}
