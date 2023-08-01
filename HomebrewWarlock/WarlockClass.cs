using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;

using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Alignments;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Util.Linq;

namespace HomebrewWarlock
{
    internal static class WarlockClass
    {
        [LocalizedString]
        internal static readonly string DisplayName = "Warlock";

        [LocalizedString]
        internal static readonly string Description =
            "Born of a supernatural bloodline, a warlock seeks to master the perilous magic that suffuses " +
            "his soul. Unlike sorcerers or wizards, who approach arcane magic through the medium of " +
            "spells, a warlock invokes powerful magic through nothing more than an effort of will. By " +
            "harnessing his innate magical gift through fearsome determination and force of will, a " +
            "warlock can perform feats of supernatural stealth, beguile the weak-minded, or scour his foes " +
            "with blasts of eldritch power.";

        internal static readonly IMicroBlueprint<BlueprintCharacterClass> Blueprint =
            new MicroBlueprint<BlueprintCharacterClass>(GeneratedGuid.WarlockClass);

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintCharacterClass> Create(BlueprintInitializationContext context) =>
            context.NewBlueprint<BlueprintCharacterClass>(GeneratedGuid.Get(nameof(WarlockClass)), nameof(WarlockClass))
            .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintStatProgression.BABMedium))
            .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintStatProgression.SavesLow))
            .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintStatProgression.SavesHigh))
            .Map(bps =>
            {
                var (@class, babMedium, savesLow, savesHigh) = bps.Expand();

                @class.LocalizedName = LocalizedStrings.WarlockClass_DisplayName;
                @class.LocalizedDescription = LocalizedStrings.WarlockClass_Description;
                @class.LocalizedDescriptionShort = @class.LocalizedDescription;
                
                @class.AddComponent<PrerequisiteIsPet>(c => 
                {
                    c.Not = true;
                    c.HideInUI = true;
                });

                @class.SkillPoints = 2;
                @class.HitDie = DiceType.D6;
                @class.m_BaseAttackBonus = babMedium.ToReference<BlueprintStatProgressionReference>();
                @class.m_FortitudeSave = savesLow.ToReference<BlueprintStatProgressionReference>();
                @class.m_ReflexSave = savesLow.ToReference<BlueprintStatProgressionReference>();
                @class.m_WillSave = savesHigh.ToReference<BlueprintStatProgressionReference>();
                
                @class.ClassSkills = new[]
                {
                    StatType.SkillPersuasion,
                    StatType.SkillAthletics,
                    StatType.SkillKnowledgeArcana,
                    StatType.SkillLoreReligion,
                    StatType.SkillPerception,
                    StatType.SkillUseMagicDevice
                };

                @class.IsArcaneCaster = true;

                @class.m_Difficulty = 3;

                @class.StartingGold = 411;

                @class.MaleEquipmentEntities = new EquipmentEntityLink[]
                {
                    new() { AssetId = "1d070a314c6b6cc4c8cb25535962542e" },
                    new() { AssetId = "db25be3becf55bb499b6ad5ddaad6640" }
                };

                @class.FemaleEquipmentEntities = new EquipmentEntityLink[]
                {
                    new() { AssetId = "2d67e529246cc754390a5c92d5ee50dd" },
                    new() { AssetId = "b5db7f26cdb3fb949b16a2d88de0e920" }
                };

                @class.PrimaryColor = 12;
                @class.SecondaryColor = 57;

                @class.AddComponent<PrerequisiteAlignment>(c => c.Alignment = AlignmentMaskType.Chaotic | AlignmentMaskType.Evil);

                @class.RecommendedAttributes = new[] { StatType.Charisma };

                return @class;
            });

        [Init]
        internal static void Init()
        {
            var context = new BlueprintInitializationContext(Triggers.BlueprintsCache_Init);

            var @class = Create(context);

            var progression = WarlockProgression.Create(context);

            @class = @class
                .Combine(progression)
                .Map(cap =>
                {
                    var (@class, progression) = cap;

                    progression.m_Classes = new BlueprintProgression.ClassWithLevel[]
                    {
                        new()
                        {
                            m_Class = @class.ToReference<BlueprintCharacterClassReference>(),
                            AdditionalLevel = 0
                        }
                    };

                    @class.m_Progression = progression.ToReference<BlueprintProgressionReference>();

                    @class.m_SignatureAbilities = new[]
                    {
                        EldritchBlast.FeatureRef.ToReference<BlueprintFeature, BlueprintFeatureReference>(),
                        WarlockProgression.BasicInvocations.ToReference<BlueprintFeature, BlueprintFeatureReference>()
                    };

                    Game.Instance.BlueprintRoot.Progression.m_CharacterClasses =
                        Game.Instance.BlueprintRoot.Progression.m_CharacterClasses
                            .Append(@class.ToReference<BlueprintCharacterClassReference>());

                    return @class;
                });

            @class.Register();
        }
    }
}
