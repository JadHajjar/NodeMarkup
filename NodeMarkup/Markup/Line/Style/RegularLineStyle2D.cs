﻿using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class SolidLineStyle : RegularLineStyle, IRegularLine
    {
        public override StyleType Type => StyleType.LineSolid;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;

        public SolidLineStyle(Color32 color, float width) : base(color, width) { }

        public override RegularLineStyle CopyLineStyle() => new SolidLineStyle(Color, Width);

        protected override IStyleData CalculateImpl(MarkupRegularLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            var borders = line.Borders;
            return new MarkupPartGroupData(lod, StyleHelper.CalculateSolid(trajectory, lod, GetDashes));

            IEnumerable<MarkupPartData> GetDashes(ITrajectory trajectory) => CalculateDashes(trajectory, borders);
        }
        protected virtual IEnumerable<MarkupPartData> CalculateDashes(ITrajectory trajectory, LineBorders borders)
        {
            if (StyleHelper.CalculateSolidPart(borders, trajectory, 0f, Width, Color, out MarkupPartData dash))
                yield return dash;
        }
    }
    public class DoubleSolidLineStyle : SolidLineStyle, IRegularLine, IDoubleLine, IDoubleAlignmentLine
    {
        public override StyleType Type => StyleType.LineDoubleSolid;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyBoolValue ColorCount { get; }
        public PropertyColorValue SecondColor { get; }
        public new PropertyValue<float> Offset { get; }
        public PropertyEnumValue<Alignment> Alignment { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(ColorCount);
                yield return nameof(Color);
                yield return nameof(SecondColor);
                yield return nameof(Width);
                yield return nameof(Offset);
                yield return nameof(Alignment);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;

        public DoubleSolidLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, float offset) : base(color, width)
        {
            ColorCount = GetUseSecondColorProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(ColorCount ? secondColor : color);
            Offset = GetOffsetProperty(offset);
            Alignment = GetAlignmentProperty(Manager.Alignment.Centre);
        }

        public override RegularLineStyle CopyLineStyle() => new DoubleSolidLineStyle(Color, SecondColor, ColorCount, Width, Offset);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset.Value = Offset;
                doubleTarget.SecondColor.Value = SecondColor;
                doubleTarget.ColorCount.Value = ColorCount;
            }
            if (target is IDoubleAlignmentLine doubleAlignmentTarget)
                doubleAlignmentTarget.Alignment.Value = Alignment;
        }

        protected override IEnumerable<MarkupPartData> CalculateDashes(ITrajectory trajectory, LineBorders borders)
        {
            var firstOffset = Alignment.Value switch
            {
                Manager.Alignment.Left => 2 * Offset,
                Manager.Alignment.Centre => Offset,
                Manager.Alignment.Right => 0,
                _ => 0,
            };
            var secondOffset = Alignment.Value switch
            {
                Manager.Alignment.Left => 0,
                Manager.Alignment.Centre => -Offset,
                Manager.Alignment.Right => -2 * Offset,
                _ => 0,
            };

            if (StyleHelper.CalculateSolidPart(borders, trajectory, firstOffset, Width, Color, out MarkupPartData firstDash))
                yield return firstDash;

            if (StyleHelper.CalculateSolidPart(borders, trajectory, secondOffset, Width, ColorCount ? SecondColor : Color, out MarkupPartData secondDash))
                yield return secondDash;
        }
        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);

            components.Add(AddUseSecondColorProperty(this, parent, true));
            components.Add(AddSecondColorProperty(this, parent, true));
            UseSecondColorChanged(this, parent, ColorCount);

            components.Add(AddOffsetProperty(this, parent, false));
            if (!isTemplate)
                components.Add(AddAlignmentProperty(this, parent, false));
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            ColorCount.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            Alignment.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            ColorCount.FromXml(config, false);
            SecondColor.FromXml(config, DefaultColor);
            Offset.FromXml(config, DefaultDoubleOffset);
            Alignment.FromXml(config, Manager.Alignment.Centre);

            if (invert)
                Alignment.Value = Alignment.Value.Invert();
        }
    }
    public class DashedLineStyle : RegularLineStyle, IRegularLine, IDashedLine
    {
        public override StyleType Type => StyleType.LineDashed;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyValue<float> DashLength { get; }
        public PropertyValue<float> SpaceLength { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(Length);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;

        public DashedLineStyle(Color32 color, float width, float dashLength, float spaceLength) : base(color, width)
        {
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
        }

        public override RegularLineStyle CopyLineStyle() => new DashedLineStyle(Color, Width, DashLength, SpaceLength);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength.Value = DashLength;
                dashedTarget.SpaceLength.Value = SpaceLength;
            }
        }

        protected override IStyleData CalculateImpl(MarkupRegularLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            if (!CheckDashedLod(lod, Width, DashLength))
                return new MarkupPartGroupData(lod);

            var borders = line.Borders;
            return new MarkupPartGroupData(lod, StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, GetDashes));

            IEnumerable<MarkupPartData> GetDashes(ITrajectory trajectory, float startT, float endT)
                => CalculateDashes(trajectory, startT, endT, borders);
        }

        protected virtual IEnumerable<MarkupPartData> CalculateDashes(ITrajectory trajectory, float startT, float endT, LineBorders borders)
        {
            if (StyleHelper.CalculateDashedParts(borders, trajectory, startT, endT, DashLength, 0, Width, Color, out MarkupPartData dash))
                yield return dash;
        }

        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddLengthProperty(this, parent, false));
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            DashLength.ToXml(config);
            SpaceLength.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            DashLength.FromXml(config, DefaultDashLength);
            SpaceLength.FromXml(config, DefaultSpaceLength);
        }
    }
    public class DoubleDashedLineStyle : DashedLineStyle, IRegularLine, IDoubleLine, IDoubleAlignmentLine
    {
        public override StyleType Type => StyleType.LineDoubleDashed;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyBoolValue ColorCount { get; }
        public PropertyColorValue SecondColor { get; }
        public new PropertyValue<float> Offset { get; }
        public PropertyEnumValue<Alignment> Alignment { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(ColorCount);
                yield return nameof(Color);
                yield return nameof(SecondColor);
                yield return nameof(Width);
                yield return nameof(Length);
                yield return nameof(Offset);
                yield return nameof(Alignment);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;

        public DoubleDashedLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, float dashLength, float spaceLength, float offset) : base(color, width, dashLength, spaceLength)
        {
            ColorCount = GetUseSecondColorProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(ColorCount ? secondColor : color);
            Offset = GetOffsetProperty(offset);
            Alignment = GetAlignmentProperty(Manager.Alignment.Centre);
        }

        public override RegularLineStyle CopyLineStyle() => new DoubleDashedLineStyle(Color, SecondColor, ColorCount, Width, DashLength, SpaceLength, Offset);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset.Value = Offset;
                doubleTarget.SecondColor.Value = SecondColor;
                doubleTarget.ColorCount.Value = ColorCount;
            }
            if (target is IDoubleAlignmentLine doubleAlignmentTarget)
                doubleAlignmentTarget.Alignment.Value = Alignment;
        }

        protected override IEnumerable<MarkupPartData> CalculateDashes(ITrajectory trajectory, float startT, float endT, LineBorders borders)
        {
            var firstOffset = Alignment.Value switch
            {
                Manager.Alignment.Left => 2 * Offset,
                Manager.Alignment.Centre => Offset,
                Manager.Alignment.Right => 0,
                _ => 0,
            };
            var secondOffset = Alignment.Value switch
            {
                Manager.Alignment.Left => 0,
                Manager.Alignment.Centre => -Offset,
                Manager.Alignment.Right => -2 * Offset,
                _ => 0,
            };

            if (StyleHelper.CalculateDashedParts(borders, trajectory, startT, endT, DashLength, firstOffset, Width, Color, out MarkupPartData firstDash))
                yield return firstDash;

            if (StyleHelper.CalculateDashedParts(borders, trajectory, startT, endT, DashLength, secondOffset, Width, ColorCount ? SecondColor : Color, out MarkupPartData secondDash))
                yield return secondDash;
        }
        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddUseSecondColorProperty(this, parent, true));
            components.Add(AddSecondColorProperty(this, parent, true));
            components.Add(AddOffsetProperty(this, parent, false));
            if (!isTemplate)
                components.Add(AddAlignmentProperty(this, parent, false));

            UseSecondColorChanged(this, parent, ColorCount);
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            ColorCount.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            Alignment.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            ColorCount.FromXml(config, false);
            SecondColor.FromXml(config, DefaultColor);
            Offset.FromXml(config, DefaultDoubleOffset);
            Alignment.FromXml(config, Manager.Alignment.Centre);
            if (invert)
                Alignment.Value = Alignment.Value.Invert();
        }
    }
    public class DoubleDashedAsymLineStyle : RegularLineStyle, IRegularLine, IDashedLine, IDoubleLine, IDoubleAlignmentLine, IAsymLine
    {
        public override StyleType Type => StyleType.LineDoubleDashedAsym;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyValue<float> DashLengthA { get; }
        public PropertyValue<float> DashLengthB { get; }
        public PropertyValue<float> DashLength
        {
            get
            {
                PropertyStructValue<float> dashLength = null;
                dashLength = new PropertyStructValue<float>(() => DashLengthValue = dashLength.Value, DashLengthValue);

                return dashLength;
            }
        }
        public PropertyValue<float> SpaceLength { get; }

        public PropertyBoolValue ColorCount { get; }
        public PropertyColorValue SecondColor { get; }
        public new PropertyValue<float> Offset { get; }
        public PropertyEnumValue<Alignment> Alignment { get; }

        public PropertyBoolValue Invert { get; }

        private float DashLengthValue
        {
            get => Mathf.Max(DashLengthA, DashLengthB);
            set
            {
                DashLengthA.Value = value;
                DashLengthB.Value = value;
            }
        }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(ColorCount);
                yield return nameof(Color);
                yield return nameof(SecondColor);
                yield return nameof(Width);
                yield return nameof(SpaceLength);
                yield return nameof(DashLength);
                yield return nameof(Offset);
                yield return nameof(Alignment);
                yield return nameof(Invert);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;

        public DoubleDashedAsymLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, float dashLengthA, float dashLengthB, float spaceLength, float offset) : base(color, width)
        {
            DashLengthA = new PropertyStructValue<float>("DLA", StyleChanged, dashLengthA);
            DashLengthB = new PropertyStructValue<float>("DLB", StyleChanged, dashLengthB);
            SpaceLength = GetSpaceLengthProperty(spaceLength);

            Offset = GetOffsetProperty(offset);
            Alignment = GetAlignmentProperty(Manager.Alignment.Centre);
            ColorCount = GetUseSecondColorProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(ColorCount ? secondColor : color);

            Invert = GetInvertProperty(false);
        }

        public override RegularLineStyle CopyLineStyle() => new DoubleDashedAsymLineStyle(Color, SecondColor, ColorCount, Width, DashLengthB, DashLengthA, DashLengthB, Offset);

        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength.Value = DashLengthValue;
                dashedTarget.SpaceLength.Value = SpaceLength;
            }
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset.Value = Offset;
                doubleTarget.SecondColor.Value = SecondColor;
                doubleTarget.ColorCount.Value = ColorCount;
            }
            if (target is IDoubleAlignmentLine doubleAlignmentTarget)
                doubleAlignmentTarget.Alignment.Value = Alignment;
        }

        protected override IStyleData CalculateImpl(MarkupRegularLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            if (!CheckDashedLod(lod, Width, DashLengthValue))
                return new MarkupPartGroupData(lod);

            var borders = line.Borders;
            return new MarkupPartGroupData(lod, StyleHelper.CalculateDashed(trajectory, DashLengthValue, SpaceLength, GetDashes));

            IEnumerable<MarkupPartData> GetDashes(ITrajectory trajectory, float startT, float endT)
                => CalculateDashes(trajectory, startT, endT, borders);
        }

        protected IEnumerable<MarkupPartData> CalculateDashes(ITrajectory trajectory, float startT, float endT, LineBorders borders)
        {
            var offsetA = Alignment.Value switch
            {
                Manager.Alignment.Left => 2 * Offset,
                Manager.Alignment.Centre => Offset,
                Manager.Alignment.Right => 0,
                _ => 0,
            };
            var offsetB = Alignment.Value switch
            {
                Manager.Alignment.Left => 0,
                Manager.Alignment.Centre => -Offset,
                Manager.Alignment.Right => -2 * Offset,
                _ => 0,
            };

            if (Invert)
            {
                offsetA = -offsetA;
                offsetB = -offsetB;
            }

            if (StyleHelper.CalculateDashedParts(borders, trajectory, startT, endT, DashLengthA, offsetA, Width, Color, out MarkupPartData firstDash))
                yield return firstDash;

            if (StyleHelper.CalculateDashedParts(borders, trajectory, startT, endT, DashLengthB, offsetB, Width, ColorCount ? SecondColor : Color, out MarkupPartData secondDash))
                yield return secondDash;
        }

        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);

            components.Add(AddDashLengthProperty(parent, false));
            components.Add(AddSpaceLengthProperty(this, parent, false));

            components.Add(AddUseSecondColorProperty(this, parent, true));
            components.Add(AddSecondColorProperty(this, parent, true));
            components.Add(AddOffsetProperty(this, parent, false));
            if (!isTemplate)
            {
                components.Add(AddAlignmentProperty(this, parent, false));
                components.Add(AddInvertProperty(this, parent, false));
            }

            UseSecondColorChanged(this, parent, ColorCount);
        }
        protected FloatRangePropertyPanel AddDashLengthProperty(UIComponent parent, bool canCollapse)
        {
            var dashLengthProperty = ComponentPool.Get<FloatRangePropertyPanel>(parent, nameof(DashLength));
            dashLengthProperty.Text = Localize.StyleOption_DashedLength;
            dashLengthProperty.Format = Localize.NumberFormat_Meter;
            dashLengthProperty.UseWheel = true;
            dashLengthProperty.WheelStep = 0.1f;
            dashLengthProperty.WheelTip = Settings.ShowToolTip;
            dashLengthProperty.CheckMin = true;
            dashLengthProperty.MinValue = 0.1f;
            dashLengthProperty.CanCollapse = canCollapse;
            dashLengthProperty.FieldWidth = 70f;
            dashLengthProperty.Init();
            dashLengthProperty.SetValues(DashLengthA, DashLengthB);
            dashLengthProperty.OnValueChanged += (float valueA, float valueB) =>
                {
                    DashLengthA.Value = valueA;
                    DashLengthB.Value = valueB;
                };

            return dashLengthProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            DashLengthA.ToXml(config);
            DashLengthB.ToXml(config);
            SpaceLength.ToXml(config);
            ColorCount.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            Alignment.ToXml(config);
            Invert.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            DashLengthA.FromXml(config, DefaultDashLength);
            DashLengthB.FromXml(config, DefaultDashLength * 2f);
            SpaceLength.FromXml(config, DefaultSpaceLength);
            ColorCount.FromXml(config, false);
            SecondColor.FromXml(config, DefaultColor);
            Offset.FromXml(config, DefaultDoubleOffset);
            Alignment.FromXml(config, Manager.Alignment.Centre);
            Invert.FromXml(config, false);
            Invert.Value ^= map.Invert ^ invert ^ typeChanged;

            if (invert)
                Alignment.Value = Alignment.Value.Invert();
        }
    }
    public class SolidAndDashedLineStyle : RegularLineStyle, IRegularLine, IDoubleLine, IDoubleAlignmentLine, IDashedLine, IAsymLine
    {
        public override StyleType Type => StyleType.LineSolidAndDashed;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyBoolValue ColorCount { get; }
        public PropertyColorValue SecondColor { get; }
        public new PropertyValue<float> Offset { get; }
        public PropertyValue<float> DashLength { get; }
        public PropertyValue<float> SpaceLength { get; }
        public PropertyBoolValue Invert { get; }
        public PropertyBoolValue CenterSolid { get; }
        private FakeAligmentProperty FakeAligment { get; }
        public PropertyEnumValue<Alignment> Alignment => FakeAligment;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(ColorCount);
                yield return nameof(Color);
                yield return nameof(SecondColor);
                yield return nameof(Width);
                yield return nameof(Length);
                yield return nameof(Offset);
                yield return nameof(CenterSolid);
                yield return nameof(Alignment);
                yield return nameof(Invert);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;

        public SolidAndDashedLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, float dashLength, float spaceLength, float offset) : base(color, width)
        {
            ColorCount = GetUseSecondColorProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(ColorCount ? secondColor : color);
            Offset = GetOffsetProperty(offset);
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
            Invert = GetInvertProperty(false);
            CenterSolid = GetCenterSolidProperty(false);
            FakeAligment = new FakeAligmentProperty(string.Empty, base.StyleChanged, this.GetAlignment, this.SetAlignment, Manager.Alignment.Centre);
        }
        private Alignment GetAlignment() => CenterSolid ? (Invert ? Manager.Alignment.Right : Manager.Alignment.Left) : Manager.Alignment.Centre;
        private void SetAlignment(Alignment value)
        {
            CenterSolid.Value = value != Manager.Alignment.Centre;
            Invert.Value = value == Manager.Alignment.Right;
        }

        protected override IStyleData CalculateImpl(MarkupRegularLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            var solidOffset = CenterSolid ? 0 : Invert ? Offset : -Offset;
            var dashedOffset = (Invert ? -Offset : Offset) * (CenterSolid ? 2 : 1);
            var borders = line.Borders;

            var dashes = new List<MarkupPartData>();

            dashes.AddRange(StyleHelper.CalculateSolid(trajectory, lod, CalculateSolidDash));
            if (CheckDashedLod(lod, Width, DashLength))
                dashes.AddRange(StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashedDash));

            return new MarkupPartGroupData(lod, dashes);

            IEnumerable<MarkupPartData> CalculateSolidDash(ITrajectory lineTrajectory)
            {
                if (StyleHelper.CalculateSolidPart(borders, lineTrajectory, solidOffset, Width, Color, out MarkupPartData dash))
                    yield return dash;
            }

            IEnumerable<MarkupPartData> CalculateDashedDash(ITrajectory lineTrajectory, float startT, float endT)
            {
                if (StyleHelper.CalculateDashedParts(borders, lineTrajectory, startT, endT, DashLength, dashedOffset, Width, ColorCount ? SecondColor : Color, out MarkupPartData dash))
                    yield return dash;
            }
        }
        public override RegularLineStyle CopyLineStyle() => new SolidAndDashedLineStyle(Color, SecondColor, ColorCount, Width, DashLength, SpaceLength, Offset);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength.Value = DashLength;
                dashedTarget.SpaceLength.Value = SpaceLength;
            }
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset.Value = Offset;
                doubleTarget.SecondColor.Value = SecondColor;
                doubleTarget.ColorCount.Value = ColorCount;
            }
            if (target is IDoubleAlignmentLine doubleAlignmentTarget)
                doubleAlignmentTarget.Alignment.Value = Alignment;
        }
        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddUseSecondColorProperty(this, parent, true));
            components.Add(AddSecondColorProperty(this, parent, true));
            components.Add(AddLengthProperty(this, parent, false));
            components.Add(AddOffsetProperty(this, parent, false));
            if (!isTemplate)
            {
                components.Add(AddCenterSolidProperty(parent, false));
                components.Add(AddInvertProperty(this, parent, false));
            }

            UseSecondColorChanged(this, parent, ColorCount);
        }
        protected BoolListPropertyPanel AddCenterSolidProperty(UIComponent parent, bool canCollapse)
        {
            var centerSolidProperty = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(CenterSolid));
            centerSolidProperty.Text = Localize.StyleOption_SolidInCenter;
            centerSolidProperty.CanCollapse = canCollapse;
            centerSolidProperty.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            centerSolidProperty.SelectedObject = CenterSolid;
            centerSolidProperty.OnSelectObjectChanged += (value) => CenterSolid.Value = value;
            return centerSolidProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            ColorCount.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            DashLength.ToXml(config);
            SpaceLength.ToXml(config);
            Invert.ToXml(config);
            CenterSolid.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            ColorCount.FromXml(config, false);
            SecondColor.FromXml(config, DefaultColor);
            Offset.FromXml(config, DefaultDoubleOffset);
            DashLength.FromXml(config, DefaultDashLength);
            SpaceLength.FromXml(config, DefaultSpaceLength);
            CenterSolid.FromXml(config, false);
            Invert.FromXml(config, false);
            Invert.Value ^= map.Invert ^ invert ^ typeChanged;
        }

        private class FakeAligmentProperty : PropertyEnumValue<Alignment>
        {
            private Func<Alignment> OnGet { get; }
            private Action<Alignment> OnSet { get; }

            public override Alignment Value { get => OnGet(); set => OnSet(value); }

            public FakeAligmentProperty(string label, Action onChanged, Func<Alignment> onGet, Action<Alignment> onSet, Alignment value = default) : base(label, onChanged, value)
            {
                OnGet = onGet;
                OnSet = onSet;
                Value = value;
            }
        }
    }
    public class SharkTeethLineStyle : RegularLineStyle, IColorStyle, IAsymLine, ISharkLine
    {
        public override StyleType Type => StyleType.LineSharkTeeth;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;
        protected override float LodWidth => 0.5f;

        public PropertyValue<float> Base { get; }
        public PropertyValue<float> Height { get; }
        public PropertyValue<float> Space { get; }
        public PropertyBoolValue Invert { get; }
        public PropertyValue<float> Angle { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(Triangle);
                yield return nameof(Space);
                yield return nameof(Angle);
                yield return nameof(Invert);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;

        public SharkTeethLineStyle(Color32 color, float baseValue, float height, float space, float angle) : base(color, 0)
        {
            Base = GetBaseProperty(baseValue);
            Height = GetHeightProperty(height);
            Space = GetSpaceProperty(space);
            Invert = GetInvertProperty(true);
            Angle = GetAngleProperty(angle);
        }
        protected override IStyleData CalculateImpl(MarkupRegularLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            if (!CheckDashedLod(lod, Height, Base))
                return new MarkupPartGroupData(lod);

            var borders = line.Borders;
            var coef = Mathf.Cos(Angle * Mathf.Deg2Rad);
            return new MarkupPartGroupData(lod, StyleHelper.CalculateDashed(trajectory, Base / coef, Space / coef, CalculateDashes));

            IEnumerable<MarkupPartData> CalculateDashes(ITrajectory trajectory, float startT, float endT)
            {
                if (StyleHelper.CalculateDashedParts(borders, trajectory, Invert ? endT : startT, Invert ? startT : endT, Base, Height / (Invert ? 2 : -2), Height, Color, out MarkupPartData dash))
                {
                    dash.Material = RenderHelper.MaterialLib[MaterialType.Triangle];
                    dash.Angle -= Angle * Mathf.Deg2Rad;
                    yield return dash;
                }
            }
        }

        public override RegularLineStyle CopyLineStyle() => new SharkTeethLineStyle(Color, Base, Height, Space, Angle);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is SharkTeethLineStyle sharkTeethTarget)
            {
                sharkTeethTarget.Base.Value = Base;
                sharkTeethTarget.Height.Value = Height;
                sharkTeethTarget.Space.Value = Space;
            }
        }
        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddTriangleProperty(this, parent, false));
            components.Add(AddSpaceProperty(this, parent, false));
            components.Add(AddAngleProperty(parent, true));

            if (!isTemplate)
                components.Add(AddInvertProperty(parent, false));
        }

        protected FloatPropertyPanel AddAngleProperty(UIComponent parent, bool canCollapse)
        {
            var angleProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Angle));
            angleProperty.Text = Localize.StyleOption_SharkToothAngle;
            angleProperty.Format = Localize.NumberFormat_Degree;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.CheckMin = true;
            angleProperty.MinValue = -60f;
            angleProperty.CheckMax = true;
            angleProperty.MaxValue = 60f;
            angleProperty.CanCollapse = canCollapse;
            angleProperty.Init();
            angleProperty.Value = Angle;
            angleProperty.OnValueChanged += (float value) => Angle.Value = value;

            return angleProperty;
        }
        protected ButtonPanel AddInvertProperty(UIComponent parent, bool canCollapse)
        {
            var invertButton = ComponentPool.Get<ButtonPanel>(parent, nameof(Invert));
            invertButton.Text = Localize.StyleOption_Invert;
            invertButton.CanCollapse = canCollapse;
            invertButton.Init();

            invertButton.OnButtonClick += () =>
            {
                Invert.Value = !Invert;
                Angle.Value = -Angle;
                if (parent.Find<FloatPropertyPanel>(nameof(Angle)) is FloatPropertyPanel angleProperty)
                    angleProperty.Value = Angle;
            };

            return invertButton;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Base.ToXml(config);
            Height.ToXml(config);
            Space.ToXml(config);
            Invert.ToXml(config);
            Angle.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Base.FromXml(config, DefaultSharkBaseLength);
            Height.FromXml(config, DefaultSharkHeight);
            Space.FromXml(config, DefaultSharkSpaceLength);
            Angle.FromXml(config, DefaultSharkAngle);
            Invert.FromXml(config, false);
            Invert.Value ^= map.Invert ^ invert ^ typeChanged;
        }
    }
    public class ZigZagLineStyle : RegularLineStyle, IRegularLine
    {
        public override StyleType Type => StyleType.LineZigZag;

        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(Step);
                yield return nameof(Offset);
                yield return nameof(Side);
                yield return nameof(StartFrom);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;

        public PropertyStructValue<float> Step { get; }
        public new PropertyStructValue<float> Offset { get; }
        public PropertyBoolValue Side { get; }
        public PropertyBoolValue StartFrom { get; }

        public ZigZagLineStyle(Color32 color, float width, float step, float offset, bool side, bool startFrom) : base(color, width)
        {
            Step = new PropertyStructValue<float>("S", StyleChanged, step);
            Offset = new PropertyStructValue<float>("O", StyleChanged, offset);
            Side = new PropertyBoolValue("SD", StyleChanged, side);
            StartFrom = new PropertyBoolValue("SF", StyleChanged, startFrom);
        }

        public override RegularLineStyle CopyLineStyle() => new ZigZagLineStyle(Color, Width, Step, Offset, Side, StartFrom);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);

            if(target is ZigZagLineStyle zigzagTarget)
            {
                zigzagTarget.Step.Value = Step;
                zigzagTarget.Offset.Value = Offset;
                zigzagTarget.Side.Value = Side;
                zigzagTarget.StartFrom.Value = StartFrom;
            }
        }

        protected override IStyleData CalculateImpl(MarkupRegularLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            var count = Mathf.FloorToInt(trajectory.Length / Step.Value);
            var startOffset = (trajectory.Length - Step.Value * count) * 0.5f;

            var dashes = new MarkupPartData[StartFrom ? count * 2 : count * 2 + 2];

            for (int i = 0; i < count; i += 1)
            {
                var startDistance = startOffset + Step.Value * i;
                var endDistance = startDistance + Step.Value;
                var middleDistance = (startDistance + endDistance) * 0.5f;
                var startT = trajectory.Travel(startDistance);
                var endT = trajectory.Travel(endDistance);
                var middleT = trajectory.Travel(middleDistance);

                if (StartFrom)
                {
                    var startPos = trajectory.Position(startT);
                    var middlePos = trajectory.Position(middleT) + trajectory.Tangent(middleT).MakeFlatNormalized().Turn90(!Side) * Offset;
                    var endPos = trajectory.Position(endT);

                    dashes[2 * i] = new MarkupPartData(startPos, middlePos, Width, Color, RenderHelper.MaterialLib[MaterialType.RectangleLines]);
                    dashes[2 * i + 1] = new MarkupPartData(middlePos, endPos, Width, Color, RenderHelper.MaterialLib[MaterialType.RectangleLines]);
                }
                else
                {
                    var startPos = trajectory.Position(startT) + trajectory.Tangent(startT).MakeFlatNormalized().Turn90(!Side) * Offset;
                    var middlePos = trajectory.Position(middleT);
                    var endPos = trajectory.Position(endT) + trajectory.Tangent(endT).MakeFlatNormalized().Turn90(!Side) * Offset;

                    dashes[2 * i] = new MarkupPartData(startPos, middlePos, Width, Color, RenderHelper.MaterialLib[MaterialType.RectangleLines]);
                    dashes[2 * i + 1] = new MarkupPartData(middlePos, endPos, Width, Color, RenderHelper.MaterialLib[MaterialType.RectangleLines]);
                }
            }

            if(!StartFrom)
            {
                var startT = trajectory.Travel(startOffset);
                var endT = trajectory.Travel(startOffset + Step.Value * count);

                var startPos = trajectory.Position(startT);
                var startDir = trajectory.Tangent(startT).MakeFlatNormalized().Turn90(!Side);
                var endPos = trajectory.Position(endT);
                var endDir = trajectory.Tangent(endT).MakeFlatNormalized().Turn90(!Side);

                dashes[count * 2] = new MarkupPartData(startPos, startPos + startDir * Offset, Width, Color, RenderHelper.MaterialLib[MaterialType.RectangleLines]);
                dashes[count * 2 + 1] = new MarkupPartData(endPos, endPos + endDir * Offset, Width, Color, RenderHelper.MaterialLib[MaterialType.RectangleLines]);
            }

            return new MarkupPartGroupData(lod, dashes);
        }

        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddStepProperty(parent, false));
            components.Add(AddOffsetProperty(parent, false));
            components.Add(AddSideProperty(parent, false));
            components.Add(AddStartFromProperty(parent, false));
        }
        protected FloatPropertyPanel AddStepProperty(UIComponent parent, bool canCollapse)
        {
            var stepProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Step));
            stepProperty.Text = Localize.StyleOption_ZigzagStep;
            stepProperty.Format = Localize.NumberFormat_Meter;
            stepProperty.UseWheel = true;
            stepProperty.WheelStep = 0.1f;
            stepProperty.WheelTip = Settings.ShowToolTip;
            stepProperty.CheckMin = true;
            stepProperty.MinValue = 0.3f;
            stepProperty.CanCollapse = canCollapse;
            stepProperty.Init();
            stepProperty.Value = Step;
            stepProperty.OnValueChanged += (float value) => Step.Value = value;

            return stepProperty;
        }
        protected FloatPropertyPanel AddOffsetProperty(UIComponent parent, bool canCollapse)
        {
            var offsetProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Offset));
            offsetProperty.Text = Localize.StyleOption_ZigzagOffset;
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0.1f;
            offsetProperty.CanCollapse = canCollapse;
            offsetProperty.Init();
            offsetProperty.Value = Offset;
            offsetProperty.OnValueChanged += (float value) => Offset.Value = value;

            return offsetProperty;
        }
        protected BoolListPropertyPanel AddSideProperty(UIComponent parent, bool canCollapse)
        {
            var sideProperty = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(Side));
            sideProperty.Text = Localize.StyleOption_ZigzagSide;
            sideProperty.CanCollapse = canCollapse;
            sideProperty.Init(Localize.StyleOption_SideLeft, Localize.StyleOption_SideRight);
            sideProperty.SelectedObject = Side;
            sideProperty.OnSelectObjectChanged += (value) => Side.Value = value;
            return sideProperty;
        }
        protected BoolListPropertyPanel AddStartFromProperty(UIComponent parent, bool canCollapse)
        {
            var startFromProperty = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(Side));
            startFromProperty.Text = Localize.StyleOption_ZigzagStartFrom;
            startFromProperty.CanCollapse = canCollapse;
            startFromProperty.Init(Localize.StyleOption_ZigzagStartFromOutside, Localize.StyleOption_ZigzagStartFromLine);
            startFromProperty.SelectedObject = StartFrom;
            startFromProperty.OnSelectObjectChanged += (value) => StartFrom.Value = value;
            return startFromProperty;
        }

        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Step.FromXml(config, 1f);
            Offset.FromXml(config, 1f);
            Side.FromXml(config, true);
            StartFrom.FromXml(config, true);

            if (typeChanged)
                Side.Value = !Side.Value;
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            Step.ToXml(config);
            Offset.ToXml(config);
            Side.ToXml(config);
            return config;
        }
    }
}
