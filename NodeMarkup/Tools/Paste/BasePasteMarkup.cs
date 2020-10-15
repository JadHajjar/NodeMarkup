﻿using ColossalFramework.Math;
using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using static ToolBase;

namespace NodeMarkup.Tools
{
    public abstract class BaseOrderToolMode : BaseToolMode
    {
        public static UITextureAtlas ButtonAtlas { get; } = GetButtonsIcons();
        private static UITextureAtlas GetButtonsIcons()
        {
            var spriteNames = new string[]
            {
                "TurnLeft",
                "Flip",
                "TurnRight",
            };

            var atlas = TextureUtil.GetAtlas(nameof(EntersOrderToolMode));
            if (atlas == UIView.GetAView().defaultAtlas)
                atlas = TextureUtil.CreateTextureAtlas("PasteButtons.png", nameof(EntersOrderToolMode), 50, 50, spriteNames, new RectOffset(0, 0, 0, 0));

            return atlas;
        }

        public Vector3 Centre { get; protected set; }
        public float Radius { get; protected set; }

        protected XElement Backup { get; set; }
        protected MarkupBuffer Buffer => Tool.MarkupBuffer;

        public bool IsMirror { get; protected set; }
        public SourceEnter[] SourceEnters { get; set; } = new SourceEnter[0];
        public TargetEnter[] TargetEnters { get; set; } = new TargetEnter[0];

        protected override void Reset(BaseToolMode prevMode)
        {
            if (prevMode is BaseOrderToolMode pasteMarkupTool)
            {
                Backup = pasteMarkupTool.Backup;
                IsMirror = pasteMarkupTool.IsMirror;
                SourceEnters = pasteMarkupTool.SourceEnters;
                TargetEnters = pasteMarkupTool.TargetEnters;
            }
            else
            {
                Backup = Markup.ToXml();
                IsMirror = false;
                SourceEnters = Tool.MarkupBuffer.Enters.Select((e, i) => new SourceEnter(e, i)).ToArray();
                TargetEnters = Markup.Enters.Select((e, i) => new TargetEnter(e, i)).ToArray();

                var min = Math.Min(TargetEnters.Length, SourceEnters.Length);
                for (var i = 0; i < min; i += 1)
                    SourceEnters[i].Target = TargetEnters[i];
            }

            Paste();
        }
        protected void Paste()
        {
            Markup.Clear();
            var map = new ObjectsMap(IsMirror);

            foreach (var source in SourceEnters)
            {
                var enterTarget = source.Target as TargetEnter;
                map.AddEnter(source.Enter.Id, enterTarget?.Enter.Id ?? 0);

                if (enterTarget != null)
                {
                    for (var i = 0; i < source.Points.Length; i += 1)
                        map.AddPoint(enterTarget.Enter.Id, i + 1, (source.Points[i].Target as Target)?.Num + 1 ?? 0);
                }
            }

            Markup.FromXml(Mod.Version, Buffer.Data, map);
            Panel.UpdatePanel();
        }
    }
    public abstract class BaseOrderToolMode<SourceType> : BaseOrderToolMode
        where SourceType : Source
    {
        public SourceType[] Sources { get; set; } = new SourceType[0];
        public Target<SourceType>[] Targets { get; set; } = new Target<SourceType>[0];

        public SourceType HoverSource { get; protected set; }
        public bool IsHoverSource => HoverSource != null;

        public SourceType SelectedSource { get; protected set; }
        public bool IsSelectedSource => SelectedSource != null;

        public Target<SourceType> HoverTarget { get; protected set; }
        public bool IsHoverTarget => HoverTarget != null;

        public Target<SourceType>[] AvailableTargets { get; protected set; }
        public abstract Func<int, SourceType, bool> AvailableTargetsGetter { get; }

        protected Basket<SourceType>[] Baskets { get; set; } = new Basket<SourceType>[0];

        protected override void Reset(BaseToolMode prevMode)
        {
            base.Reset(prevMode);

            HoverSource = null;
            SelectedSource = null;
            HoverTarget = null;

            Targets = GetTargets(prevMode);
            Sources = GetSources(prevMode);

            foreach (var target in Targets)
                target.Update(this);

            SetAvailableTargets();
            SetBaskets();
        }

        protected abstract SourceType[] GetSources(BaseToolMode prevMode);
        protected abstract Target<SourceType>[] GetTargets(BaseToolMode prevMode);

        public void GetHoverSource() => HoverSource = NodeMarkupTool.MouseRayValid ? Sources.FirstOrDefault(s => s.IsHover(NodeMarkupTool.MouseRay)) : null;
        public void GetHoverTarget() => HoverTarget = NodeMarkupTool.MouseRayValid ? AvailableTargets.FirstOrDefault(t => t.IsHover(NodeMarkupTool.MouseRay)) : null;

        public override void OnUpdate()
        {
            foreach (var source in Sources)
                source.Update(this);

            GetHoverSource();
            GetHoverTarget();
        }
        public override void OnMouseDown(Event e)
        {
            if (IsHoverSource)
            {
                SelectedSource = HoverSource;
                SetAvailableTargets();
            }
        }
        public override void OnPrimaryMouseClicked(Event e) => EndDrag();
        public override void OnMouseUp(Event e)
        {
            if (IsSelectedSource)
            {
                if (IsHoverTarget)
                {
                    foreach (var source in Sources)
                    {
                        if (source.Target == HoverTarget)
                            source.Target = null;
                    }

                    SelectedSource.Target = HoverTarget;
                }
                else
                    SelectedSource.Target = null;

                EndDrag();
                Paste();
            }
        }
        private void EndDrag()
        {
            SelectedSource = null;
            SetAvailableTargets();
            SetBaskets();
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            foreach (var basket in Baskets)
            {
                if (!IsSelectedSource || SelectedSource.Target == basket)
                    basket.Render(cameraInfo, this);
            }

            RenderOverlayAfterBaskets(cameraInfo);

            foreach (var target in Targets)
                target.Render(cameraInfo, this);

            RenderOverlayAfterTargets(cameraInfo);

            foreach (var source in Sources)
            {
                if (!IsSelectedSource || SelectedSource == source || (source.Target != null && source.Target != HoverTarget))
                    source.Render(cameraInfo, this);
            }
        }
        protected virtual void RenderOverlayAfterBaskets(RenderManager.CameraInfo cameraInfo) { }
        protected virtual void RenderOverlayAfterTargets(RenderManager.CameraInfo cameraInfo) { }

        protected void SetAvailableTargets() => AvailableTargets = IsSelectedSource ? GetAvailableTargets(SelectedSource).ToArray() : Targets.ToArray();
        private IEnumerable<Target<SourceType>> GetAvailableTargets(SourceType source)
        {
            var borders = new AvalibleBorders<SourceType>(this, source);
            return borders.GetTargets(Targets);
        }
        private void SetBaskets() => Baskets = GetBaskets();
        protected abstract Basket<SourceType>[] GetBaskets();
    }
}
