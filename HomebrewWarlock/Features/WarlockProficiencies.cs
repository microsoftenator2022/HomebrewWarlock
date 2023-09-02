using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Armors;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

using static MicroWrath.Encyclopedia;

namespace HomebrewWarlock.Features
{
    internal static class WarlockProficiencies
    {
        [LocalizedString]
        internal static readonly string DisplayName = "Warlock Proficiencies";

        [LocalizedString]
        internal static readonly string Description =
            $"Warlocks are proficient with {new Link(Page.Weapon_Proficiency, "simple weapons")} and "
            + $"light armor, but not with shields.{Environment.NewLine}"
            + $"A warlock can use any of his invocations while wearing light armor without incurring the "
            + $"normal {new Link(Page.Spell_Fail_Chance, "arcane spell failure chance")}. Like other "
            + $"arcane spell casters, a warlock wearing medium or heavy armor or wielding a shield incurs a "
            + $"chance of arcane spell failure.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context) =>
            context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get(nameof(WarlockProficiencies)), nameof(WarlockProficiencies))
                .Combine(BlueprintsDb.Owlcat.BlueprintFeature.LightArmorProficiency)
                .Combine(BlueprintsDb.Owlcat.BlueprintFeature.SimpleWeaponProficiency)
                .Map(bps =>
                {
                    (BlueprintFeature feature, var lightArmor, var simpleWeapons) = bps.Expand();

                    feature.m_DisplayName = LocalizedStrings.Features_WarlockProficiencies_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_WarlockProficiencies_Description;

                    feature.AddAddFacts(c => c.m_Facts = new[]
                    {
                        lightArmor.ToReference<BlueprintUnitFactReference>(),
                        simpleWeapons.ToReference<BlueprintUnitFactReference>()
                    });

                    feature.AddArcaneArmorProficiency(c => c.Armor = new[] { ArmorProficiencyGroup.Light });

                    feature.SetIcon("11ab2b30adcd57341827dc076a172994", 21300000);

                    return feature;
                });
    }
}
