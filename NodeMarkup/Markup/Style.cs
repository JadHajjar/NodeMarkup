﻿using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface IStyle { }
    public interface IColorStyle : IStyle
    {
        PropertyColorValue Color { get; }
    }
    public interface IWidthStyle : IStyle
    {
        PropertyStructValue<float> Width { get; }
    }
    public abstract class Style : IToXml
    {
        public static float DefaultDashLength => 1.5f;
        public static float DefaultSpaceLength => 1.5f;

        protected static string Length => string.Empty;
        protected static string Offset => string.Empty;

        public static bool FromXml<T>(XElement config, ObjectsMap map, bool invert, bool typeChanged, out T style) where T : Style
        {
            var type = IntToType(config.GetAttrValue<int>("T"));

            if (SingletonManager<StyleTemplateManager>.Instance.GetDefault<T>(type) is T defaultStyle)
            {
                style = defaultStyle;
                style.FromXml(config, map, invert, typeChanged);
                return true;
            }
            else
            {
                style = default;
                return false;
            }
        }
        private static StyleType IntToType(int rawType)
        {
            var typeGroup = rawType & (int)StyleType.GroupMask;
            var typeNum = (rawType & (int)StyleType.ItemMask) + 1;
            var type = (StyleType)((typeGroup == 0 ? (int)StyleType.RegularLine : typeGroup << 1) + typeNum);
            return type;
        }
        private static int TypeToInt(StyleType type)
        {
            var typeGroup = (int)type & (int)StyleType.GroupMask;
            var typeNum = ((int)type & (int)StyleType.ItemMask) - 1;
            var rawType = ((typeGroup >> 1) & (int)StyleType.GroupMask) + typeNum;
            return rawType;
        }

        public static Color32 DefaultColor { get; } = new Color32(136, 136, 136, 224);
        public static float DefaultWidth { get; } = 0.15f;

        protected virtual float WidthWheelStep { get; } = 0.01f;
        protected virtual float WidthMinValue { get; } = 0.05f;

        protected abstract Style GetDefault();
        public static T GetDefault<T>(StyleType type) where T : Style
        {
            return type.GetGroup() switch
            {
                StyleType.RegularLine => GetDefault(RegularLineStyle.Defaults, type.ToEnum<RegularLineStyle.RegularLineType, StyleType>()) as T,
                StyleType.StopLine => GetDefault(StopLineStyle.Defaults, type.ToEnum<StopLineStyle.StopLineType, StyleType>()) as T,
                StyleType.Filler => GetDefault(FillerStyle.Defaults, type.ToEnum<FillerStyle.FillerType, StyleType>()) as T,
                StyleType.Crosswalk => GetDefault(CrosswalkStyle.Defaults, type.ToEnum<CrosswalkStyle.CrosswalkType, StyleType>()) as T,
                _ => null,
            };
        }
        private static TypeStyle GetDefault<TypeEnum, TypeStyle>(Dictionary<TypeEnum, TypeStyle> dic, TypeEnum type)
            where TypeEnum : Enum
            where TypeStyle : Style
        {
            return dic.TryGetValue(type, out var style) ? (TypeStyle)style.Copy() : null;
        }

        public static string XmlName { get; } = "S";

        public Action OnStyleChanged { private get; set; }
        public string XmlSection => XmlName;
        public abstract StyleType Type { get; }
        public abstract MarkupLOD SupportLOD { get; }

        protected virtual void StyleChanged() => OnStyleChanged?.Invoke();

        public PropertyColorValue Color { get; }
        public PropertyStructValue<float> Width { get; }

        public abstract Dictionary<string, int> PropertyIndices { get; }
        protected static Dictionary<string, int> CreatePropertyIndices(IEnumerable<string> names)
        {
            var dic = new Dictionary<string, int>();
            foreach(var name in names)
            {
                dic[name] = dic.Count;
            }
            return dic;
        }

        public Style(Color32 color, float width)
        {
            Color = GetColorProperty(color);
            Width = GetWidthProperty(width);
        }
        protected XElement BaseToXml() => new XElement(XmlSection, new XAttribute("T", TypeToInt(Type)));
        public virtual XElement ToXml()
        {
            var config = BaseToXml();
            Color.ToXml(config);
            Width.ToXml(config);
            return config;
        }
        public virtual void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            Color.FromXml(config, DefaultColor);
            Width.FromXml(config, DefaultWidth);
        }

        public abstract Style Copy();
        protected void CopyTo(Style target)
        {
            if (this is IWidthStyle widthSource && target is IWidthStyle widthTarget)
                widthTarget.Width.Value = widthSource.Width;
            if (this is IColorStyle colorSource && target is IColorStyle colorTarget)
                colorTarget.Color.Value = colorSource.Color;
        }

        public virtual List<EditorItem> GetUIComponents(object editObject, UIComponent parent, bool isTemplate = false)
        {
            var components = new List<EditorItem>();

            if (this is IColorStyle)
                components.Add(AddColorProperty(parent, false));
            if (this is IWidthStyle)
                components.Add(AddWidthProperty(parent, false));

            return components;
        }
        public int GetUIComponentSortIndex(EditorItem item)
        {
            if(PropertyIndices.TryGetValue(item.name, out var index))
                return index;
            else
                return int.MaxValue;
        }
        private ColorAdvancedPropertyPanel AddColorProperty(UIComponent parent, bool canCollapse)
        {
            var colorProperty = ComponentPool.Get<ColorAdvancedPropertyPanel>(parent, nameof(Color));
            colorProperty.Text = Localize.StyleOption_Color;
            colorProperty.WheelTip = Settings.ShowToolTip;
            colorProperty.CanCollapse = canCollapse;
            colorProperty.Init(GetDefault()?.Color);
            colorProperty.Value = Color;
            colorProperty.OnValueChanged += (Color32 color) => Color.Value = color;

            return colorProperty;
        }
        private FloatPropertyPanel AddWidthProperty(UIComponent parent, bool canCollapse)
        {
            var widthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Width));
            widthProperty.Text = Localize.StyleOption_Width;
            widthProperty.Format = Localize.NumberFormat_Meter;
            widthProperty.UseWheel = true;
            widthProperty.WheelStep = WidthWheelStep;
            widthProperty.WheelTip = Settings.ShowToolTip;
            widthProperty.CheckMin = true;
            widthProperty.MinValue = WidthMinValue;
            widthProperty.CanCollapse = canCollapse;
            widthProperty.Init();
            widthProperty.Value = Width;
            widthProperty.OnValueChanged += (float value) => Width.Value = value;

            return widthProperty;
        }
        protected Vector2PropertyPanel AddLengthProperty(IDashedLine dashedStyle, UIComponent parent, bool canCollapse)
        {
            var lengthProperty = ComponentPool.Get<Vector2PropertyPanel>(parent, nameof(Length));
            lengthProperty.Text = Localize.StyleOption_Length;
            lengthProperty.FieldsWidth = 50f;
            lengthProperty.SetLabels(Localize.StyleOption_Dash, Localize.StyleOption_Space);
            lengthProperty.Format = Localize.NumberFormat_Meter;
            lengthProperty.UseWheel = true;
            lengthProperty.WheelStep = new Vector2(0.1f, 0.1f);
            lengthProperty.WheelTip = Settings.ShowToolTip;
            lengthProperty.CheckMin = true;
            lengthProperty.MinValue = new Vector2(0.1f, 0.1f);
            lengthProperty.CanCollapse = canCollapse;
            lengthProperty.Init(0, 1);
            lengthProperty.Value = new Vector2(dashedStyle.DashLength, dashedStyle.SpaceLength);
            lengthProperty.OnValueChanged += (Vector2 value) =>
            {
                dashedStyle.DashLength.Value = value.x;
                dashedStyle.SpaceLength.Value = value.y;
            };

            return lengthProperty;
        }
        protected FloatPropertyPanel AddDashLengthProperty(IDashedLine dashedStyle, UIComponent parent, bool canCollapse)
        {
            var dashLengthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(dashedStyle.DashLength));
            dashLengthProperty.Text = Localize.StyleOption_DashedLength;
            dashLengthProperty.Format = Localize.NumberFormat_Meter;
            dashLengthProperty.UseWheel = true;
            dashLengthProperty.WheelStep = 0.1f;
            dashLengthProperty.WheelTip = Settings.ShowToolTip;
            dashLengthProperty.CheckMin = true;
            dashLengthProperty.MinValue = 0.1f;
            dashLengthProperty.CanCollapse = canCollapse;
            dashLengthProperty.Init();
            dashLengthProperty.Value = dashedStyle.DashLength;
            dashLengthProperty.OnValueChanged += (float value) => dashedStyle.DashLength.Value = value;

            return dashLengthProperty;
        }
        protected FloatPropertyPanel AddSpaceLengthProperty(IDashedLine dashedStyle, UIComponent parent, bool canCollapse)
        {
            var spaceLengthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(dashedStyle.SpaceLength));
            spaceLengthProperty.Text = Localize.StyleOption_SpaceLength;
            spaceLengthProperty.Format = Localize.NumberFormat_Meter;
            spaceLengthProperty.UseWheel = true;
            spaceLengthProperty.WheelStep = 0.1f;
            spaceLengthProperty.WheelTip = Settings.ShowToolTip;
            spaceLengthProperty.CheckMin = true;
            spaceLengthProperty.MinValue = 0.1f;
            spaceLengthProperty.CanCollapse = canCollapse;
            spaceLengthProperty.Init();
            spaceLengthProperty.Value = dashedStyle.SpaceLength;
            spaceLengthProperty.OnValueChanged += (float value) => dashedStyle.SpaceLength.Value = value;

            return spaceLengthProperty;
        }
        protected ButtonPanel AddInvertProperty(IAsymLine asymStyle, UIComponent parent, bool canCollapse)
        {
            var buttonsPanel = ComponentPool.Get<ButtonPanel>(parent, nameof(asymStyle.Invert));
            buttonsPanel.Text = Localize.StyleOption_Invert;
            buttonsPanel.CanCollapse = canCollapse;
            buttonsPanel.Init();
            buttonsPanel.OnButtonClick += OnButtonClick;

            void OnButtonClick() => asymStyle.Invert.Value = !asymStyle.Invert;

            return buttonsPanel;
        }

        protected enum PropertyNames
        {
            C, //Color
            SC, //SecondColor
            W, //Width
            O, //Offset
            MO, //MedianOffset
            A, //Alignment, Angle
            DL, //Dash length
            SL, //Space length
            I, //Invert
            CS, //Solid in center
            B, //Base
            H, //Height
            S, //Space
            OB, //OffsetBefore
            OA, //OffsetAfter
            LW, //Crosswalk line width
            P, //Parallel
            USC, //Use second color
            UG, //Use gap
            GL, //Gap length
            GP, //Gap period
            SS, //Square side
            LC, //Line count
            
        }

        protected PropertyColorValue GetColorProperty(Color32 defaultValue) => new PropertyColorValue("C", StyleChanged, defaultValue);
        protected PropertyColorValue GetSecondColorProperty(Color32 defaultValue) => new PropertyColorValue("SC", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetWidthProperty(float defaultValue) => new PropertyStructValue<float>("W", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetLineOffsetProperty(float defaultValue) => new PropertyStructValue<float>("O", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetOffsetProperty(float defaultValue) => new PropertyStructValue<float>("O", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetMedianOffsetProperty(float defaultValue) => new PropertyStructValue<float>("MO", StyleChanged, defaultValue);
        protected PropertyEnumValue<Alignment> GetAlignmentProperty(Alignment defaultValue) => new PropertyEnumValue<Alignment>("A", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetDashLengthProperty(float defaultValue) => new PropertyStructValue<float>("DL", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetSpaceLengthProperty(float defaultValue) => new PropertyStructValue<float>("SL", StyleChanged, defaultValue);
        protected PropertyBoolValue GetInvertProperty(bool defaultValue) => new PropertyBoolValue("I", StyleChanged, defaultValue);
        protected PropertyBoolValue GetCenterSolidProperty(bool defaultValue) => new PropertyBoolValue("CS", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetBaseProperty(float defaultValue) => new PropertyStructValue<float>("B", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetHeightProperty(float defaultValue) => new PropertyStructValue<float>("H", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetSpaceProperty(float defaultValue) => new PropertyStructValue<float>("S", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetOffsetBeforeProperty(float defaultValue) => new PropertyStructValue<float>("OB", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetOffsetAfterProperty(float defaultValue) => new PropertyStructValue<float>("OA", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetLineWidthProperty(float defaultValue) => new PropertyStructValue<float>("LW", StyleChanged, defaultValue);
        protected PropertyBoolValue GetParallelProperty(bool defaultValue) => new PropertyBoolValue("P", StyleChanged, defaultValue);
        protected PropertyBoolValue GetUseSecondColorProperty(bool defaultValue) => new PropertyBoolValue("USC", StyleChanged, defaultValue);
        protected PropertyBoolValue GetUseGapProperty(bool defaultValue) => new PropertyBoolValue("UG", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetGapLengthProperty(float defaultValue) => new PropertyStructValue<float>("GL", StyleChanged, defaultValue);
        protected PropertyStructValue<int> GetGapPeriodProperty(int defaultValue) => new PropertyStructValue<int>("GP", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetSquareSideProperty(float defaultValue) => new PropertyStructValue<float>("SS", StyleChanged, defaultValue);
        protected PropertyStructValue<int> GetLineCountProperty(int defaultValue) => new PropertyStructValue<int>("LC", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetAngleProperty(float defaultValue) => new PropertyStructValue<float>("A", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetStepProperty(float defaultValue) => new PropertyStructValue<float>("S", StyleChanged, defaultValue);
        protected PropertyStructValue<int> GetOutputProperty(int defaultValue) => new PropertyStructValue<int>("O", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetAngleBetweenProperty(float defaultValue) => new PropertyStructValue<float>("A", StyleChanged, defaultValue);
        protected PropertyEnumValue<ChevronFillerStyle.From> GetStartingFromProperty(ChevronFillerStyle.From defaultValue) => new PropertyEnumValue<ChevronFillerStyle.From>("SF", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetElevationProperty(float defaultValue) => new PropertyStructValue<float>("E", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetCornerRadiusProperty(float defaultValue) => new PropertyStructValue<float>("CR", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetMedianCornerRadiusProperty(float defaultValue) => new PropertyStructValue<float>("MCR", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetCurbSizeProperty(float defaultValue) => new PropertyStructValue<float>("CS", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetMedianCurbSizeProperty(float defaultValue) => new PropertyStructValue<float>("MCS", StyleChanged, defaultValue);
        protected PropertyStructValue<int> GetLeftRailAProperty(int defaultValue) => new PropertyStructValue<int>("LRA", StyleChanged, defaultValue);
        protected PropertyStructValue<int> GetLeftRailBProperty(int defaultValue) => new PropertyStructValue<int>("LRB", StyleChanged, defaultValue);
        protected PropertyStructValue<int> GetRightRailAProperty(int defaultValue) => new PropertyStructValue<int>("RRA", StyleChanged, defaultValue);
        protected PropertyStructValue<int> GetRightRailBProperty(int defaultValue) => new PropertyStructValue<int>("RRB", StyleChanged, defaultValue);
        protected PropertyBoolValue GetFollowRailsProperty(bool defaultValue) => new PropertyBoolValue("FR", StyleChanged, defaultValue);

        public enum StyleType
        {
            [NotItem]
            ItemMask = 0xFF,
            [NotItem]
            GroupMask = ~ItemMask,

            #region REGULAR

            [NotItem]
            [Description(nameof(Localize.LineStyle_RegularLinesGroup))]
            RegularLine = Markup.Item.RegularLine,

            [Description(nameof(Localize.LineStyle_Solid))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            LineSolid,

            [Description(nameof(Localize.LineStyle_Dashed))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            LineDashed,

            [Description(nameof(Localize.LineStyle_DoubleSolid))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            LineDoubleSolid,

            [Description(nameof(Localize.LineStyle_DoubleDashed))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            LineDoubleDashed,

            [Description(nameof(Localize.LineStyle_SolidAndDashed))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            LineSolidAndDashed,

            [Description(nameof(Localize.LineStyle_SharkTeeth))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            LineSharkTeeth,

            [Description(nameof(Localize.LineStyle_DoubleDashedAsym))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            LineDoubleDashedAsym,

            [Description(nameof(Localize.LineStyle_ZigZag))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular)]
            LineZigZag,

            [NotItem]
            Regular3DLine = Markup.Item.RegularLine + 0x80,

            [Description(nameof(Localize.LineStyle_Pavement))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            LinePavement,

            [NotItem]
            RegularPropLine = Regular3DLine + 0x10,

            [Description(nameof(Localize.LineStyle_Prop))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            LineProp,

            [Description(nameof(Localize.LineStyle_Tree))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            LineTree,

            [Description(nameof(Localize.LineStyle_Text))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            LineText,


            [NotItem]
            RegularNetworkLine = Regular3DLine + 0x20,

            [Description(nameof(Localize.LineStyle_Network))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            LineNetwork,

            [Description(nameof(Localize.LineStyle_Empty))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            [NotVisible]
            EmptyLine = LineBuffer - 1,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            [NotVisible]
            LineBuffer = Markup.Item.RegularLine + 0x100 - 1,

            #endregion

            #region STOP

            [NotItem]
            [Description(nameof(Localize.LineStyle_StopLinesGroup))]
            [NetworkType(NetworkType.Road)]
            StopLine = Markup.Item.StopLine,

            [Description(nameof(Localize.LineStyle_StopSolid))]
            [NetworkType(NetworkType.Road)]
            StopLineSolid,

            [Description(nameof(Localize.LineStyle_StopDashed))]
            [NetworkType(NetworkType.Road)]
            StopLineDashed,

            [Description(nameof(Localize.LineStyle_StopDouble))]
            [NetworkType(NetworkType.Road)]
            StopLineDoubleSolid,

            [Description(nameof(Localize.LineStyle_StopDoubleDashed))]
            [NetworkType(NetworkType.Road)]
            StopLineDoubleDashed,

            [Description(nameof(Localize.LineStyle_StopSolidAndDashed))]
            [NetworkType(NetworkType.Road)]
            StopLineSolidAndDashed,

            [Description(nameof(Localize.LineStyle_StopSharkTeeth))]
            [NetworkType(NetworkType.Road)]
            StopLineSharkTeeth,

            [Description(nameof(Localize.LineStyle_StopPavement))]
            [NetworkType(NetworkType.Road)]
            StopLinePavement,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NetworkType(NetworkType.Road)]
            [NotVisible]
            StopLineBuffer = Markup.Item.StopLine + 0x100 - 1,

            #endregion

            #region FILLER

            [NotItem]
            [Description(nameof(Localize.FillerStyle_Group))]
            Filler = Markup.Item.Filler,

            [Description(nameof(Localize.FillerStyle_Stripe))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            FillerStripe,

            [Description(nameof(Localize.FillerStyle_Grid))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            FillerGrid,

            [Description(nameof(Localize.FillerStyle_Solid))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            FillerSolid,

            [Description(nameof(Localize.FillerStyle_Chevron))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            FillerChevron,

            [NotItem]
            Filler3D = Filler + 0x80,

            [Description(nameof(Localize.FillerStyle_Pavement))]
            [NetworkType(NetworkType.All)]
            FillerPavement,

            [Description(nameof(Localize.FillerStyle_Grass))]
            [NetworkType(NetworkType.All)]
            FillerGrass,

            [Description(nameof(Localize.FillerStyle_Gravel))]
            [NetworkType(NetworkType.All)]
            FillerGravel,

            [Description(nameof(Localize.FillerStyle_Ruined))]
            [NetworkType(NetworkType.All)]
            FillerRuined,

            [Description(nameof(Localize.FillerStyle_Cliff))]
            [NetworkType(NetworkType.All)]
            FillerCliff,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NetworkType(NetworkType.All)]
            [NotVisible]
            FillerBuffer = Markup.Item.Filler + 0x100 - 1,

            #endregion

            #region CROSSWALK

            [NotItem]
            [Description(nameof(Localize.CrosswalkStyle_Group))]
            Crosswalk = Markup.Item.Crosswalk,

            [Description(nameof(Localize.CrosswalkStyle_Existent))]
            [NetworkType(NetworkType.Road)]
            CrosswalkExistent,

            [Description(nameof(Localize.CrosswalkStyle_Zebra))]
            [NetworkType(NetworkType.Road)]
            CrosswalkZebra,

            [Description(nameof(Localize.CrosswalkStyle_DoubleZebra))]
            [NetworkType(NetworkType.Road)]
            CrosswalkDoubleZebra,

            [Description(nameof(Localize.CrosswalkStyle_ParallelSolidLines))]
            [NetworkType(NetworkType.Road)]
            CrosswalkParallelSolidLines,

            [Description(nameof(Localize.CrosswalkStyle_ParallelDashedLines))]
            [NetworkType(NetworkType.Road)]
            CrosswalkParallelDashedLines,

            [Description(nameof(Localize.CrosswalkStyle_Ladder))]
            [NetworkType(NetworkType.Road)]
            CrosswalkLadder,

            [Description(nameof(Localize.CrosswalkStyle_Solid))]
            [NetworkType(NetworkType.Road)]
            CrosswalkSolid,

            [Description(nameof(Localize.CrosswalkStyle_ChessBoard))]
            [NetworkType(NetworkType.Road)]
            CrosswalkChessBoard,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NetworkType(NetworkType.Road)]
            [NotVisible]
            CrosswalkBuffer = Markup.Item.Crosswalk + 0x100 - 1,

            #endregion
        }
    }
    public abstract class Style<StyleType> : Style
        where StyleType : Style<StyleType>
    {
        public Style(Color32 color, float width) : base(color, width) { }

        public virtual void CopyTo(StyleType target) => base.CopyTo(target);
        public sealed override Style Copy() => CopyStyle();
        public abstract StyleType CopyStyle();

        protected sealed override Style GetDefault() => GetDefaultStyle();
        protected StyleType GetDefaultStyle() => SingletonManager<StyleTemplateManager>.Instance.GetDefault<StyleType>(Type);
    }

}
