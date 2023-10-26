using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace HomebrewWarlock.Resources
{
    internal static class Sprites
    {
        static Sprite GetAssemblyResourceSprite(string name) =>
            AssetUtils.GetSpriteAssemblyResource(Assembly.GetExecutingAssembly(), $"{nameof(HomebrewWarlock)}.Resources.{name}")!;

        internal static Sprite EldritchBlast => GetAssemblyResourceSprite("eb_icon.png");
        internal static Sprite EldritchSpear => GetAssemblyResourceSprite("es2_icon.png");
        internal static Sprite EldritchBlastOverlay => GetAssemblyResourceSprite("eb_overlay.png");
        internal static Sprite EldritchBlastOverlaySmall => GetAssemblyResourceSprite("eb_overlay_small.png");

        // TODO: Replace these with something based on the Spell Immunity icon - it's much closer to what I originally wanted
        internal static class DarkOnesOwnLuck
        {
            internal static Sprite Base => GetAssemblyResourceSprite("dool_icon.png");
            internal static Sprite Reflex => GetAssemblyResourceSprite("dol_icon_Ref.png");
            internal static Sprite Fortitude => GetAssemblyResourceSprite("dol_icon_Fort.png");
            internal static Sprite Will => GetAssemblyResourceSprite("dol_icon_Will.png");
        }

        internal static Sprite HideousBlow => GetAssemblyResourceSprite("hblow.png");
        internal static Sprite ChillingTentacles => GetAssemblyResourceSprite("tentacles.png");
        internal static Sprite EldritchCone => GetAssemblyResourceSprite("cone.png");
        internal static Sprite DarkDiscorporation => GetAssemblyResourceSprite("darkbats.png");
        internal static Sprite EldritchDoom => GetAssemblyResourceSprite("aoe.png");
        internal static Sprite EnervateRotated => GetAssemblyResourceSprite("rotatervate.png");
        internal static Sprite EldritchBlastMythic => GetAssemblyResourceSprite("eb_mythic_icon.png");

        internal static Sprite SummonSwarm => AssetUtils.Direct.GetSprite("4abed12203b403a47b0b32425580e5bb", 21300000);
        internal static Sprite BeguilingInfluence => AssetUtils.Direct.GetSprite("494cc3f31fcb2a24cb7e69ec5df0055c", 21300000);
        internal static Sprite OtherworldlyWhispers => AssetUtils.Direct.GetSprite("aec14e47a17206049aea57b6e325b900", 21300000);
        internal static Sprite Sickened => AssetUtils.Direct.GetSprite("d03c40abdab34d9498d7c492e4e8fecc", 21300000);
        internal static Sprite Shaken => AssetUtils.Direct.GetSprite("45798f988ea9f254780b54f6355a7ada", 21300000);
        internal static Sprite HellfireRay => AssetUtils.Direct.GetSprite("e9c468f1ba0bf304dbf34082cdecf2d2", 21300000);
        internal static Sprite Blind => AssetUtils.Direct.GetSprite("e1166369a1fd8184c8622aba232bec13", 21300000);
        internal static Sprite IceBlast => AssetUtils.Direct.GetSprite("056c53cea683cc544bf89b92f0b23000", 21300000);
        internal static Sprite TouchOfChaos => AssetUtils.Direct.GetSprite("1ad80155704e5764fb5e7540f7bb4b18", 21300000);
        internal static Sprite MoltenOrb => AssetUtils.Direct.GetSprite("50c4b1f118ead514fa88324ea210c3cb", 21300000);
        internal static Sprite SkillFocus => AssetUtils.Direct.GetSprite("42cb25b90b7c7d34e956c7822a9349cb", 21300000);
        internal static Sprite ChainLightning => AssetUtils.Direct.GetSprite("d22d729c1abd51d479a5571c69e7597e", 21300000);
        internal static Sprite Piercing => AssetUtils.Direct.GetSprite("fca17227026aa5346993963496450fbb", 21300000);
        internal static Sprite Confusion => AssetUtils.Direct.GetSprite("767a6ba6e391aff4388d2350778e899c", 21300000);
        internal static Sprite Nauseated => AssetUtils.Direct.GetSprite("64eaa7a3a1b457845812acfd7309739a", 21300000);
        internal static Sprite AcidArrow02 => AssetUtils.Direct.GetSprite("823ce7f3a53d1474eaad50c3d1d2bc17", 21300000);
        internal static Sprite BatteringBlast => AssetUtils.Direct.GetSprite("d8118b5286e9b354ca8865fcd3c44055", 21300000);
        internal static Sprite SpellCombat => AssetUtils.Direct.GetSprite("d7f95c54ea7a4b24f91b3f01cd84e251", 21300000);
        internal static Sprite MetamagicMastery => AssetUtils.Direct.GetSprite("15d11c952fdc96b45849f312f3931192", 21300000);
        internal static Sprite EmpowerSpell => AssetUtils.Direct.GetSprite("3c7ea7228ae2fdb4a9a87d7fdf3465cc", 21300000);
        internal static Sprite QuickenSpell => AssetUtils.Direct.GetSprite("ed900c6a7b1c40341b9aa325c6068e37", 21300000);
        internal static Sprite MaximizeSpell => AssetUtils.Direct.GetSprite("ee077ba5f8174f148a74c4543e58fac9", 21300000);
        internal static Sprite InfusedCurative => AssetUtils.Direct.GetSprite("39d4935e5d3dee04089959b1c324407b", 21300000);

        internal static Sprite EssenceSprite(Sprite baseSprite) => Sprite.Create(
            AssetUtils.AlphaBlend(baseSprite.texture, EldritchBlastOverlay.texture),
            //UnityUtil.AlphaBlend(UnityUtil.CopyReadable(baseSprite.texture), EldritchBlastOverlay.texture),
            baseSprite.rect,
            baseSprite.pivot);

        internal static Sprite SickeningBlast => EssenceSprite(Sickened);
        internal static Sprite FrightfulBlast => EssenceSprite(Shaken);
        internal static Sprite BrimstoneBlast => EssenceSprite(HellfireRay);
        internal static Sprite BeshadowedBlast => EssenceSprite(Blind);
        internal static Sprite HellrimeBlast => EssenceSprite(IceBlast);
        internal static Sprite BewitchingBlast => EssenceSprite(Confusion);
        internal static Sprite NoxiousBlast => EssenceSprite(Nauseated);
        internal static Sprite VitriolicBlast => EssenceSprite(AcidArrow02);
        internal static Sprite RepellingBlast => EssenceSprite(BatteringBlast);
        internal static Sprite UtterdarkBlast => EssenceSprite(EnervateRotated);
    }
}
