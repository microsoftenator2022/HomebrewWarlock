using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;

namespace HomebrewWarlock.NewComponents
{

    [TypeId("6e992a69-b1ce-4080-804f-854d8196a9e8")]
    internal class AddClassLevelToCasterLevel : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateAbilityParams>
    {
        public BlueprintCharacterClassReference Class = null!;

        public ConditionsChecker Conditions = Default.ConditionsChecker;

        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            //static BlueprintCharacterClass? getSourceClass(EntityFact fact)
            //{
            //    if (fact.SourceFact is Feature f && f.SourceClass is not null)
            //        return f.SourceClass;

            //    if (fact.SourceFact is null)
            //        return null;

            //    return getSourceClass(fact.SourceFact);
            //}

            //if (evt.AbilityData is null || evt.AbilityData.Fact is null)
            //    return;

            //if (getSourceClass(evt.AbilityData.Fact) is not { } c || c != Class.Get()) 
            //    return;

            if (evt.AbilityData is null || evt.AbilityData.Caster is not { } caster)
                return;

            if (!Conditions.Check())
                return;

            evt.AddBonusCasterLevel(caster.Progression.GetClassLevel(Class.Get()));
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
    }
}
