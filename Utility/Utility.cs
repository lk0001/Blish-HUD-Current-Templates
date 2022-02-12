﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blish_HUD;
using Microsoft.Xna.Framework;

namespace lk0001.CurrentTemplates.Utility
{
    public static class UpdateCadenceUtil
    {

        private static readonly Logger Logger = Logger.GetLogger(typeof(UpdateCadenceUtil));

        private static readonly HashSet<IntPtr> _asyncStateMonitor = new();

        public static void UpdateWithCadence(Action<GameTime> call, GameTime gameTime, double cadence, ref double lastCheck)
        {
            lastCheck += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (lastCheck >= cadence)
            {
                call(gameTime);
                lastCheck = 0;
            }
        }

        public static void UpdateAsyncWithCadence(Func<GameTime, Task> call, GameTime gameTime, double cadence, ref double lastCheck)
        {
            lastCheck += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (lastCheck >= cadence)
            {
                lock (_asyncStateMonitor)
                {
                    if (_asyncStateMonitor.Contains(call.Method.MethodHandle.Value))
                    {
                        Logger.Debug($"Async {call.Method.Name} has skipped its cadence because it has not completed running.");
                        return;
                    }

                    _asyncStateMonitor.Add(call.Method.MethodHandle.Value);
                }

                call(gameTime).ContinueWith(_ => {
                    lock (_asyncStateMonitor) _asyncStateMonitor.Remove(call.Method.MethodHandle.Value);
                });
                lastCheck = 0;
            }
        }

    }
}
