# HomebrewWarlock (name pending)

My attempt at porting 3.5e Warlock (Complete Arcane) to WotR.

### Requires [ModMenu](https://github.com/WittleWolfie/ModMenu)

Mild changes were made in a few areas to fit better with PF1e and WotR. eg. some 24h invocations are now permanent, some are toggled instead.
Where possible I have tried to use Pathfinder equivalents for spells and feats.

## Currently implemented

All base class features except Imbue Item (crafting).

#### Eldritch Blast
1d6 Magic damage. Increases by 1d6 at levels 3, 5, 7, 9, 11, 13, 14, 17, 20

![EB_screenshot](https://github.com/microsoftenator2022/HomebrewWarlock/assets/105488202/86ad3700-8c2d-49b3-8aff-999393bc0068)

#### Deceive Item
Take 10 on UMD

#### Damage reduction
+1/cold iron at levels 3, 7, 11, 15, 19

#### Fiendish Resilience
Fast Healing 1/2/5 (levels 8, 13, 18)

#### Energy Resistance
resistance 5 vs 2 elements, increases to 10 at level 20

### Invocations
#### Least

- Beguiling influence - +6 to persuade checks. Permanent.
- Dark One's Own Luck - +CHA to one save type. Toggled.
- Eldritch Glaive - Blast Shape. Reach touch attack weapon. Makes a full attack as a full round action and can perform AoO for 1 round.

![EG_screenshot](https://github.com/microsoftenator2022/HomebrewWarlock/assets/105488202/ea7b966f-da5e-4b56-a649-d711c5d33154)

- Eldritch Spear - EB shape: Short -> Long range.
- Frightful Blast - EB essence: Inflicts shaken.
- Hideous Blow - Single melee weapon attack + EB damage.
- See the Unseen - +6 to Knowledge (Arcana) and Lore (Religion). Permanent.
- Sickening Blast - EB essence: inflicts sickened.
- Summon Swarm (Spiders) - As Summon Swarm spell.

#### Lesser
- Beshadowed Blast - EB essence: inflicts blinded.
- Brimstone Blast - EB essence: Fire damage + 2d6 fire damage per round for CL/5 rounds.

![BB_screenshot](https://github.com/microsoftenator2022/HomebrewWarlock/assets/105488202/c2ad6aa8-549e-410b-92eb-43a70e55ffb5)

- Curse of Despair - As Bestow Curse spell.
- Eldritch Chain - EB shape: Chains to an additional target per 5 CL.
- Fell Flight - Wings. Toggled.
- Hellrime Blast - EB essence: Cold damage and -4 DEX for 10 minutes.
- The Dead Walk - As Animate Dead spell.
- Voidsense - 30ft Blindsense.
- Voracious Dispel - Dispel + damages caster of dispelled effect.
- Walk Unseen - Standard invisibility. Toggled.

#### Greater
- Bewitching Blast - EB essence: inflicts confused.
- Chilling Tentacles (!!!) - Black Tentacles + cold AoE.
- Devour Magic - Targeted Greater Dispel + heal.
- Eldritch Cone - EB shape: 30ft Cone.
- Noxious Blast - EB essence: Inflicts nausea.
- Repelling Blast - EB essence: YEET!
- Vitriolic Blast - EB essence: Acid damage, ignores SR, 2d6 acid damage per round for 3 rounds.

![VB_screenshot](https://github.com/microsoftenator2022/HomebrewWarlock/assets/105488202/fe47652f-b3d4-4364-8617-ea9b37fa940d)

#### Dark

- Dark Discorporation - Polymorph to ~~bat~~raven swarm

![DD_screenshot](https://github.com/microsoftenator2022/HomebrewWarlock/assets/105488202/a084d530-66a5-487c-ad0e-2ff0152e9373)
- Eldritch Doom - EB shape: 20ft AoE centered on caster.
- Utterdark Blast - EB essence: Negative energy damage + 2 negative levels.
- Word of Changing - As Baleful Polymorph spell.

### Feats
Ability Focus (Eldritch Blast)

#### Metamagic (Eldritch Blast)
- Empower
- Maximize
- Quicken

### Homebrew
- Mythic Eldritch Blast: 1d6 + 1 divine damage on eldritch blasts per mythic rank.
  Compared to Kineticist, Warlock's raw damage output is pretty unimpressive. This is a rough attempt to address this. This is subject to change.
- Eldritch Glaive Toggle: optionally can change Eldritch Glaive to a toggle like Kinetic Blade instead of a full-round action.
- More to come.

### Known Issues and Missing Features
- Imbue Item: will be added in the future
- ~~Utterdark blast does not heal undead (they are just immune)~~ fixed in 0.9.1
- Missing elemental weapon enchant FX.
- Hideous Blow does not work with unarmed attacks
- ~~Mythic Eldritch Blast cannot be toggled so all blasts become full actions~~ fixed in 0.9.1
