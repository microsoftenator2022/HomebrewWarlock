using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.Invocations;
using HomebrewWarlock.Fx;
using HomebrewWarlock.Homebrew;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using Kingmaker.Visual.Animation.Kingmaker.Actions;

using MicroWrath.Util.Linq;

namespace HomebrewWarlock.Features.EldritchBlast.Components
{
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

    [TypeId("77d75317-01c7-4fad-b1b1-eb08832b649e")]
    internal class EldritchBlastCalculateSpellLevel : ContextCalculateAbilityParams
    {
        public EldritchBlastCalculateSpellLevel() : this(1) { }

        public EldritchBlastCalculateSpellLevel(int equivalentSpellLevel)
        {
            SpellLevel = BaseEquivalentSpellLevel = equivalentSpellLevel;
            StatType = StatType.Charisma;
            ReplaceSpellLevel = true;
        }

        public int BaseEquivalentSpellLevel;

        public override AbilityParams Calculate(MechanicsContext context)
        {
            RecalculateSpellLevel(context);

            return base.Calculate(context);
        }

        public void RecalculateSpellLevel(MechanicsContext context)
        {
            if (context?.MaybeOwner is not { } owner) return;

            if (owner.Buffs.Enumerable
                .Where(b => b.IsTurnedOn)
                .SelectMany(b => b.Blueprint.ComponentsArray.OfType<EldritchBlastEssence>())
                .FirstOrDefault() is { } essence)
                this.SpellLevel = Math.Max(essence.EquivalentSpellLevel, this.BaseEquivalentSpellLevel);
            else this.SpellLevel = this.BaseEquivalentSpellLevel;
        }
    }

    [TypeId("40686a57-7952-4459-8dd9-3e6c0c830ebb")]
    internal class EldritchBlastEssence : UnitFactComponentDelegate
    {
        public static IEnumerable<Buff> GetEssenceBuffs(UnitEntityData unit) =>
            unit.Buffs.Enumerable.Where(buff => buff.BlueprintComponents.OfType<EldritchBlastEssence>().Any());

        public int EquivalentSpellLevel = 1;

        void RecalculateBlastDCs()
        {
            if (Context is null || Owner is null) return;

            foreach (var component in Owner.Abilities.Enumerable
                .SelectMany(a => a.Blueprint.Components.OfType<EldritchBlastCalculateSpellLevel>()))
            {
                component.RecalculateSpellLevel(Context);
            }
        }

        public override void OnTurnOn()
        {
            RecalculateBlastDCs();

            base.OnTurnOn();
        }

        public override void OnTurnOff()
        {
            RecalculateBlastDCs();

            base.OnTurnOff();
        }

        public virtual IDictionary<AbilityProjectileType, BlueprintProjectileReference[]> Projectiles { get; } =
            new (AbilityProjectileType, BlueprintProjectileReference[])[0].ToDictionary();

        public virtual ActionList Actions { get; set; }

        public virtual ActionList FxActions
        {
            get
            {
                var actions = new ActionList() { Actions = new GameAction[0] };

                if (Projectiles[AbilityProjectileType.Simple].FirstOrDefault() is { } blueprint)
                {
                    if (EldritchBlastOnHitFx.GetProjectileHitFx(blueprint) is { } onHit)
                        actions.Add(onHit);

                    if (EldritchBlastOnHitFx.GetProjectileHitSnapFx(blueprint) is { } onHitSnap)
                        actions.Add(onHitSnap);
                }

                return actions;
            }
        }

        public EldritchBlastEssence()
        {
            Actions = new();
        }
    }

    [TypeId("5b3a3656-5820-4175-969e-eee497dab50f")]
    internal class EldritchBlastElementalEssence : EldritchBlastEssence, IInitiatorRulebookHandler<RuleDealDamage>
    {
        public DamageEnergyType BlastDamageType = DamageEnergyType.Magic;
        public virtual void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            if (BlastDamageType is DamageEnergyType.Magic) return;

            if (evt.SourceAbility is null) return;
            if (!evt.SourceAbility.ComponentsArray.OfType<EldritchBlastComponent>().Any()) return;

            var damage = evt.DamageBundle.ToArray();
            evt.Remove(_ => true);

            foreach (var bd in damage)
            {
                if (bd is EnergyDamage ed && ed.EnergyType is DamageEnergyType.Magic)
                {
                    ed = new EnergyDamage(ed.Dice, ed.Bonus, BlastDamageType);
                    ed.CopyFrom(bd);

                    evt.Add(ed);
                }
                else evt.Add(bd);
            }
        }
        public virtual void OnEventDidTrigger(RuleDealDamage evt) { }
    }

    internal class EldritchBlastEssenceActions : ContextAction
    {
        public override string GetCaption() => "Eldritch Blast Essence Actions";

        public override void RunAction()
        {
            if (Context is null) return;

            var essenceComponents = new EldritchBlastEssence[0];

            if (Context.MaybeCaster is not null)
            {
                essenceComponents = EldritchBlastEssence.GetEssenceBuffs(Context.MaybeCaster)
                    .SelectMany(buff => buff.BlueprintComponents.OfType<EldritchBlastEssence>())
                    .ToArray();
            }

            foreach (var essence in essenceComponents)
            {
                essence.Actions.Run();
            }
        }
    }

    internal class EldritchBlastOnHitFx : ContextAction
    {
        public BlueprintProjectileReference? DefaultProjectile;

        internal static ContextActionSpawnFxOnLocator? GetProjectileHitFx(BlueprintProjectile projectile)
        {
            if (projectile.ProjectileHit.HitFx is not { } hitFx) return null;

            return new ContextActionSpawnFxOnLocator()
            {
                PrefabLink = new() { AssetId = hitFx.AssetId },
                TargetBone = projectile.TargetBone,
                TargetBoneOffsetMultiplier = projectile.TargetBoneOffsetMultiplier
            };
        }

        internal static ContextActionSpawnFxOnLocator? GetProjectileHitSnapFx(BlueprintProjectile projectile)
        {
            if (projectile.ProjectileHit.HitSnapFx is not { } hitSnapFx) return null;

            return new ContextActionSpawnFxOnLocator()
            {
                PrefabLink = new() { AssetId = hitSnapFx.AssetId }
            };
        }

        public override string GetCaption() => "Eldritch Blast FX";
        public override void RunAction()
        {
            if (Context is null) return;

            var essenceComponents = new EldritchBlastEssence[0];

            if (Context.MaybeCaster is not null)
            {
                essenceComponents = EldritchBlastEssence.GetEssenceBuffs(Context.MaybeCaster)
                    .SelectMany(buff => buff.BlueprintComponents.OfType<EldritchBlastEssence>())
                    .ToArray();
            }

            if (essenceComponents.Length == 0)
            {
                if (DefaultProjectile is null || DefaultProjectile.Get() is not { } projectile) return;

                GetProjectileHitFx(projectile)?.RunAction();
                GetProjectileHitSnapFx(projectile)?.RunAction();

                return;
            }

            foreach (var essence in essenceComponents)
            {
                essence.FxActions.Run();
            }
        }
    }

    [TypeId("dd6af2a9-111e-4d45-a254-62cbf7e4e478")]
    internal class DeliverEldritchBlastProjectile : AbilityDeliverProjectile
    {
        public BlueprintProjectileReference? DefaultProjectile;

        public override IEnumerator<AbilityDeliveryTarget> Deliver(AbilityExecutionContext context, TargetWrapper target)
        {
            base.m_Projectiles = new[] { DefaultProjectile };

            if (context is not null && context.MaybeCaster is { } caster)
            {
                var essenceProjectiles = EldritchBlastEssence.GetEssenceBuffs(caster)
                    .SelectMany(buff => buff.BlueprintComponents.OfType<EldritchBlastEssence>())
                    .Select(c => c.Projectiles)
                    .FirstOrDefault(ep => ep.ContainsKey(base.Type));

                if (essenceProjectiles is not null)
                    base.m_Projectiles = essenceProjectiles[base.Type];
            }

            return base.Deliver(context, target);
        }
    }

    internal class BlastAbility(int equivalentSpellLevel)
    {
        public static ContextActionDealDamage BaseDamage =>
            GameActions.ContextActionDealDamage(action =>
            {
                action.m_Type = ContextActionDealDamage.Type.Damage;

                action.DamageType.Type = DamageType.Energy;
                action.DamageType.Energy = DamageEnergyType.Magic;

                action.Value.DiceType = DiceType.D6;

                action.Value.DiceCountValue.ValueType = ContextValueType.Rank;
                action.Value.DiceCountValue.Value = 1;
                action.Value.DiceCountValue.ValueRank = AbilityRankType.DamageDice;

                action.WriteRawResultToSharedValue = true;
                action.ResultSharedValue = AbilitySharedValue.Damage;
            });

        public readonly int EquivalentSpellLevel = equivalentSpellLevel;

        public virtual ActionList DamageActions => new() { Actions = new[] { BaseDamage } };

        public virtual BlueprintAbility ConfigureAbility(
            BlueprintAbility ability,
            BlueprintFeatureReference rankFeature) =>
            ConfigureAbility(ability, rankFeature,
                UnitCommand.CommandType.Standard);

        public virtual BlueprintAbility ConfigureAbility(
            BlueprintAbility ability,
            BlueprintFeatureReference rankFeature,
            UnitCommand.CommandType castTime)
        {
            ability.Type = AbilityType.SpellLike;

            ability.CanTargetEnemies = true;
            ability.SpellResistance = true;
            ability.EffectOnAlly = AbilityEffectOnUnit.None;
            ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
            ability.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Directional;
            ability.ActionType = castTime;

            ability.AvailableMetamagic =
                Metamagic.Empower |
                Metamagic.Maximize |
                Metamagic.Quicken;

            ability.AddComponent<EldritchBlastComponent>();
            ability.AddComponent(new EldritchBlastCalculateSpellLevel(EquivalentSpellLevel));

            ability.AddContextRankConfig(c =>
            {
                c.m_Type = AbilityRankType.DamageDice;
                c.m_BaseValueType = ContextRankBaseValueType.FeatureRank;
                c.m_Feature = rankFeature;
                c.m_Progression = ContextRankProgression.AsIs;
                c.m_StartLevel = 0;
                c.m_StepLevel = 1;
            });

            var runAction = ability.EnsureComponent<AbilityEffectRunAction>();
            
            runAction.Actions.Add(DamageActions.Actions);
            runAction.Actions.Add(new EldritchBlastEssenceActions());

            //ability.AddContextRankConfig(crc =>
            //{
            //    crc.m_BaseValueType = ContextRankBaseValueType.MythicLevel;
            //    crc.m_Type = AbilityRankType.DamageDiceAlternative;
            //});

            //runAction.Actions.Add(GameActions.Conditional(c =>
            //{
            //    c.ConditionsChecker.Add(Conditions.ContextConditionCasterHasFact(casterHas =>
            //        casterHas.m_Fact = MythicBlast.CastBuffRef.ToReference<BlueprintUnitFact, BlueprintUnitFactReference>()));

            //    c.IfTrue.Add(GameActions.ContextActionDealDamage(damage =>
            //    {
            //        damage.DamageType.Type = DamageType.Energy;
            //        damage.DamageType.Energy = DamageEnergyType.Divine;

            //        damage.Value.DiceType = DiceType.D6;
            //        damage.Value.DiceCountValue.ValueType = ContextValueType.Rank;
            //        damage.Value.DiceCountValue.ValueRank = AbilityRankType.DamageDiceAlternative;

            //        damage.Value.BonusValue.ValueType = ContextValueType.Rank;
            //        damage.Value.BonusValue.ValueRank = AbilityRankType.DamageDiceAlternative;
            //    }));
            //}));

            return ability;
        }
    }

    internal class EldritchBlastTouch(BlueprintItemWeaponReference touchWeapon, int equivalentSpellLevel = 1) : BlastAbility(equivalentSpellLevel)
    {
        public override BlueprintAbility ConfigureAbility(BlueprintAbility ability, BlueprintFeatureReference rankFeature)
        {
            ability = base.ConfigureAbility(ability, rankFeature);

            ability.Range = AbilityRange.Touch;

            ability.AddComponent<AbilityDeliverTouch>(c => c.m_TouchWeapon = touchWeapon);

            return ability;
        }
    }

    internal class CastSpellWithContextParams : ContextAction
    {
        public BlueprintAbilityReference? Spell;
        public bool MarkAsChild;

        public override string GetCaption() => $"Cast {Spell?.Get()}";
        public override void RunAction()
        {
            if (base.Context.MaybeCaster is not { } caster)
                return;
            if (base.Target.Unit is not { } target)
                return;

            var data = new AbilityData(this.Spell, caster);

            data.OverrideCasterLevel = base.Context.Params.CasterLevel;
            data.OverrideDC = base.Context.Params.DC;
            data.OverrideSpellLevel = base.Context.Params.SpellLevel;

            data.MetamagicData = new() { MetamagicMask = base.Context.Params.Metamagic };

            if (this.MarkAsChild)
                data.IsChildSpell = true;

            var rule = new RuleCastSpell(data, target);

            rule.IsDuplicateSpellApplied = base.AbilityContext?.IsDuplicateSpellApplied ?? false;

            Rulebook.Trigger(rule);
        }
    }
}
