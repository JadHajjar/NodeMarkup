﻿using ColossalFramework.Math;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.UI;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ToolBase;
using ColossalFramework.UI;
using ColossalFramework;
using ModsCommon;

namespace NodeMarkup.Tools
{
    public class SelectToolMode : BaseSelectToolMode<NodeMarkupTool>, IToolModePanel, IToolMode<ToolModeType>
    {
        public bool ShowPanel => false;
        public ToolModeType Type => ToolModeType.Select;


        public override string GetToolInfo()
        {
            if (IsHoverNode)
                return string.Format(Localize.Tool_InfoHoverNode, HoverNode.Id) + GetStepOverInfo();
            else if (IsHoverSegment)
                return string.Format(Localize.Tool_InfoHoverSegment, HoverSegment.Id) + GetStepOverInfo();
            else
                return Localize.Tool_SelectInfo;
        }
        private string GetStepOverInfo() => NodeMarkupTool.SelectionStepOverShortcut.NotSet? string.Empty : "\n\n" + string.Format(CommonLocalize.Tool_InfoSelectionStepOver, Colors.AddInfoColor(NodeMarkupTool.SelectionStepOverShortcut));

        public override void OnPrimaryMouseClicked(Event e)
        {
            var markup = default(Markup);
            if (IsHoverNode)
                markup = SingletonManager<NodeMarkupManager>.Instance[HoverNode.Id];
            else if (IsHoverSegment)
                markup = SingletonManager<SegmentMarkupManager>.Instance[HoverSegment.Id];
            else
                return;

            SingletonMod<Mod>.Logger.Debug($"Select marking {markup}");
            Tool.SetMarkup(markup);

            if (markup.NeedSetOrder)
            {
                var messageBox = MessageBox.Show<YesNoMessageBox>();
                messageBox.CaptionText = Localize.Tool_RoadsWasChangedCaption;
                messageBox.MessageText = Localize.Tool_RoadsWasChangedMessage;
                messageBox.OnButton1Click = OnYes;
                messageBox.OnButton2Click = OnNo;
            }
            else
                OnNo();

            bool OnYes()
            {
                BaseOrderToolMode.IntersectionTemplate = markup.Backup;
                Tool.SetMode(ToolModeType.EditEntersOrder);
                markup.NeedSetOrder = false;
                return true;
            }
            bool OnNo()
            {
                Tool.SetDefaultMode();
                markup.NeedSetOrder = false;
                return true;
            }
        }
        protected override bool IsValidNode(ushort nodeId) => nodeId.GetNode().m_flags.CheckFlags(NetNode.Flags.None, NetNode.Flags.Middle | NetNode.Flags.Underground);

        protected override bool CheckItemClass(ItemClass itemClass) => itemClass.m_layer == ItemClass.Layer.Default && itemClass switch
        {
            { m_service: ItemClass.Service.Road } => true,
            { m_service: ItemClass.Service.PublicTransport, m_subService: ItemClass.SubService.PublicTransportPlane } => true,
            { m_service: ItemClass.Service.PublicTransport, m_subService: ItemClass.SubService.PublicTransportTrain } => true,
            { m_service: ItemClass.Service.PublicTransport, m_subService: ItemClass.SubService.PublicTransportMetro } => true,
            { m_service: ItemClass.Service.Beautification, m_subService: ItemClass.SubService.BeautificationParks } => true,
            _ => false,
        };

        public override void RenderGeometry(RenderManager.CameraInfo cameraInfo) => RenderLight(cameraInfo);
    }
}
