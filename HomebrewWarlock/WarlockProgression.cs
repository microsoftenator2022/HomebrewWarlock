using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;

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
        internal static BlueprintInitializationContext.ContextInitializer<BlueprintProgression> Create(BlueprintInitializationContext context)
        {
            var progression = context.NewBlueprint<BlueprintProgression>(GeneratedGuid.Get(nameof(WarlockProgression)), nameof(WarlockProgression))
                .Combine(WarlockProficiencies.Create(context))
                .Combine(EldritchBlast.CreateFeature(context))
                .Map(progressionAndFeatures =>
                {
                    var (progression, proficiencies, eldritchBlast) = progressionAndFeatures.Flatten();

                    progression.AddFeatures(1, proficiencies, eldritchBlast);
                    progression.AddFeatures(2, BlueprintsDb.Owlcat.BlueprintFeature.DetectMagic.ToReference<BlueprintFeature, BlueprintFeatureReference>());
                    progression.AddFeatures(3, eldritchBlast);
                    //progression.AddFeatures(4);
                    progression.AddFeatures(5, eldritchBlast);
                    //progression.AddFeatures(6);
                    progression.AddFeatures(7, eldritchBlast);
                    //progression.AddFeatures(8);
                    progression.AddFeatures(9, eldritchBlast);
                    //progression.AddFeatures(10);
                    progression.AddFeatures(11, eldritchBlast);
                    //progression.AddFeatures(13);
                    progression.AddFeatures(14, eldritchBlast);
                    //progression.AddFeatures(15);
                    //progression.AddFeatures(16);
                    progression.AddFeatures(17, eldritchBlast);
                    //progression.AddFeatures(18);
                    //progression.AddFeatures(19);
                    progression.AddFeatures(20, eldritchBlast);


                    return progression;
                });

            return progression;
        }
    }
}
