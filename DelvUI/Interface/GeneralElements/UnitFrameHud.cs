using DelvUI.Helpers;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace DelvUI.Interface.GeneralElements
{
    public unsafe class UnitFrameHud : HudElement, IHudElementWithActor
    {
        private UnitFrameConfig Config => (UnitFrameConfig)_config;
        private LabelHud _leftLabel;
        private LabelHud _rightLabel;

        private ImGuiWindowFlags _childFlags = 0;
        private readonly OpenContextMenuFromTarget _openContextMenuFromTarget;

        public GameObject Actor { get; set; } = null;

        public UnitFrameHud(string id, UnitFrameConfig config) : base(id, config)
        {
            // labels
            _leftLabel = new LabelHud(id + "_leftLabel", Config.LeftLabelConfig);
            _rightLabel = new LabelHud(id + "_rightLabel", Config.RightLabelConfig);

            // interaction stuff
            _openContextMenuFromTarget =
                Marshal.GetDelegateForFunctionPointer<OpenContextMenuFromTarget>(Plugin.SigScanner.ScanText("48 85 D2 74 7F 48 89 5C 24"));

            _childFlags |= ImGuiWindowFlags.NoTitleBar;
            _childFlags |= ImGuiWindowFlags.NoScrollbar;
            _childFlags |= ImGuiWindowFlags.AlwaysAutoResize;
            _childFlags |= ImGuiWindowFlags.NoBackground;
            _childFlags |= ImGuiWindowFlags.NoBringToFrontOnFocus;
        }

        public override void Draw(Vector2 origin)
        {
            if (!Config.Enabled || Actor == null)
            {
                return;
            }

            ImGuiWindowFlags windowFlags = 0;
            windowFlags |= ImGuiWindowFlags.NoBackground;
            windowFlags |= ImGuiWindowFlags.NoTitleBar;
            windowFlags |= ImGuiWindowFlags.NoMove;
            windowFlags |= ImGuiWindowFlags.NoDecoration;
            windowFlags |= ImGuiWindowFlags.NoInputs;

            var startPos = new Vector2(origin.X + Config.Position.X - Config.Size.X / 2f, origin.Y + Config.Position.Y - Config.Size.Y / 2f);
            var endPos = startPos + Config.Size;

            var drawList = ImGui.GetWindowDrawList();
            var addon = (AtkUnitBase*)Plugin.GameGui.GetAddonByName("ContextMenu", 1);

            DrawHelper.ClipAround(addon, ID, drawList, (drawListPtr, windowName) =>
            {
                ImGui.SetNextWindowPos(startPos);
                ImGui.SetNextWindowSize(Config.Size);

                ImGui.Begin(windowName, windowFlags);

                UpdateChildFlags(addon);

                if (ImGui.BeginChild(windowName, Config.Size, default, _childFlags))
                {
                    // health bar
                    if (Actor is not Character)
                    {
                        DrawFriendlyNPC(drawListPtr, startPos, endPos);
                    }
                    else
                    {
                        DrawChara(drawListPtr, origin, (Character)Actor);
                    }

                    // Check if mouse is hovering over the box properly
                    if (ImGui.IsMouseHoveringRect(startPos, endPos))
                    {
                        if (ImGui.GetIO().MouseClicked[0])
                        {
                            Plugin.TargetManager.SetTarget(Actor);
                        }
                        else if (ImGui.GetIO().MouseClicked[1])
                        {
                            var agentHud = new IntPtr(Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalID(4));
                            _openContextMenuFromTarget(agentHud, Actor.Address);
                        }
                    }
                }

                ImGui.EndChild();
                ImGui.End();

                // labels
                _leftLabel.DrawWithActor(origin + Config.Position, Actor);
                _rightLabel.DrawWithActor(origin + Config.Position, Actor);
            });
        }

        private unsafe void UpdateChildFlags(AtkUnitBase* addon)
        {
            if (!addon->IsVisible)
            {
                _childFlags &= ~ImGuiWindowFlags.NoInputs;
            }
            else
            {
                if (ImGui.IsMouseHoveringRect(new Vector2(addon->X, addon->Y), new Vector2(addon->X + addon->WindowNode->AtkResNode.Width, addon->Y + addon->WindowNode->AtkResNode.Height)))
                {
                    _childFlags |= ImGuiWindowFlags.NoInputs;
                }
                else
                {
                    _childFlags &= ~ImGuiWindowFlags.NoInputs;
                }
            }
        }

        private void DrawChara(ImDrawListPtr drawList, Vector2 origin, Character chara)
        {
            if (Config.TankStanceIndicatorConfig != null && Config.TankStanceIndicatorConfig.Enabled && JobsHelper.IsJobTank(chara.ClassJob.Id))
            {
                DrawTankStanceIndicator(drawList, origin);
            }

            var startPos = new Vector2(origin.X + Config.Position.X - Config.Size.X / 2f, origin.Y + Config.Position.Y - Config.Size.Y / 2f);
            var endPos = startPos + Config.Size;
            var scale = (float)chara.CurrentHp / Math.Max(1, chara.MaxHp);
            var color = Config.UseCustomColor ? Config.CustomColor.Map : Utils.ColorForActor(chara);
            var bgColor = BackgroundColor(chara);

            // background
            drawList.AddRectFilled(startPos, endPos, bgColor);

            // health
            drawList.AddRectFilledMultiColor(
                startPos,
                startPos + new Vector2(Config.Size.X * scale, Config.Size.Y),
                color["gradientLeft"],
                color["gradientRight"],
                color["gradientRight"],
                color["gradientLeft"]
            );

            // shield
            if (Config.ShieldConfig.Enabled)
            {
                var shield = Utils.ActorShieldValue(Actor);

                if (Config.ShieldConfig.FillHealthFirst)
                {
                    DrawHelper.DrawShield(shield, scale, startPos, Config.Size,
                        Config.ShieldConfig.Height, Config.ShieldConfig.HeightInPixels, Config.ShieldConfig.Color.Map, drawList);
                }
                else
                {
                    DrawHelper.DrawOvershield(shield, startPos, Config.Size,
                        Config.ShieldConfig.Height, Config.ShieldConfig.HeightInPixels, Config.ShieldConfig.Color.Map, drawList);
                }
            }

            // border
            drawList.AddRect(startPos, endPos, 0xFF000000);
        }

        private void DrawFriendlyNPC(ImDrawListPtr drawList, Vector2 startPos, Vector2 endPos)
        {
            var color = GlobalColors.Instance.NPCFriendlyColor;

            drawList.AddRectFilled(startPos, endPos, GlobalColors.Instance.EmptyUnitFrameColor.Base);

            drawList.AddRectFilledMultiColor(
                startPos,
                endPos,
                color.LeftGradient,
                color.RightGradient,
                color.RightGradient,
                color.LeftGradient
            );

            drawList.AddRect(startPos, endPos, 0xFF000000);
        }

        private void DrawTankStanceIndicator(ImDrawListPtr drawList, Vector2 origin)
        {
            if (Actor is not BattleChara battleChara)
            {
                return;
            }
            
            var tankStanceBuff = battleChara.StatusList.Where(
                o => o.StatusId is 79 or 91 or 392 or 393 or 743 or 1396 or 1397 or 1833
            );

            var thickness = Config.TankStanceIndicatorConfig.Thickness + 1;
            var barSize = new Vector2(Config.Size.Y > Config.Size.X ? Config.Size.X : Config.Size.Y, Config.Size.Y);
            var cursorPos = new Vector2(
                origin.X + Config.Position.X - Config.Size.X / 2f - thickness,
                origin.Y + Config.Position.Y - Config.Size.Y / 2f + thickness
            );

            var color = !tankStanceBuff.Any() ? Config.TankStanceIndicatorConfig.UnactiveColor : Config.TankStanceIndicatorConfig.ActiveColor;

            drawList.AddRectFilled(cursorPos, cursorPos + barSize, color.Base);
            drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);
        }

        private uint BackgroundColor(Character chara)
        {
            if (Config.ShowTankInvulnerability && chara is BattleChara battleChara && Utils.HasTankInvulnerability(battleChara))
            {
                uint color;
                if (Config.UseCustomInvulnerabilityColor)
                {
                    color = Config.CustomInvulnerabilityColor.Base;
                }
                else
                {
                    color = ImGui.ColorConvertFloat4ToU32(GlobalColors.Instance.SafeColorForJobId(chara.ClassJob.Id).Vector.AdjustColor(-.8f));
                }

                return color;
            }

            if (Config.UseCustomBackgroundColor)
            {
                return Config.CustomBackgroundColor.Base;
            }

            return GlobalColors.Instance.EmptyUnitFrameColor.Base;
        }

        private delegate void OpenContextMenuFromTarget(IntPtr agentHud, IntPtr gameObject);
    }
}
