using System;
using System.Collections.Generic;
using System.Linq;

using HomebrewWarlock.Features.Invocations.Greater;

using UnityEngine;

using UnityModManagerNet;

namespace HomebrewWarlock
{
    internal partial class Main : ModMain, IMicroMod
    {
        internal static void OnUpdate(UnityModManager.ModEntry modEntry, float delta) =>
            Update(modEntry, delta);

        internal static event Action<UnityModManager.ModEntry, float> Update = (_, _) => { };

        static bool reloading;

        static bool constructorCalled;

        Main()
        {
            constructorCalled = true;
        }

        [Init]
        static void Init()
        {
            MicroLogger.Debug(() => "Main.Init");

            MicroLogger.Debug(() => $"Main constructor called? {constructorCalled}");

            var instance = Main.Instance as HomebrewWarlock.Main;
            
            if (instance?.ModEntry is null)
                return;
            
            MicroLogger.Debug(() => "Reload handler");

            instance.ModEntry.OnUpdate = OnUpdate;
#if DEBUG
            Update += (modEntry, delta) =>
            {
                if (!reloading && (
                        Input.GetKey(KeyCode.LeftAlt) ||
                        Input.GetKey(KeyCode.RightAlt)
                    ) &&
                    Input.GetKeyDown(KeyCode.R))
                {
                    MicroLogger.Debug(() => "Reload bundle requested");

                    ChillingTentacles.Fx.ReloadBundle();

                    reloading = true;
                }
                else if (reloading && !Input.GetKeyDown(KeyCode.R))
                {
                    reloading = false;
                }
            };
#endif
        }
    }
}
