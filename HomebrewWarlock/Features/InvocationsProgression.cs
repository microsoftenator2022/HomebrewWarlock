using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;

using MicroWrath.BlueprintInitializationContext;

namespace HomebrewWarlock.Features
{
    internal static class InvocationsProgression
    {
        [LocalizedString]
        internal const string DisplayName = "Warlock Invocations";

        public static BlueprintInitializationContext.ContextInitializer<BlueprintProgression> Create(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<(BlueprintFeatureSelection, BlueprintFeature, BlueprintFeature, BlueprintFeature)> invocations)
        {
            var progression = context.NewBlueprint<BlueprintProgression>(GeneratedGuid.Get("WarlockInvocationsProgression"))
                .Combine(invocations)
                .Map(bps =>
                {
                    var (progression, (invocationSelection, lesser, greater, dark)) = bps;

                    progression.m_DisplayName = LocalizedStrings.Features_InvocationsProgression_DisplayName;

                    progression.m_Classes =
                    [
                        new() { m_Class = GeneratedGuid.WarlockClass.ToBlueprintReference<BlueprintCharacterClassReference>() },
                        //new() { m_Class = GeneratedGuid.HellfireWarlockClass.ToBlueprintReference<BlueprintCharacterClassReference>() },
                    ];

                    progression.LevelEntries =
                    [
                        new() { Level = 1, m_Features = [invocationSelection.ToReference<BlueprintFeatureBaseReference>()] },
                        new() { Level = 2, m_Features = [invocationSelection.ToReference<BlueprintFeatureBaseReference>()] },
                        new() { Level = 4, m_Features = [invocationSelection.ToReference<BlueprintFeatureBaseReference>()] },
                        new()
                        {
                            Level = 6,
                            m_Features =
                                [
                                    invocationSelection.ToReference<BlueprintFeatureBaseReference>(),
                                    lesser.ToReference<BlueprintFeatureBaseReference>()
                                ]
                        },
                        new() { Level = 8, m_Features = [invocationSelection.ToReference<BlueprintFeatureBaseReference>()] },
                        new() { Level = 10, m_Features = [invocationSelection.ToReference<BlueprintFeatureBaseReference>()] },
                        new() { Level = 11, m_Features = [invocationSelection.ToReference<BlueprintFeatureBaseReference>()] },
                        new()
                        {
                            Level = 13,
                            m_Features =
                                [
                                    invocationSelection.ToReference<BlueprintFeatureBaseReference>(),
                                    greater.ToReference<BlueprintFeatureBaseReference>()
                                ]
                        },
                        new() { Level = 15, m_Features = [invocationSelection.ToReference<BlueprintFeatureBaseReference>()] },
                        new()
                        {
                            Level = 16,
                            m_Features =
                                [
                                    invocationSelection.ToReference<BlueprintFeatureBaseReference>(),
                                    dark.ToReference<BlueprintFeatureBaseReference>()
                                ]
                        },
                        new() { Level = 18, m_Features = [invocationSelection.ToReference<BlueprintFeatureBaseReference>()] },
                        new() { Level = 20, m_Features = [invocationSelection.ToReference<BlueprintFeatureBaseReference>()] },
                    ];

                    progression.GiveFeaturesForPreviousLevels = true;

                    progression.HideInCharacterSheetAndLevelUp = true;

                    return progression;
                });

            return progression;
        }
    }
}