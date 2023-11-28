using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Utility;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Util.Linq;

namespace HomebrewWarlock.Archetypes
{
    internal static class WitchEldritchPatron
    {
        [LocalizedString]
        public const string DisplayName = "Eldritch Patron";

        [LocalizedString]
        public const string Description =
            "TODO: Proper description\n" +
            "Based on Stigmatized Witch\n" +
            "Oracle Curse -> Can take a Warlock Invocation whenever would gain Witch Hex. " +
            "Lesser = Major Hexes, Greater = Level 14, Dark = Grand Hexes\n" +
            "Familiar -> ?";

        [LocalizedString]
        public const string HexSelectionDisplayName = "Eldritch Patron Hex Selection";

        public static BlueprintInitializationContext.ContextInitializer<BlueprintArchetype> Create(BlueprintInitializationContext context)
        {
            //var ebFirstRank = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("EldritchBlastFirstRankFeature"))
            //    .Map(feature =>
            //    {
            //        feature.AddAddFacts(c => c.m_Facts = [GeneratedGuid.EldritchBlastRank.ToBlueprintReference<BlueprintUnitFactReference>()]);

            //        feature.HideInUI = true;

            //        return feature;
            //    });

            var addFirstRank = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("AddEldritchBlastFirstRankFeature"))
                //.Combine(ebFirstRank)
                .Map(feature =>
                {
                    //var (feature, firstRank) = bps;

                    feature.AddAddFeatureIfHasFact(c =>
                    {
                        c.m_CheckedFact = GeneratedGuid.EldritchBlastPrerequisiteFeature.ToBlueprintReference<BlueprintUnitFactReference>();
                        c.m_Feature = GeneratedGuid.EldritchBlastRank.ToBlueprintReference<BlueprintUnitFactReference>();
                    });

                    feature.HideInUI = true;
                    feature.ReapplyOnLevelUp = true;

                    return feature;
                });

            var hexInvocationSelection = context.NewBlueprint<BlueprintFeatureSelection>(GeneratedGuid.Get("EldritchPatronHexSelection"))
                .GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeatureSelection.WitchHexSelection)
                .Map(bps =>
                {
                    var (selection, witchHex) = bps;

                    selection.m_DisplayName = LocalizedStrings.Archetypes_WitchEldritchPatron_HexSelectionDisplayName;

                    selection.AddFeatures(
                        witchHex.ToMicroBlueprint<BlueprintFeatureSelection>(),
                        GeneratedGuid.WarlockInvocationSelection.ToMicroBlueprint<BlueprintFeatureSelection>());

                    return selection;
                });

            var witchFeatures = context
                .GetBlueprint(BlueprintsDb.Owlcat.BlueprintProgression.WitchProgression)
                .GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeatureSelection.WitchHexSelection)
                .GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeatureSelection.WitchFamiliarSelection)
                .GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeatureSelection.WitchPatronSelection)
                .GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeature.WitchCantripsFeature)
                .GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeature.AccursedWitchCantripsFeature)
                .Map(bps => bps.Expand());

            var archetype = context.CloneBlueprint(
                BlueprintsDb.Owlcat.BlueprintArchetype.AccursedWitchArchetype,
                GeneratedGuid.Get(nameof(WitchEldritchPatron)))
                .Combine(addFirstRank)
                .Combine(hexInvocationSelection)
                .Combine(witchFeatures)
                .Map(bps =>
                {
                    var
                    (
                        archetype,
                        ebr,
                        hexSelection,
                        (
                            witchProgression,
                            witchHexSelection,
                            witchFamiliar,
                            patronSelection,
                            witchCantrips,
                            accursedWitchCantrips
                        )
                    ) = bps.Expand();

                    archetype.LocalizedName = LocalizedStrings.Archetypes_WitchEldritchPatron_DisplayName;
                    archetype.LocalizedDescription = LocalizedStrings.Archetypes_WitchEldritchPatron_Description;
                    archetype.LocalizedDescriptionShort = Default.LocalizedString;

                    archetype.AddSkillPoints = 0;

                    var removeFeatures = new Dictionary<int, List<BlueprintFeatureBaseReference>>
                    {
                        [1] = 
                            [
                                patronSelection.ToReference<BlueprintFeatureBaseReference>(),
                                witchCantrips.ToReference<BlueprintFeatureBaseReference>()
                            ]
                    };

                    var addFeatures = new Dictionary<int, List<BlueprintFeatureBaseReference>>
                    {
                        [1] =
                            [
                                ebr.ToReference<BlueprintFeatureBaseReference>(),
                                accursedWitchCantrips.ToReference<BlueprintFeatureBaseReference>()
                            ],
                        [10] = [GeneratedGuid.LesserInvocationsPrerequisiteFeature.ToBlueprintReference<BlueprintFeatureBaseReference>()],
                        [14] = [GeneratedGuid.GreaterInvocationsPrerequisiteFeature.ToBlueprintReference<BlueprintFeatureBaseReference>()],
                        [18] = [GeneratedGuid.DarkInvocationsPrerequisiteFeature.ToBlueprintReference<BlueprintFeatureBaseReference>()]
                    };

                    foreach (var le in witchProgression.LevelEntries)
                    {
                        if (le.Features.Any(f => f == witchHexSelection))
                        {
                            if (!removeFeatures.ContainsKey(le.Level))
                                removeFeatures[le.Level] = [];

                            removeFeatures[le.Level].Add(witchHexSelection.ToReference<BlueprintFeatureBaseReference>());

                            if (!addFeatures.ContainsKey(le.Level))
                                addFeatures[le.Level] = [];

                            addFeatures[le.Level].Add(hexSelection.ToReference<BlueprintFeatureBaseReference>());
                        }
                    }

                    archetype.AddFeatures = addFeatures
                        .Select(kv => new LevelEntry() { Level = kv.Key, m_Features = kv.Value })
                        .ToArray();

                    archetype.RemoveFeatures = removeFeatures
                        .Select(kv => new LevelEntry() { Level = kv.Key, m_Features = kv.Value })
                        .ToArray();

                    return archetype;
                });

            return archetype;
        }

        [Init]
        static void Init()
        {
            var context = new BlueprintInitializationContext(Triggers.BlueprintsCache_Init);
            
            Create(context)
                .GetBlueprint(BlueprintsDb.Owlcat.BlueprintCharacterClass.WitchClass)
                .Map(bps =>
                {
                    var (archetype, witch) = bps;
#if DEBUG
                    witch.m_Archetypes = witch.m_Archetypes.Append(archetype.ToReference());
#endif
                })
                .Register();
        }
    }
}
