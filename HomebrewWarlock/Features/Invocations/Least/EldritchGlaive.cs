using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using HomebrewWarlock.Features.EldritchBlast.Components;
using HomebrewWarlock.Fx;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using Kingmaker.View.Animation;
using Kingmaker.View.Equipment;
using Kingmaker.Visual.Animation.Kingmaker;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using Kingmaker.Visual.Sound;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

using UnityEngine;

namespace HomebrewWarlock.Features.Invocations.Least
{
    using BaseBlastFeatures =
        (BlueprintFeature baseFeature,
        BlueprintFeature rankFeature,
        BlueprintAbility baseAbility,
        BlueprintProjectile projectile);

    internal static class EldritchGlaive
    {
        internal class SkipEquipAnimation : BlueprintComponent
        {
            [HarmonyPatch(typeof(UnitViewHandsEquipment), nameof(UnitViewHandsEquipment.HandleEquipmentSlotUpdated), typeof(HandSlot), typeof(ItemEntity))]
            static class UnitViewHandsEquipment_HandleEquipmentSlotUpdated_Patch
            {
                static bool Prefix(HandSlot slot, ItemEntity previousItem, UnitViewHandsEquipment __instance)
                {
                    var item = slot.MaybeItem;

                    if (item is not null && item.Blueprint.ComponentsArray.OfType<SkipEquipAnimation>().Any() ||
                        previousItem is not null && previousItem.Blueprint.ComponentsArray.OfType<SkipEquipAnimation>().Any())
                    {
                        __instance.ChangeEquipmentWithoutAnimation();
                        return false;
                    }

                    return true;
                }
            }
        }

        internal class AddEldritchGlaive : UnitBuffComponentDelegate<AddKineticistBladeData>, IAreaActivationHandler
        {
            public BlueprintItemWeaponReference? Weapon;

            private ItemEntityWeapon CreateWeapon() => this.Weapon?.Get().CreateEntity<ItemEntityWeapon>()!;

            public AddEldritchGlaive() : base() { }

            public AddEldritchGlaive(BlueprintItemWeaponReference weapon) : base()
            {
                Weapon = weapon;
            }

            public override void OnActivate()
            {
                if (Weapon is null)
                {
                    MicroLogger.Error($"{nameof(EldritchGlaive)}: Weapon is null");
                    return;
                }

                base.OnActivate();

                base.Owner.MarkNotOptimizableInSave();

                base.Data.Applied = this.CreateWeapon();
                base.Data.Applied.MakeNotLootable();
                base.Data.Applied.VisualSourceItemBlueprint = this.Weapon.Get();

                using (ContextData<ItemEntity.CanBeEquippedForce>.Request())
                {
                    if (!base.Owner.Body.PrimaryHand.CanInsertItem(Data.Applied))
                    {
                        base.Data.Applied = null;
                        return;
                    }

                    using (ContextData<ItemsCollection.SuppressEvents>.Request())
                    {
                        base.Owner.Body.PrimaryHand.InsertItem(base.Data.Applied);
                    }
                }
                
            }

            public override void OnDeactivate()
            {
                base.OnDeactivate();

                if (base.Data.Applied is null) return;

                base.Data.Applied.HoldingSlot?.RemoveItem();

                using (ContextData<ItemsCollection.SuppressEvents>.Request())
                {
                    base.Data.Applied.Collection?.Remove(base.Data.Applied);
                }

                base.Data.Applied = null;
            }

            public override void OnTurnOn() =>
                base.Data.Applied?.HoldingSlot?.Lock?.Retain();

            public override void OnTurnOff() =>
                base.Data.Applied?.HoldingSlot?.Lock?.Release();

            public void OnAreaActivated()
            {
                if (base.Data.Applied is not null) return;

                this.OnActivate();
                this.OnTurnOn();
            }
        }

        internal class UseCustomWeaponRange : BlueprintComponent
        {
            public BlueprintItemWeaponReference? Weapon;

            [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.GetApproachDistance))]
            static class AbilityData_GetApproachDistance_Patch
            {
                static ItemEntityWeapon? TryGetEldritchGlaiveWeapon(AbilityData abilityData)
                {
                    var uegr = abilityData.Blueprint.ComponentsArray.OfType<UseCustomWeaponRange>().FirstOrDefault();
                    
                    return uegr?.Weapon?.Get()?.CreateEntity<ItemEntityWeapon>();
                }

                [HarmonyTranspiler]
                static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    var match = instructions.FindInstructionsIndexed(new Func<CodeInstruction, bool>[]
                    {
                        ci => ci.opcode == OpCodes.Ldarg_0,
                        ci => ci.Calls(AccessTools.PropertyGetter(typeof(AbilityData), nameof(AbilityData.Range))),
                        ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(9),
                        ci => ci.opcode == OpCodes.Bne_Un_S
                    });

                    if (match.Count() != 4)
                    {
                        MicroLogger.Error($"{nameof(AbilityData_GetApproachDistance_Patch)}: Could not find {nameof(match)}");
                        return instructions;
                    }

                    var insertLocation = match.Last().index + 1;

                    var match2 = instructions
                        .FindInstructionsIndexed(new Func<CodeInstruction, bool>[]
                        {
                            ci => ci.opcode == OpCodes.Ldarg_0,
                            ci => ci.Calls(AccessTools.PropertyGetter(typeof(AbilityData), nameof(AbilityData.Caster))),
                            ci => ci.Calls(AccessTools.PropertyGetter(typeof(UnitDescriptor), nameof(UnitDescriptor.Unit))),
                            ci => ci.Calls(AccessTools.Method(typeof(UnitEntityData), nameof(UnitEntityData.GetFirstWeapon))),
                            ci => ci.opcode == OpCodes.Dup,
                            ci => ci.opcode == OpCodes.Brtrue_S
                        }, match.Last().index);

                    if (match2.Count() != 6)
                    {
                        MicroLogger.Error($"{nameof(AbilityData_GetApproachDistance_Patch)}: Could not find {nameof(match2)}");
                        return instructions;
                    }

                    var targetLabel = match2.Last().instruction.operand;
                    
                    var newIs = new CodeInstruction[]
                    {
                        new CodeInstruction(OpCodes.Ldarg_0),
                        CodeInstruction.Call((AbilityData ad) => TryGetEldritchGlaiveWeapon(ad)),
                        new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Brtrue_S, targetLabel),
                        new CodeInstruction(OpCodes.Pop)
                    };

                    var iList = instructions.ToList();

                    iList.InsertRange(insertLocation, newIs);

                    return iList;
                }
            }
        }

        internal class DeliverFullAttack : AbilityCustomLogic
        {
            public bool IgnoreDamageStatBonus;
            public bool IgnoreBusyHands;

            public override IEnumerator<AbilityDeliveryTarget?> Deliver(AbilityExecutionContext context, TargetWrapper target)
            {
                if (context.MaybeCaster is not { } caster)
                    yield break;

                if (target?.Unit is not { } targetUnit)
                    yield break;

                var attacks = UnitAttack.EnumerateFullAttack(caster).ToList();

                MicroLogger.Debug(() => $"{attacks.Count()} attacks");

                var attackNumber = 0;

                foreach (var attackInfo in attacks)
                {
                    var weapon = attackInfo.Hand?.Weapon;

                    MicroLogger.Debug(() => $"Weapon: {weapon?.Name}");
                    MicroLogger.Debug(() => $"Weapon animation: {weapon?.GetAnimationStyle()}");

                    while (caster.AreHandsBusyWithAnimation && !IgnoreBusyHands)
                    {
                        caster.View.AnimationManager.Tick();
                        yield return null;
                    }

                    UnitAnimationType at = UnitAnimationType.SpecialAttack;
                    if (attackInfo.Hand == caster.Body.PrimaryHand)
                        at = UnitAnimationType.MainHandAttack;
                    else if (attackInfo.Hand == caster.Body.SecondaryHand)
                        at = UnitAnimationType.OffHandAttack;

                    MicroLogger.Debug(() => $"Animation type: {at}");

                    var animation = caster.View.AnimationManager.CreateHandle(at);
                    animation.AttackWeaponStyle = weapon?.GetAnimationStyle() ?? WeaponAnimationStyle.None;

                    caster.View.AnimationManager.Execute(animation);

                    MicroLogger.Debug(() => $"Animation weapon style: {animation?.AttackWeaponStyle}");
                    MicroLogger.Debug(() => $"Animation handle execution mode: {animation?.ExecutionMode}");

                    MicroLogger.Debug(sb =>
                    {
                        sb.Append("Active Animations:");
                        foreach (var a in caster.View.AnimationManager.ActiveAnimations)
                        {
                            sb.AppendLine();
                            sb.Append($"{a.Handle.Action.name}: {a.State}");
                        }
                    });

                    var attack = new RuleAttackWithWeapon(caster, targetUnit, weapon, 0)
                    {
                        Reason = context,
                        IsFullAttack = true,
                        AttackNumber = attackNumber,
                        AttacksCount = attacks.Count()
                    };

                    if (IgnoreDamageStatBonus)
                        attack.WeaponStats.OverrideDamageBonusStatMultiplier(0);

                    attack = context.TriggerRule(attack);

                    MicroLogger.Debug(() => $"{attack.AttackRoll.Result}");

                    while (!animation.IsStarted || !animation.IsActed)
                    {
                        caster.View.AnimationManager.Tick();
                        yield return null;
                    }

                    yield return new(target) { AttackRoll = attack.AttackRoll };
                }
            }

            public override void Cleanup(AbilityExecutionContext context) { }
        }

        [LocalizedString]
        internal const string DisplayName = "Eldritch Glaive";

        [LocalizedString]
        internal static readonly string Description =
            "Your eldritch blast takes on physical substance, appearing similar to a glaive." + Environment.NewLine +
            //"As a full-round action, you can make a single melee touch attack as if wielding a reach weapon. " +
            "Attacks with this weapon are melee touch attacks. " +
            "If you hit, your target is affected as if struck by your eldritch blast (including any eldritch " +
            "essence applied to the blast). " +
            "Unlike hideous blow, you cannot combine your eldritch glaive with damage from a held weapon. " +
            Environment.NewLine + "Furthermore, " +
            //"until the start of your next turn, " +
            "you also threaten nearby squares as if wielding a reach weapon, and you can make attacks of opportunity " +
            "with your eldritch glaive." + Environment.NewLine +
            "If your base attack bonus is +6 or higher, you can make as many attacks with your eldritch glaive as " +
            "your base attack bonus allows.";
        //+
        //Environment.NewLine + "For example, a 12th-level warlock could attack twice, once with a base attack " +
        //"bonus of +6, and again with a base attack bonus of +1.";

        [LocalizedString]
        internal const string Duration = "1 round";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintItemWeapon> CreateWeapon(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BaseBlastFeatures> baseFeatures)
        {
            var weaponModel = context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintItemWeapon.DemonicHungerItem)
                .Map(weapon =>
                {
                    return weapon.VisualParameters.m_WeaponModel.CreateDynamicProxy(wm =>
                    {
                        var material = wm.transform.Find("THW_GlaiveBaphometCultLeader").gameObject.GetComponent<MeshRenderer>().material;

                        material.SetColor("_RimColor", new(0, 0, 0, 1));
                        material.SetFloat("_RimLighting", 1);

                        var texture_d = AssetUtils.GetTextureAssemblyResource(
                            Assembly.GetExecutingAssembly(),
                            $@"{nameof(HomebrewWarlock)}\Resources\THW_GlaiveBaphometCultLeader_D3.png",
                            mipChain: true);

                        var texture_m = AssetUtils.GetTextureAssemblyResource(
                            Assembly.GetExecutingAssembly(),
                            $@"{nameof(HomebrewWarlock)}\Resources\THW_GlaiveBaphometCultLeader_M2.png",
                            mipChain: true);

                        material.SetTexture("_BaseMap", texture_d);
                        material.SetTexture("_MasksMap", texture_m);
                    });
                });

            var weaponType = context.CloneBlueprint(BlueprintsDb.Owlcat.BlueprintWeaponType.Glaive,
                GeneratedGuid.Get("EldritchGlaiveWeaponType"),
                nameof(GeneratedGuid.EldritchGlaiveWeaponType))
                .Map(wt =>
                {
                    wt.m_DefaultNameText = wt.m_TypeNameText =
                        LocalizedStrings.Features_Invocations_Least_EldritchGlaive_DisplayName;
                    
                    wt.Category = WeaponCategory.Touch;
                    wt.m_AttackType = AttackType.Touch;
                    wt.m_CriticalModifier = DamageCriticalModifierType.X2;
                    wt.m_BaseDamage.m_Dice = DiceType.D6;

                    wt.m_IsLight = true;

                    wt.m_FighterGroupFlags = default;

                    wt.m_Weight = 0;

                    wt.VisualParameters.m_SoundType = WeaponSoundType.Unarmed;
                    wt.VisualParameters.m_MissSoundType = WeaponMissSoundType.MediumBlunt;

                    return wt;
                });

            var onHit = context.NewBlueprint<BlueprintAbility>(
                GeneratedGuid.Get("EldritchGlaiveOnHitAbility"),
                nameof(GeneratedGuid.EldritchGlaiveOnHitAbility))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintItemWeapon.TouchItem))
                .Combine(baseFeatures)
                .Map(bps =>
                {
                    var (ability, touch, baseFeatures) = bps.Expand();

                    ability.m_DisplayName = baseFeatures.baseFeature.m_DisplayName;

                    return new EldritchBlastTouch(touch.ToReference<BlueprintItemWeaponReference>())
                        .ConfigureAbility(ability, baseFeatures.rankFeature.ToReference<BlueprintFeatureReference>());
                });

            var enchant = context.NewBlueprint<BlueprintWeaponEnchantment>(
                GeneratedGuid.Get("EldritchGlaiveWeaponEnchantment"),
                nameof(GeneratedGuid.EldritchGlaiveWeaponEnchantment))
                .Combine(onHit)
                .Map(bps =>
                {
                    var (enchant, onHit) = bps;

                    enchant.m_EnchantName = LocalizedStrings.Features_EldritchBlast_EldritchBlast_DisplayName;

                    enchant.AddComponent<AddInitiatorAttackWithWeaponTrigger>(c =>
                    {
                        c.OnlyHit = true;
                        c.Action.Add(GameActions.ContextActionCastSpell(c => c.m_Spell = onHit.ToReference<BlueprintAbilityReference>()));
                    });

                    enchant.WeaponFxPrefab = WeaponFxPrefabs.Standard;

                    return enchant;
                });

            var weapon = context.CloneBlueprint(BlueprintsDb.Owlcat.BlueprintItemWeapon.DemonicHungerItem,
                GeneratedGuid.Get("EldritchGlaiveWeapon"),
                nameof(GeneratedGuid.EldritchGlaiveWeapon))
                .Combine(weaponType)
                .Combine(weaponModel)
                .Combine(enchant)
                .Map(weaponAndModel =>
                {
                    var (weapon, weaponType, model, enchant) = weaponAndModel.Expand();

                    weapon.m_Type = weaponType.ToReference<BlueprintWeaponTypeReference>();

                    weapon.Components = new BlueprintComponent[0];

                    weapon.AddComponent<SkipEquipAnimation>();

                    weapon.VisualParameters.m_WeaponModel = model;
                    
                    weapon.m_DisplayNameText = LocalizedStrings.Features_Invocations_Least_EldritchGlaive_DisplayName;
                    weapon.m_FlavorText = weapon.m_DescriptionText = Default.LocalizedString;

                    //weapon.m_Cost = 0;

                    weapon.SpendCharges = false;
                    weapon.Charges = 0;

                    weapon.m_Enchantments = new[]
                    {
                        enchant.ToReference<BlueprintWeaponEnchantmentReference>()
                    };

                    weapon.m_OverrideDamageDice = true;
                    weapon.m_DamageDice.m_Rolls = 0;
                    weapon.m_DamageDice.m_Dice = DiceType.Zero;

                    return weapon;
                });

            return weapon;
        }

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BaseBlastFeatures> baseFeatures)
        {
            var weapon = CreateWeapon(context, baseFeatures);

            var buff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("EldritchGlaiveBuff"),
                nameof(GeneratedGuid.EldritchGlaiveBuff))
                .Combine(weapon)
                .Map(bps =>
                {
                    var (buff, weapon) = bps;

                    buff.m_DisplayName = LocalizedStrings.Features_Invocations_Least_EldritchGlaive_DisplayName;
                    buff.m_Description = LocalizedStrings.Features_Invocations_Least_EldritchGlaive_Description;

                    buff.AddComponent<AddEldritchGlaive>(c => c.Weapon = weapon.ToReference<BlueprintItemWeaponReference>());
                    //buff.AddActionsOnBuffApply(c => c.Actions.Add(GameActions.ContextActionProvokeAttackOfOpportunity(a => a.ApplyToCaster = true)));

                    buff.m_Flags = BlueprintBuff.Flags.HiddenInUi | BlueprintBuff.Flags.IsFromSpell;

                    return buff;
                });

            var attack = context.NewBlueprint<BlueprintAbility>(
                GeneratedGuid.Get("EldritchGlaiveAttackAbility"),
                nameof(GeneratedGuid.EldritchGlaiveAttackAbility))
                .Combine(buff)
                .Map(bps =>
                {
                    var (ability, buff) = bps;

                    ability.m_DisplayName = buff.m_DisplayName;

                    ability.AddComponent<DeliverFullAttack>(c => c.IgnoreDamageStatBonus = true);

                    ability.ActionType = UnitCommand.CommandType.Free;

                    ability.CanTargetEnemies = true;
                    ability.ShouldTurnToTarget = true;
                    ability.Hidden = true;

                    ability.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Special;

                    return ability;
                });
             
            var ability = context.NewBlueprint<BlueprintAbility>(
                GeneratedGuid.Get("EldritchGlaiveAbility"),
                nameof(GeneratedGuid.EldritchGlaiveAbility))
                .Combine(buff)
                .Combine(baseFeatures)
                .Combine(weapon)
                .Combine(attack)
                .Map(bps =>
                {
                    var (ability, buff, baseFeatures, weapon, attack) = bps.Expand();

                    ability.m_DisplayName = buff.m_DisplayName;
                    ability.m_Description = buff.m_Description;

                    ability.Type = AbilityType.Special;

                    ability.CanTargetEnemies = true;

                    ability.EffectOnAlly = AbilityEffectOnUnit.None;
                    ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;

                    ability.ActionType = UnitCommand.CommandType.Standard;
                    ability.m_IsFullRoundAction = true;

                    ability.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Immediate;

                    ability.AddComponent<ArcaneSpellFailureComponent>();
                    ability.AddComponent<EldritchBlastCalculateSpellLevel>();

                    //ability.AddComponent<AbilityDeliveredByWeapon>();

                    ability.AddComponent<AbilityEffectRunAction>(c =>
                    {
                        c.Actions.Add(
                            GameActions.ContextActionApplyBuff(a =>
                            {
                                a.ToCaster = true;
                                a.m_Buff = buff.ToReference<BlueprintBuffReference>();
                                a.DurationValue.BonusValue = 1;
                            }),
                            GameActions.ContextActionCastSpell(a =>
                            {
                                a.m_Spell = attack.ToReference<BlueprintAbilityReference>();
                            }));
                    });

                    ability.Range = AbilityRange.Weapon;
                    
                    ability.AddComponent<UseCustomWeaponRange>(c => c.Weapon = weapon.ToReference<BlueprintItemWeaponReference>());

                    ability.LocalizedDuration = LocalizedStrings.Features_Invocations_Least_EldritchGlaive_Duration;

                    return ability;
                });

            var toggle = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("EldritchGlaiveToggleAbility"),
                nameof(GeneratedGuid.EldritchGlaiveToggleAbility))
                .Combine(buff)
                .Map(bps =>
                {
                    var (toggle, buff) = bps;

                    toggle.m_DisplayName = buff.m_DisplayName;
                    toggle.m_Description = buff.m_Description;

                    toggle.ActivationType = AbilityActivationType.WithUnitCommand;
                    toggle.m_ActivateWithUnitCommand = UnitCommand.CommandType.Move;

                    toggle.m_Buff = buff.ToReference<BlueprintBuffReference>();

                    toggle.DeactivateIfOwnerUnconscious = true;

                    return toggle;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("EldritchGlaiveFeature"), nameof(GeneratedGuid.EldritchGlaiveFeature))
                .Combine(ability)
                .Combine(toggle)
                .Map(bps =>
                {
                    var (feature, ability, toggle) = bps.Expand();

                    feature.m_DisplayName = ability.m_DisplayName;
                    feature.m_Description = ability.m_Description;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return feature;
                });

            return feature;
        }
    }
}
