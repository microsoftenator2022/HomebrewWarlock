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
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Alignments;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;
using MicroWrath.Util;
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

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintCharacterClass> Create(BlueprintInitializationContext context) =>
            context.NewBlueprint<BlueprintCharacterClass>(GeneratedGuid.Get(nameof(WarlockClass)), nameof(WarlockClass))
            .Map((BlueprintCharacterClass @class) =>
            {
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
                @class.m_BaseAttackBonus = BlueprintsDb.Owlcat.BlueprintStatProgression.BABMedium.ToReference<BlueprintStatProgression, BlueprintStatProgressionReference>();
                @class.m_FortitudeSave = BlueprintsDb.Owlcat.BlueprintStatProgression.SavesLow.ToReference<BlueprintStatProgression, BlueprintStatProgressionReference>();
                @class.m_ReflexSave = BlueprintsDb.Owlcat.BlueprintStatProgression.SavesLow.ToReference<BlueprintStatProgression, BlueprintStatProgressionReference>();
                @class.m_WillSave = BlueprintsDb.Owlcat.BlueprintStatProgression.SavesHigh.ToReference<BlueprintStatProgression, BlueprintStatProgressionReference>();
                
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
                        EldritchBlast.Feature.ToReference<BlueprintFeature, BlueprintFeatureReference>(),
                        InvocationSelection.Selection.ToReference<BlueprintFeature, BlueprintFeatureReference>()
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
