using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;

using MicroWrath.BlueprintInitializationContext;

namespace HomebrewWarlock.Features
{
    internal static class EldritchBlastProgression
    {
        [LocalizedString]
        internal const string DisplayName = "Eldritch Blast";

        public static BlueprintInitializationContext.ContextInitializer<BlueprintProgression> Create(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<EldritchBlastFeatures> ebFeatures)
        {
            var progression = context.NewBlueprint<BlueprintProgression>(GeneratedGuid.Get("EldritchBlastProgression"))
                .Combine(ebFeatures)
                .Map(bps =>
                {
                    var (progression, ebFeatures) = bps;

                    progression.m_DisplayName = LocalizedStrings.Features_EldritchBlastProgression_DisplayName;
                    
                    var ebRankRef = ebFeatures.EldritchBlastRank.ToReference<BlueprintFeatureBaseReference>();

                    progression.m_Classes =
                    [
                        new() { m_Class = GeneratedGuid.WarlockClass.ToBlueprintReference<BlueprintCharacterClassReference>() },
                        //new() { m_Class = GeneratedGuid.HellfireWarlockClass.ToBlueprintReference<BlueprintCharacterClassReference>() },
                    ];

                    progression.LevelEntries =
                    [
                        new() { Level = 1, m_Features = [ebRankRef] },
                        new() { Level = 3, m_Features = [ebRankRef] },
                        new() { Level = 5, m_Features = [ebRankRef] },
                        new() { Level = 7, m_Features = [ebRankRef] },
                        new() { Level = 9, m_Features = [ebRankRef] },
                        new() { Level = 11, m_Features = [ebRankRef] },
                        new() { Level = 14, m_Features = [ebRankRef] },
                        new() { Level = 17, m_Features = [ebRankRef] },
                        new() { Level = 20, m_Features = [ebRankRef] },
                    ];

                    //progression.GiveFeaturesForPreviousLevels = true;

                    progression.HideInCharacterSheetAndLevelUp = true;

                    return progression;
                });

            return progression;
        }
    }
}
