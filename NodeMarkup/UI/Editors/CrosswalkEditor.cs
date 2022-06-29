﻿using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class CrosswalksEditor : SimpleEditor<CrosswalkItemsPanel, MarkupCrosswalk>
    {
        #region PROPERTIES

        public override string Name => NodeMarkup.Localize.CrosswalkEditor_Crosswalks;
        public override string EmptyMessage => string.Format(NodeMarkup.Localize.CrosswalkEditor_EmptyMessage, LocalizeExtension.Shift);
        public override Markup.SupportType Support { get; } = Markup.SupportType.Croswalks;

        private List<EditorItem> StyleProperties { get; set; } = new List<EditorItem>();
        private CrosswalkBorderSelectPropertyPanel RightBorder { get; set; }
        private CrosswalkBorderSelectPropertyPanel LeftBorder { get; set; }
        private WarningTextProperty Warning { get; set; }
        private StylePropertyPanel Style { get; set; }
        private CrosswalkBorderToolMode CrosswalkBorderToolMode { get; }

        public CrosswalkBorderSelectPropertyPanel HoverBorderPanel { get; private set; }

        #endregion

        #region BASIC

        public CrosswalksEditor()
        {
            CrosswalkBorderToolMode = Tool.CreateToolMode<CrosswalkBorderToolMode>();
            CrosswalkBorderToolMode.Init(this);
        }

        protected override IEnumerable<MarkupCrosswalk> GetObjects() => Markup.Crosswalks;

        protected override void OnFillPropertiesPanel(MarkupCrosswalk crosswalk)
        {
            AddHeader();
            AddWarning();

            AddBordersProperties();
            AddStyleTypeProperty();
            AddStyleProperties();

            FillBorders();
        }
        protected override void OnObjectDelete(MarkupCrosswalk crosswalk)
        {
            Panel.DeleteLine(crosswalk.CrosswalkLine);
            Markup.RemoveCrosswalk(crosswalk);
            base.OnObjectDelete(crosswalk);
        }
        protected override void OnClear()
        {
            base.OnClear();

            RightBorder = null;
            LeftBorder = null;
            Warning = null;
            Style = null;

            StyleProperties.Clear();
        }
        protected override void OnObjectUpdate(MarkupCrosswalk editObject) => FillBorders();

        #endregion

        #region PROPERTIES PANELS

        private void AddHeader()
        {
            var header = ComponentPool.Get<CrosswalkHeaderPanel>(PropertiesPanel, "Header");
            header.Init(EditObject.Style.Value.Type, SelectTemplate, false);
            header.OnSaveTemplate += SaveTemplate;
            header.OnCopy += CopyStyle;
            header.OnPaste += PasteStyle;
            header.OnCut += CutLines;
        }
        private void AddWarning()
        {
            Warning = ComponentPool.Get<WarningTextProperty>(PropertiesPanel, nameof(Warning));
            Warning.Text = NodeMarkup.Localize.CrosswalkEditor_BordersWarning;
            Warning.Init();
        }
        private void AddBordersProperties()
        {
            LeftBorder = AddBorderProperty(BorderPosition.Left, nameof(LeftBorder), NodeMarkup.Localize.CrosswalkEditor_LeftBorder);
            RightBorder = AddBorderProperty(BorderPosition.Right, nameof(RightBorder), NodeMarkup.Localize.CrosswalkEditor_RightBorder);

            FillBorders();
        }
        private void FillBorders()
        {
            FillBorder(LeftBorder, LeftBorgerChanged, GetBorderLines(BorderPosition.Left), EditObject.LeftBorder);
            FillBorder(RightBorder, RightBorgerChanged, GetBorderLines(BorderPosition.Right), EditObject.RightBorder);

            Warning.isVisible = Settings.ShowPanelTip && (!LeftBorder.EnableControl || !RightBorder.EnableControl);
        }
        private MarkupRegularLine[] GetBorderLines(BorderPosition border)
        {
            var point = border == BorderPosition.Right ? EditObject.CrosswalkLine.Start : EditObject.CrosswalkLine.End;
            if (point.Enter.TryGetPoint(point.Num, MarkupPoint.PointType.Enter, out MarkupPoint enterPoint))
                return enterPoint.Markup.GetPointLines(enterPoint).OfType<MarkupRegularLine>().ToArray();
            else
                return new MarkupRegularLine[0];
        }
        private void FillBorder(CrosswalkBorderSelectPropertyPanel panel, Action<MarkupRegularLine> action, MarkupRegularLine[] lines, MarkupRegularLine value)
        {
            panel.OnValueChanged -= action;
            panel.Clear();
            panel.AddRange(lines);
            panel.Value = value;

            if (Settings.ShowPanelTip)
            {
                panel.isVisible = true;
                panel.EnableControl = lines.Any();
            }
            else
            {
                panel.EnableControl = true;
                panel.isVisible = lines.Any();
            }

            panel.OnValueChanged += action;
        }
        private CrosswalkBorderSelectPropertyPanel AddBorderProperty(BorderPosition position, string name, string text)
        {
            var border = ComponentPool.Get<CrosswalkBorderSelectPropertyPanel>(PropertiesPanel, name);
            border.Text = text;
            border.Position = position;
            border.Init();
            border.OnSelect += (panel) => SelectBorder(panel);
            border.OnReset += (panel) => Tool.SetDefaultMode();
            border.OnEnter += HoverBorder;
            border.OnLeave += LeaveBorder;
            return border;
        }

        private void RightBorgerChanged(MarkupRegularLine line) => EditObject.RightBorder.Value = line;
        private void LeftBorgerChanged(MarkupRegularLine line) => EditObject.LeftBorder.Value = line;

        private void AddStyleTypeProperty()
        {
            Style = ComponentPool.Get<CrosswalkPropertyPanel>(PropertiesPanel, nameof(Style));
            Style.Text = NodeMarkup.Localize.Editor_Style;
            Style.Init();
            Style.UseWheel = true;
            Style.WheelTip = true;
            Style.SelectedObject = EditObject.Style.Value.Type;
            Style.OnSelectObjectChanged += StyleChanged;
        }
        private void AddStyleProperties()
        {
            StyleProperties = EditObject.Style.Value.GetUIComponents(EditObject, PropertiesPanel);
            if (StyleProperties.OfType<ColorPropertyPanel>().FirstOrDefault() is ColorPropertyPanel colorProperty)
                colorProperty.OnValueChanged += (Color32 c) => RefreshSelectedItem();
        }
        private void ClearStyleProperties()
        {
            foreach (var property in StyleProperties)
                ComponentPool.Free(property);

            StyleProperties.Clear();
        }

        #endregion

        #region STYLE CHANGE

        private void StyleChanged(Style.StyleType style)
        {
            if (style == EditObject.Style.Value.Type)
                return;

            var newStyle = SingletonManager<StyleTemplateManager>.Instance.GetDefault<CrosswalkStyle>(style);
            EditObject.Style.Value.CopyTo(newStyle);
            EditObject.Style.Value = newStyle;

            AfterStyleChanged();
        }
        private void AfterStyleChanged()
        {
            RefreshSelectedItem();
            PropertiesPanel.StopLayout();
            ClearStyleProperties();
            AddStyleProperties();
            PropertiesPanel.StartLayout();
        }
        private void ApplyStyle(CrosswalkStyle style)
        {
            EditObject.Style.Value = style.CopyStyle();
            Style.SelectedObject = EditObject.Style.Value.Type;

            AfterStyleChanged();
        }

        #endregion

        #region HANDLERS

        private void SaveTemplate()
        {
            if (SingletonManager<StyleTemplateManager>.Instance.AddTemplate(EditObject.Style, out StyleTemplate template))
                Panel.EditStyleTemplate(template);
        }
        private void SelectTemplate(StyleTemplate template)
        {
            if (template.Style is CrosswalkStyle style)
                ApplyStyle(style);
        }
        private void CopyStyle() => Tool.ToStyleBuffer(Manager.Style.StyleType.Crosswalk, EditObject.Style.Value);
        private void PasteStyle()
        {
            if (Tool.FromStyleBuffer<CrosswalkStyle>(Manager.Style.StyleType.Crosswalk, out var style))
                ApplyStyle(style);
        }
        private void CutLines() => Markup.CutLinesByCrosswalk(EditObject);

        public void HoverBorder(CrosswalkBorderSelectPropertyPanel selectPanel) => HoverBorderPanel = selectPanel;
        public void LeaveBorder(CrosswalkBorderSelectPropertyPanel selectPanel) => HoverBorderPanel = null;

        public bool SelectBorder(CrosswalkBorderSelectPropertyPanel selectPanel) => SelectBorder(selectPanel, null);
        public bool SelectBorder(CrosswalkBorderSelectPropertyPanel selectPanel, Func<Event, bool> afterAction)
        {
            if (Tool.Mode == CrosswalkBorderToolMode && selectPanel == CrosswalkBorderToolMode.SelectPanel)
            {
                Tool.SetDefaultMode();
                return true;
            }
            else
            {
                Tool.SetMode(CrosswalkBorderToolMode);
                CrosswalkBorderToolMode.SelectPanel = selectPanel;
                CrosswalkBorderToolMode.AfterSelectPanel = afterAction;
                selectPanel.Focus();
                return false;
            }
        }
        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            ItemsPanel.HoverObject?.Render(new OverlayData(cameraInfo) { Color = Colors.Hover });
            HoverBorderPanel?.Value?.Render(new OverlayData(cameraInfo) { Color = Colors.Hover });
        }

        public void BorderSetup()
        {
            if (!Settings.QuickBorderSetup)
                return;

            var hasLeft = LeftBorder.Objects.Any();
            var hasRight = RightBorder.Objects.Any();

            if (hasLeft)
                SelectBorder(LeftBorder, hasRight ? (_) => SelectBorder(RightBorder) : (Func<Event, bool>)null);
            else if (hasRight)
                SelectBorder(RightBorder);
        }

        #endregion
    }
    public class CrosswalkItemsPanel : ItemsPanel<CrosswalkItem, MarkupCrosswalk>
    {
        public override int Compare(MarkupCrosswalk x, MarkupCrosswalk y)
        {
            int result;
            if ((result = x.CrosswalkLine.Start.Enter.CompareTo(y.CrosswalkLine.Start.Enter)) == 0)
                result = x.CrosswalkLine.Start.Num.CompareTo(y.CrosswalkLine.Start.Num);
            return result;
        }
    }
    public class CrosswalkItem : EditItem<MarkupCrosswalk, StyleIcon>
    {
        public override void Refresh()
        {
            base.Refresh();

            Icon.Type = Object.Style.Value.Type;
            Icon.StyleColor = Object.Style.Value.Color;
        }
    }

    public class CrosswalkBorderToolMode : BasePanelMode<CrosswalksEditor, CrosswalkBorderSelectPropertyPanel, MarkupRegularLine>
    {
        protected override bool IsHover => LineSelector.IsHoverLine;
        protected override MarkupRegularLine Hover => LineSelector.HoverLine?.Line;

        private LinesSelector<MarkupLineBound> LineSelector { get; set; }

        protected override void OnSetPanel()
        {
            var color = SelectPanel.Position == BorderPosition.Left ? Colors.Green : Colors.Red;
            LineSelector = new LinesSelector<MarkupLineBound>(SelectPanel.Objects.Select(i => new MarkupLineBound(i, 0.5f)).ToArray(), color);
        }

        public override void OnToolUpdate() => LineSelector.OnUpdate();
        public override string GetToolInfo()
        {
            return SelectPanel.Position switch
            {
                BorderPosition.Right => Localize.CrosswalkEditor_InfoSelectRightBorder,
                BorderPosition.Left => Localize.CrosswalkEditor_InfoSelectLeftBorder,
                _ => null,
            };
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            LineSelector.Render(cameraInfo);
        }
    }
}
