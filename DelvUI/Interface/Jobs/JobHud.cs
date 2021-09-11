using Dalamud.Plugin;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;

namespace DelvUI.Interface.Jobs
{
    public class JobHud : HudElement, IHudElementWithActor
    {
        protected DalamudPluginInterface PluginInterface => Plugin.PluginInterface;

        public JobConfig Config => (JobConfig)_config;

        public GameObject Actor { get; set; } = null;

        public JobHud(string ID, JobConfig config) : base(ID, config)
        {
        }

        public override void Draw(Vector2 origin)
        {
        }
    }
}
