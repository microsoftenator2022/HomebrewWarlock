using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;

namespace HomebrewWarlock.NewComponents
{
    [AllowedOn(typeof(BlueprintFeature))]
    [AllowedOn(typeof(BlueprintBuff))]
    [TypeId("86391879-8ce3-4309-a7ef-87cc518aa3b0")]
    internal class AddFactAtRank : UnitFactComponentDelegate<AddFeatureIfHasFactData>, IFeatureRankHandler
    {
        public int Rank = 2;

        public BlueprintUnitFactReference? FactToAdd;

        protected void Apply()
        {
            if (base.Owner is null || base.Fact is null || this.FactToAdd is null)
                return;

            if (this.Rank < 2)
                MicroLogger.Warning($"{nameof(AddFactAtRank)}.{nameof(Rank)} < 2 will always be applied");

            if (base.Data.AppliedFact is not null)
            {
                if (Fact.GetRank() < Rank)
                    OnDeactivate();

                return;
            }

            if (Fact.GetRank() >= this.Rank)
                base.Data.AppliedFact = base.Owner.AddFact(FactToAdd, base.Context);
        }

        public virtual void HandleUnitGainFeatureRank(Feature feature) => this.Apply();
        public virtual void HandleUnitLostFeatureRank(Feature feature) => this.Apply();
        public override void OnActivate() => this.Apply();
        public override void OnDeactivate()
        {
            base.Owner.RemoveFact(base.Data.AppliedFact);
            base.Data.AppliedFact = null;
        }
    }
}
