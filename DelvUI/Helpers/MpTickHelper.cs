using ImGuiNET;
using System;
using System.Linq;
using Dalamud.Game;

namespace DelvUI.Helpers
{
    internal class MPTickHelper
    {
        public const double ServerTickRate = 3;
        protected const float PollingRate = 1 / 30f;
        private int _lastMpValue = -1;
        protected double LastTickTime;
        protected double LastUpdate;

        public MPTickHelper()
        {
            Plugin.Framework.Update += FrameworkOnOnUpdateEvent;
        }

        public double LastTick => LastTickTime;

        private void FrameworkOnOnUpdateEvent(Framework framework)
        {
            var player = Plugin.ClientState.LocalPlayer;
            if (player is null)
            {
                return;
            }

            var now = ImGui.GetTime();
            if (now - LastUpdate < PollingRate)
            {
                return;
            }

            LastUpdate = now;

            var mp = player.CurrentMp;

            // account for lucid dreaming screwing up mp calculations
            var lucidDreamingActive = player.StatusList.Any(e => e.StatusId == 1204);

            if (!lucidDreamingActive && _lastMpValue < mp)
            {
                LastTickTime = now;
            }
            else if (LastTickTime + ServerTickRate <= now)
            {
                LastTickTime += ServerTickRate;
            }

            _lastMpValue = (int)mp;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            Plugin.Framework.Update -= FrameworkOnOnUpdateEvent;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MPTickHelper() { Dispose(true); }
    }
}
