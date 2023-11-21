using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;

namespace HomebrewWarlock.NewComponents
{
    [TypeId("e1f79e89-f767-4026-a6ce-84ebf1e30b99")]
    internal class SetMovementSpeed : UnitFactComponentDelegate
    {
        public override void OnTurnOn() =>
            base.Owner.Stats.Speed.AddModifierUnique(
                this.Value - base.Owner.Stats.Speed.BaseValue, base.Runtime, this.Descriptor);
        public override void OnTurnOff() => base.Owner.Stats.Speed.RemoveModifiersFrom(base.Runtime);

        public int Value = 30;

        public ModifierDescriptor Descriptor;
    }
}
