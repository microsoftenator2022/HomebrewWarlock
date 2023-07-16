using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using MicroWrath.Util;
using MicroWrath.Util.Unity;

using UnityEngine;

namespace HomebrewWarlock.Resources
{
    internal static class Sprites
    {
        static Sprite GetAssemblyResourceSprite(string name) =>
            AssetUtils.GetSpriteAssemblyResource(Assembly.GetExecutingAssembly(), $"{nameof(HomebrewWarlock)}.Resources.{name}")!;

        internal static Sprite EldritchBlast => GetAssemblyResourceSprite("eb_icon.png");
        internal static Sprite EldritchSpear => GetAssemblyResourceSprite("es_icon.png");
        internal static Sprite EldritchBlastOverlay => GetAssemblyResourceSprite("eb_overlay.png");
        internal static Sprite EldritchBlastOverlaySmall => GetAssemblyResourceSprite("eb_overlay_small.png");
        internal static class DarkOnesOwnLuck
        {
            internal static Sprite Base => GetAssemblyResourceSprite("dool_icon.png");
            internal static Sprite Reflex => GetAssemblyResourceSprite("dol_icon_Ref.png");
            internal static Sprite Fortitude => GetAssemblyResourceSprite("dol_icon_Fort.png");
            internal static Sprite Will => GetAssemblyResourceSprite("dol_icon_Will.png");
        }

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

        internal static Sprite EssenceSprite(Sprite baseSprite) => Sprite.Create(UnityUtil.AlphaBlend(
            UnityUtil.CopyReadable(baseSprite.texture), EldritchBlastOverlay.texture), baseSprite.rect, baseSprite.pivot);

        internal static Sprite SickeningBlast => EssenceSprite(Sickened);
        internal static Sprite FrightfulBlast => EssenceSprite(Shaken);
        internal static Sprite BrimstoneBlast => EssenceSprite(HellfireRay);
        internal static Sprite BeshadowedBlast => EssenceSprite(Blind);
        internal static Sprite HellrimeBlast => EssenceSprite(IceBlast);
    }
}
