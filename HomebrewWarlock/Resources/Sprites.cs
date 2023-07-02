using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using MicroWrath.Util;

using UnityEngine;

namespace HomebrewWarlock.Resources
{
    internal static class Sprites
    {
        static Sprite GetAssemblyResourceSprite(string name) =>
            AssetUtils.GetSpriteAssemblyResource(Assembly.GetExecutingAssembly(), $"{nameof(HomebrewWarlock)}.Resources.{name}")!;

        internal static Sprite EldritchBlast => GetAssemblyResourceSprite("eb_icon.png");
        internal static Sprite EldritchSpear => GetAssemblyResourceSprite("es_icon.png");
        internal static class DarkOnesOwnLuck
        {
            internal static Sprite Base => GetAssemblyResourceSprite("dool_icon.png");
            internal static Sprite Reflex => GetAssemblyResourceSprite("dol_icon_Ref.png");
            internal static Sprite Fortitude => GetAssemblyResourceSprite("dol_icon_Fort.png");
            internal static Sprite Will => GetAssemblyResourceSprite("dol_icon_Will.png");
        }
        internal static Sprite SummonSwarm => throw new NotImplementedException();
        internal static Sprite BeguilingInfluence => throw new NotImplementedException();
        internal static Sprite OtherworldlyWhispers => throw new NotImplementedException();
    }
}
