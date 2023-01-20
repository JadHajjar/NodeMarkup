﻿using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace NodeMarkup.UI.Editors
{
    public class PointsEditor : SimpleEditor<PointsItemsPanel, MarkingEnterPoint>
    {
        public override string Name => NodeMarkup.Localize.PointEditor_Points;
        public override string EmptyMessage => string.Empty;
        public override Marking.SupportType Support { get; } = Marking.SupportType.Points;

        protected PropertyGroupPanel TemplatePanel { get; private set; }
#if DEBUG
        protected PropertyGroupPanel DebugPanel { get; private set; }
#endif

        private FloatPropertyPanel Offset { get; set; }
        private BoolListPropertyPanel Split { get; set; }
        private FloatPropertyPanel Shift { get; set; }

        protected override IEnumerable<MarkingEnterPoint> GetObjects() => Markup.Enters.SelectMany(e => e.EnterPoints);
        protected override void OnObjectSelect(MarkingEnterPoint point)
        {
            base.OnObjectSelect(point);

            TemplatePanel = ComponentPool.Get<PropertyGroupPanel>(ContentPanel.Content);
            TemplatePanel.StopLayout();
            FillTemplatePanel(point);
            TemplatePanel.StartLayout();
            TemplatePanel.Init();
#if DEBUG
            DebugPanel = ComponentPool.Get<PropertyGroupPanel>(ContentPanel.Content);
            DebugPanel.StopLayout();
            FillDebugPanel(point);
            DebugPanel.StartLayout();
            DebugPanel.Init();
#endif
        }


        protected override void OnFillPropertiesPanel(MarkingEnterPoint point)
        {
            AddOffset(point);
            AddSplit(point);
            AddShift(point);
        }
        private void FillTemplatePanel(MarkingEnterPoint point)
        {
            AddRoad(point);
            AddTemplate(point);
        }
#if DEBUG
        private void FillDebugPanel(MarkingEnterPoint point)
        {
            var position = ComponentPool.Get<FloatPropertyPanel>(DebugPanel, "Position");
            position.Text = "Position";
            position.Format = NodeMarkup.Localize.NumberFormat_Meter;
            position.isEnabled = false;
            position.Init();
            position.Value = point.GetRelativePosition();

            var isInverted = ComponentPool.Get<StringPropertyPanel>(DebugPanel, "isInverted");
            isInverted.Text = "Is inverted";
            isInverted.isEnabled = false;
            isInverted.Init();
            isInverted.Value = point.Enter.IsLaneInvert.ToString();

            var location = ComponentPool.Get<StringPropertyPanel>(DebugPanel, "Location");
            location.Text = "Location";
            location.isEnabled = false;
            location.Init();
            location.Value = point.Source.Location.ToString();

            var networkType = ComponentPool.Get<StringPropertyPanel>(DebugPanel, "NetworkType");
            networkType.Text = "Network type";
            networkType.isEnabled = false;
            networkType.Init();
            networkType.Value = point.Source.NetworkType.ToString();

            if (point.Source is NetInfoPointSource source)
            {
                if (source.LeftLane != null)
                {
                    var index = ComponentPool.Get<StringPropertyPanel>(DebugPanel, "Left Index");
                    index.Text = "Left Index";
                    index.isEnabled = false;
                    index.Init();
                    index.Value = source.LeftLane.Index.ToString();

                    var id = ComponentPool.Get<StringPropertyPanel>(DebugPanel, "Left Id");
                    id.Text = "Left Id";
                    id.isEnabled = false;
                    id.Init();
                    id.Value = source.LeftLane.LaneId.ToString();

                    var pos = ComponentPool.Get<StringPropertyPanel>(DebugPanel, "Left Position");
                    pos.Text = "Left position";
                    pos.isEnabled = false;
                    pos.Init();
                    pos.Value = source.LeftLane.Position.ToString();

                    var width = ComponentPool.Get<StringPropertyPanel>(DebugPanel, "Left Width");
                    width.Text = "Left half width";
                    width.isEnabled = false;
                    width.Init();
                    width.Value = source.LeftLane.HalfWidth.ToString();
                }

                if (source.RightLane != null)
                {
                    var index = ComponentPool.Get<StringPropertyPanel>(DebugPanel, "Right Index");
                    index.Text = "Right Index";
                    index.isEnabled = false;
                    index.Init();
                    index.Value = source.RightLane.Index.ToString();

                    var id = ComponentPool.Get<StringPropertyPanel>(DebugPanel, "Right Id");
                    id.Text = "Right Id";
                    id.isEnabled = false;
                    id.Init();
                    id.Value = source.RightLane.LaneId.ToString();

                    var pos = ComponentPool.Get<StringPropertyPanel>(DebugPanel, "Right Position");
                    pos.Text = "Right position";
                    pos.isEnabled = false;
                    pos.Init();
                    pos.Value = source.RightLane.Position.ToString();

                    var width = ComponentPool.Get<StringPropertyPanel>(DebugPanel, "Right Width");
                    width.Text = "Right half width";
                    width.isEnabled = false;
                    width.Init();
                    width.Value = source.RightLane.HalfWidth.ToString();
                }
            }
        }
#endif
        private void AddOffset(MarkingEnterPoint point)
        {
            Offset = ComponentPool.Get<FloatPropertyPanel>(PropertiesPanel, nameof(Offset));
            Offset.Text = NodeMarkup.Localize.PointEditor_Offset;
            Offset.Format = NodeMarkup.Localize.NumberFormat_Meter;
            Offset.UseWheel = true;
            Offset.WheelStep = 0.1f;
            Offset.WheelTip = Settings.ShowToolTip;
            Offset.Init();
            Offset.Value = point.Offset;
            Offset.OnValueChanged += OffsetChanged;
        }
        private void AddSplit(MarkingEnterPoint point)
        {
            Split = ComponentPool.Get<BoolListPropertyPanel>(PropertiesPanel, nameof(Split));
            Split.Text = NodeMarkup.Localize.PointEditor_SplitIntoTwo;
            Split.Init(NodeMarkup.Localize.StyleOption_No, NodeMarkup.Localize.StyleOption_Yes);
            Split.SelectedObject = point.Split;
            Split.OnSelectObjectChanged += SplitChanged;
        }
        private void AddShift(MarkingEnterPoint point)
        {
            Shift = ComponentPool.Get<FloatPropertyPanel>(PropertiesPanel, nameof(Shift));
            Shift.Text = NodeMarkup.Localize.PointEditor_SplitOffset;
            Shift.Format = NodeMarkup.Localize.NumberFormat_Meter;
            Shift.UseWheel = true;
            Shift.WheelStep = 0.1f;
            Shift.WheelTip = Settings.ShowToolTip;
            Shift.CheckMin = true;
            Shift.MinValue = 0f;
            Shift.Init();
            Shift.Value = point.SplitOffset;
            Shift.OnValueChanged += (value) => point.SplitOffset.Value = value;
            Shift.isVisible = point.Split;
        }
        private void AddRoad(MarkingEnterPoint point)
        {
            var roadNameProperty = ComponentPool.Get<StringPropertyPanel>(TemplatePanel, "Road");
            roadNameProperty.Text = NodeMarkup.Localize.PointEditor_RoadName;
            roadNameProperty.FieldWidth = 230;
            roadNameProperty.EnableControl = false;
            roadNameProperty.Init();
            roadNameProperty.Value = point.Enter.GetSegment().Info.name;
        }
        private void AddTemplate(MarkingEnterPoint point)
        {
            var buttonsPanel = ComponentPool.Get<ButtonsPanel>(TemplatePanel, "Buttons");
            var saveIndex = buttonsPanel.AddButton(NodeMarkup.Localize.PointEditor_SaveOffsets);
            var revertIndex = buttonsPanel.AddButton(NodeMarkup.Localize.PointEditor_RevertOffsets);
            buttonsPanel.Init();

            SetEnable();

            buttonsPanel.OnButtonClick += OnButtonClick;

            void OnButtonClick(int index)
            {
                if (index == saveIndex)
                {
                    var invert = point.Enter.IsLaneInvert;
                    var offsets = point.Enter.EnterPoints.Select(p => invert ? -p.Offset : p.Offset);
                    if (invert)
                        offsets = offsets.Reverse();

                    SingletonManager<RoadTemplateManager>.Instance.SaveOffsets(point.Enter.RoadName, offsets.ToArray());
                    SetEnable();
                }
                else if (index == revertIndex)
                {
                    SingletonManager<RoadTemplateManager>.Instance.RevertOffsets(point.Enter.RoadName);
                    SetEnable();
                }
            }
            void SetEnable()
            {
                buttonsPanel[revertIndex].isEnabled = SingletonManager<RoadTemplateManager>.Instance.ContainsOffset(point.Enter.RoadName);
            }
        }

        protected override void OnClear()
        {
            base.OnClear();
            Offset = null;
            Split = null;
            Shift = null;

            TemplatePanel = null;
#if DEBUG
            DebugPanel = null;
#endif
        }
        protected override void OnObjectUpdate(MarkingEnterPoint editObject)
        {
            Offset.OnValueChanged -= OffsetChanged;
            Offset.Value = EditObject.Offset;
            Offset.OnValueChanged += OffsetChanged;
        }

        private void OffsetChanged(float value)
        {
            EditObject.Offset.Value = value;
#if DEBUG
            if(DebugPanel.Find<FloatPropertyPanel>("Position") is FloatPropertyPanel position)
                position.Value = EditObject.GetRelativePosition();
#endif
        }

        private void SplitChanged(bool value)
        {
            EditObject.Split.Value = value;
            Shift.isVisible = value;
        }

        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            ItemsPanel.HoverGroupObject?.Render(new OverlayData(cameraInfo) { Color = Colors.White, Width = 2f });
            ItemsPanel.HoverObject?.Render(new OverlayData(cameraInfo) { Color = Colors.White, Width = 2f });
        }
    }
    public class PointsItemsPanel : ItemsGroupPanel<PointItem, MarkingEnterPoint, PointGroup, Entrance>
    {
        public override bool GroupingEnable => Settings.GroupPoints.value;

        public override int Compare(MarkingEnterPoint x, MarkingEnterPoint y) => 0;

        public override int Compare(Entrance x, Entrance y) => 0;

        protected override string GroupName(Entrance group) => group.ToString();

        protected override Entrance SelectGroup(MarkingEnterPoint point) => point.Enter;
    }
    public class PointItem : EditItem<MarkingEnterPoint, ColorIcon>
    {
        public override bool ShowDelete => false;
        public override void Init(Editor editor, MarkingEnterPoint editObject)
        {
            base.Init(editor, editObject);
            Icon.InnerColor = Object.Color;
        }
    }
    public class PointGroup : EditGroup<Entrance, PointItem, MarkingEnterPoint> { }
}
