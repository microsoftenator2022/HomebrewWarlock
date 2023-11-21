using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Mechanics.Conditions;

namespace HomebrewWarlock.NewConditions
{
    [TypeId("16b16f9a-3efa-4061-b10f-7ca4094f4fe0")]
    internal class ContextCasterHasSwiftAction : ContextCondition
    {
        public override bool CheckCondition()
        {
            var result = base.Context.MaybeCaster?.HasSwiftAction() ?? false;

            MicroLogger.Debug(() => $"{base.Context.MaybeCaster} has swift action? {result}");

            return result;
        }

        public override string GetConditionCaption() => "Caster has swift action";
    }
}
