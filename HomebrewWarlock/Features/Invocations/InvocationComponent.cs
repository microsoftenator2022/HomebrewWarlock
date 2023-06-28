using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Components;

using MicroWrath;
using MicroWrath.Components;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;

namespace HomebrewWarlock.Features.Invocations
{
    internal static class Invocation
    {
        internal static void AddInvocationComponents(this BlueprintAbility invocation, int equivalentSpellLevel = 1)
        {
            invocation.AddComponent<ContextCalculateAbilityParams>(c =>
            {
                c.StatType = StatType.Charisma;
                c.ReplaceSpellLevel = true;
                c.SpellLevel = equivalentSpellLevel;
            });

            invocation.AddComponent<ArcaneSpellFailureComponent>();
        }
    }
}
