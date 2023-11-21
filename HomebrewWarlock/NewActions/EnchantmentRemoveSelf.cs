using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Mechanics.Actions;

namespace HomebrewWarlock.NewActions
{
    [TypeId("380b1d40-f5a2-4641-a575-d06a80b4bab1")]
    internal class EnchantmentRemoveSelf : ContextAction
    {
        public override string GetCaption() => "Remove self";
        public override void RunAction()
        {
            ItemEnchantment.Data data = ContextData<ItemEnchantment.Data>.Current;

            var enchant = data.ItemEnchantment;

            enchant.DestroyFx();
            enchant.Owner.RemoveEnchantment(enchant);
        }
    }
}