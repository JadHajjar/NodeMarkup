﻿using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using System;
using System.Linq;

namespace NodeMarkup.UI
{
    public abstract class StylePropertyPanel : EnumOncePropertyPanel<Style.StyleType, StylePropertyPanel.StyleDropDown>
    {
        protected override bool IsEqual(Style.StyleType first, Style.StyleType second) => first == second;
        public class StyleDropDown : UIDropDown<Style.StyleType> { }
        protected override string GetDescription(Style.StyleType value) => value.Description();
    }
    public abstract class StylePropertyPanel<StyleType> : StylePropertyPanel
        where StyleType : Enum
    {
        protected override void FillItems(Func<Style.StyleType, bool> selector)
        {
            foreach (var value in Enum.GetValues(typeof(StyleType)).Cast<object>().Cast<Style.StyleType>())
            {
                if (selector?.Invoke(value) != false && value.IsVisible())
                    Selector.AddItem(value, GetDescription(value));
            }
        }
    }
    public class RegularStylePropertyPanel : StylePropertyPanel<RegularLineStyle.RegularLineType> { }
    public class StopStylePropertyPanel : StylePropertyPanel<StopLineStyle.StopLineType> { }
    public class CrosswalkPropertyPanel : StylePropertyPanel<CrosswalkStyle.CrosswalkType> { }
    public class FillerStylePropertyPanel : StylePropertyPanel<FillerStyle.FillerType> { }



    public class MarkupLineListPropertyPanel : ListPropertyPanel<MarkupLine, MarkupLineListPropertyPanel.MarkupLineDropDown>
    {
        protected override bool IsEqual(MarkupLine first, MarkupLine second) => ReferenceEquals(first, second);
        public class MarkupLineDropDown : UIDropDown<MarkupLine> { }
    }
    public class ChevronFromPropertyPanel : EnumOncePropertyPanel<ChevronFillerStyle.From, ChevronFromPropertyPanel.ChevronFromSegmented>
    {
        protected override bool IsEqual(ChevronFillerStyle.From first, ChevronFillerStyle.From second) => first == second;
        public class ChevronFromSegmented : UIOnceSegmented<ChevronFillerStyle.From> { }
        protected override string GetDescription(ChevronFillerStyle.From value) => value.Description();
    }
    public class LineAlignmentPropertyPanel : EnumOncePropertyPanel<Alignment, LineAlignmentPropertyPanel.AlignmentSegmented>
    {
        protected override bool IsEqual(Alignment first, Alignment second) => first == second;
        public class AlignmentSegmented : UIOnceSegmented<Alignment> { }
        protected override string GetDescription(Alignment value) => value.Description();
    }
    public class PropColorPropertyPanel : EnumOncePropertyPanel<PropLineStyle.ColorOptionEnum, PropColorPropertyPanel.PropColorDropDown>
    {
        protected override bool IsEqual(PropLineStyle.ColorOptionEnum first, PropLineStyle.ColorOptionEnum second) => first == second;
        public class PropColorDropDown : UIDropDown<PropLineStyle.ColorOptionEnum> { }
        protected override string GetDescription(PropLineStyle.ColorOptionEnum value) => value.Description();
    }
}
