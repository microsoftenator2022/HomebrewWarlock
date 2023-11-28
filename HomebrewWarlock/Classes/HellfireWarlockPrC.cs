using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features;

using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

namespace HomebrewWarlock
{
    internal static class HellfireWarlockInvokerFeatures
    {
        [LocalizedString]
        internal const string DisplayName = "Warlock Progression";

        [LocalizedString]
        internal const string Description =
            "At each level, you gain new invocations known, increased damage with eldritch blast, and an increase " +
            "in invoker level as if you had also gained a level in the warlock class. You do not, however, gain any " +
            "other benefit a character of that class would have gained.";

        public static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(
            BlueprintInitializationContext context)
        {
            var feature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("HellfireWarlockInvokerProgressions"))
                .Map(feature =>
                {
                    feature.m_DisplayName = LocalizedStrings.HellfireWarlockInvokerFeatures_DisplayName;
                    feature.m_Description = LocalizedStrings.HellfireWarlockInvokerFeatures_Description;

                    feature.AddAddFeatureIfHasFact(c =>
                    {
                        c.m_CheckedFact = GeneratedGuid.EldritchBlastProgression.ToBlueprintReference<BlueprintUnitFactReference>();
                        c.m_Feature = GeneratedGuid.EldritchBlastProgression.ToBlueprintReference<BlueprintUnitFactReference>();
                    });

                    feature.AddAddFeatureIfHasFact(c =>
                    {
                        c.m_CheckedFact = GeneratedGuid.WarlockInvocationsProgression.ToBlueprintReference<BlueprintUnitFactReference>();
                        c.m_Feature = GeneratedGuid.WarlockInvocationsProgression.ToBlueprintReference<BlueprintUnitFactReference>();
                    });

                    return feature;
                });

            return feature;
        }
    }

    internal static class HellfireWarlockClass
    {
        [LocalizedString]
        internal const string DisplayName = "Hellfire Warlock";

        [LocalizedString]
        internal const string ShortDescription =
            "The hellfire warlocks are a secretive group of specialist warlocks who have mastered hellfire, a " +
            "dangerous energy found only in the Nine Hells. By tapping into this infernal power, these characters " +
            "learn to infuse their eldritch blasts and magic items that they wield with the dark power of hellfire.";

        [LocalizedString]
        internal const string Description =
            "The hellfire warlock class offers great power at the expense of versatility. This prestige class " +
            "presents a focused approach to tap the power of the Nine Hells, though often at a grave price. As they " +
            "advance, these warlocks can access greater uses of hellfire, including the ability to infuse magic " +
            "items with the power of hellfire and lash out with the fires of Hell against any foe that strikes them.";

        public static BlueprintInitializationContext.ContextInitializer<BlueprintCharacterClass> Create(
            BlueprintInitializationContext context)
        {
            var warlockProgressionsFeature = HellfireWarlockInvokerFeatures.Create(context);

            var savesLow = context.NewBlueprint<BlueprintStatProgression>(GeneratedGuid.Get("HellfireWarlockSavesLow"))
                .Map(bp =>
                {
                    bp.Bonuses = [0, 0, 0, 1];

                    return bp;
                });

            var savesHigh = context.NewBlueprint<BlueprintStatProgression>(GeneratedGuid.Get("HellfireWarlockSavesHigh"))
                .Map(bp =>
                {
                    bp.Bonuses = [0, 2, 2, 3];

                    return bp;
                });

            var bab = context.NewBlueprint<BlueprintStatProgression>(GeneratedGuid.Get("HellfireWarlockBAB"))
                .Map(bp =>
                {
                    bp.Bonuses = [0, 0, 1, 2];

                    return bp;
                });

            var hellfireBlast = HellfireBlast.Create(context);

            var progression = context.NewBlueprint<BlueprintProgression>(GeneratedGuid.Get("HellfireWarlockProgression"))
                .Combine(warlockProgressionsFeature)
                .Combine(hellfireBlast)
                .Map(bps =>
                {
                    var (progression, warlockProgressions, hellfireBlast) = bps.Expand();

                    progression.m_Classes =
                    [
                        new() { m_Class = GeneratedGuid.HellfireWarlockClass.ToBlueprintReference<BlueprintCharacterClassReference>() }
                    ];

                    var hellfireBlastRankRef = GeneratedGuid.HellfireBlastRankFeature.ToBlueprintReference<BlueprintFeatureBaseReference>();

                    progression.LevelEntries =
                    [
                        new()
                        {
                            Level = 1,
                            m_Features =
                            [
                                warlockProgressions.ToReference<BlueprintFeatureBaseReference>(),
                                hellfireBlast.ToReference<BlueprintFeatureBaseReference>(),
                                hellfireBlastRankRef
                            ]
                        },
                        new() { Level = 2, m_Features = [hellfireBlastRankRef] },
                        new() { Level = 3, m_Features = [hellfireBlastRankRef] }
                    ];

                    progression.UIGroups =
                    [
                        new UIGroup()
                        {
                            m_Features =
                            [
                                //hellfireBlast.ToReference<BlueprintFeatureBaseReference>(),
                                hellfireBlastRankRef
                            ]
                        }
                    ];

                    return progression;
                });

            var @class = context.NewBlueprint<BlueprintCharacterClass>(GeneratedGuid.Get("HellfireWarlockClass"))
                .Combine(savesLow)
                .Combine(savesHigh)
                .Combine(bab)
                .Combine(progression)
                .Map(bps =>
                {
                    var (@class, savesLow, savesHigh, bab, progression) = bps.Expand();

                    @class.LocalizedName = LocalizedStrings.HellfireWarlockClass_DisplayName;
                    @class.LocalizedDescription = LocalizedStrings.HellfireWarlockClass_Description;
                    @class.LocalizedDescriptionShort = LocalizedStrings.HellfireWarlockClass_ShortDescription;

                    @class.PrestigeClass = true;

                    @class.HitDie = DiceType.D6;

                    @class.SkillPoints = 2;

                    @class.ClassSkills =
                    [
                        StatType.SkillKnowledgeArcana,
                        StatType.SkillLoreReligion,
                        StatType.SkillPersuasion,
                        StatType.SkillUseMagicDevice
                    ];

                    @class.m_BaseAttackBonus = bab.ToReference();

                    @class.m_FortitudeSave = savesLow.ToReference();
                    @class.m_ReflexSave = savesLow.ToReference();
                    @class.m_WillSave = savesHigh.ToReference();

                    @class.IsArcaneCaster = true;

                    @class.m_Progression = progression.ToReference();

                    @class.AddComponent<PrerequisiteStatValue>(c =>
                    {
                        c.Stat = StatType.SkillPersuasion;
                        c.Value = 6;
                    });

                    @class.AddComponent<PrerequisiteStatValue>(c =>
                    {
                        c.Stat = StatType.SkillKnowledgeArcana;
                        c.Value = 12;
                    });

                    @class.AddComponent<PrerequisiteFeature>(c =>
                    {
                        c.m_Feature = GeneratedGuid.BrimstoneBlastFeature.ToBlueprintReference<BlueprintFeatureReference>();
                        c.Group = Prerequisite.GroupType.Any;
                    });

                    @class.AddComponent<PrerequisiteFeature>(c =>
                    {
                        c.m_Feature = GeneratedGuid.HellrimeBlastFeature.ToBlueprintReference<BlueprintFeatureReference>();
                        c.Group = Prerequisite.GroupType.Any;
                    });

                    @class.AddComponent<PrerequisiteClassLevel>(c =>
                    {
                        c.m_CharacterClass = @class.ToReference();
                        c.Level = 3;
                        c.Not = true;

                        c.HideInUI = true;
                    });

                    @class.AddComponent<PrerequisiteIsPet>(c =>
                    {
                        c.Not = true;
                        c.HideInUI = true;
                    });

                    return @class;
                });

            return @class;
        }

        [Init]
        static void Init()
        {
            var context = new BlueprintInitializationContext(Triggers.BlueprintsCache_Init);

            Create(context)
                .Map(@class =>
                {
#if DEBUG
                    Game.Instance.BlueprintRoot.Progression.m_CharacterClasses =
                        [.. Game.Instance.BlueprintRoot.Progression.m_CharacterClasses, @class.ToReference()];
#endif
                })
                .Register();
        }
    }
}
