using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;

namespace HomebrewWarlock.NewComponents
{
    [TypeId("d93e4dbb-2ef2-475e-a62b-e3ae2dd2cf9d")]
    internal class AlignDamageWithCaster : UnitFactComponentDelegate, IInitiatorRulebookHandler<RulePrepareDamage>
    {
        public void OnEventAboutToTrigger(RulePrepareDamage evt)
        {
            if (!(evt.Reason.Fact?.Blueprint?.Components?.OfType<AlignDamageWithCaster>()?.Any() ?? false))
                return;

            MicroLogger.Debug(sb =>
            {
                sb.AppendLine($"Damage Reason: {evt.Reason.Name}");
                sb.AppendLine($"  SourceEntity: {evt.Reason.SourceEntity}");
                sb.AppendLine($"  Caster: {evt.Reason.Caster}");
                sb.Append($"  Fact: {evt.Reason.Fact}");
            });

            if (evt.Reason.Fact.Blueprint != this.Fact.Blueprint)
                return;

            if (evt.Reason.Caster is not { } caster)
                return;

            var casterAlignment = caster.Alignment.ValueVisible;

            MicroLogger.Debug(() => $"Caster alignment: {casterAlignment}");

            foreach (var alignment in casterAlignment.ToDamageAlignments())
            {
                MicroLogger.Debug(() => $"Adding {alignment} to damage");

                foreach (var damage in evt.DamageBundle)
                {
                    damage.AddAlignment(alignment);
                }
            }
        }

        public void OnEventDidTrigger(RulePrepareDamage evt) { }
    }
}
