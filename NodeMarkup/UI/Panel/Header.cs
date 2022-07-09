﻿using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.UI.Panel
{
    public class PanelHeader : HeaderMoveablePanel<PanelHeaderContent>
    {
        private MarkupType Type { get; set; }
        public bool Available { set => Content.SetAvailable(value); }

        private HeaderButtonInfo<HeaderButton> PasteButton { get; }
        private HeaderButtonInfo<HeaderButton> EdgeLinesButton { get; }
        private HeaderButtonInfo<HeaderButton> CutButton { get; }
        private HeaderButtonInfo<HeaderButton> BeetwenIntersectionsButton { get; }
        private HeaderButtonInfo<HeaderButton> WholeStreetButton { get; }

        public PanelHeader()
        {
            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeMarkupTextures.Atlas, NodeMarkupTextures.AddTemplateHeaderButton, NodeMarkup.Localize.Panel_SaveAsPreset, NodeMarkupTool.SaveAsIntersectionTemplateShortcut));

            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeMarkupTextures.Atlas, NodeMarkupTextures.CopyHeaderButton, NodeMarkup.Localize.Panel_CopyMarking, NodeMarkupTool.CopyMarkingShortcut));

            PasteButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeMarkupTextures.Atlas, NodeMarkupTextures.PasteHeaderButton, NodeMarkup.Localize.Panel_PasteMarking, NodeMarkupTool.PasteMarkingShortcut);
            Content.AddButton(PasteButton);

            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeMarkupTextures.Atlas, NodeMarkupTextures.ClearHeaderButton, NodeMarkup.Localize.Panel_ClearMarking, NodeMarkupTool.DeleteAllShortcut));

            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, NodeMarkupTextures.Atlas, NodeMarkupTextures.EditHeaderButton, NodeMarkup.Localize.Panel_EditMarking, NodeMarkupTool.EditMarkingShortcut));

            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, NodeMarkupTextures.Atlas, NodeMarkupTextures.OffsetHeaderButton, NodeMarkup.Localize.Panel_ResetOffset, NodeMarkupTool.ResetOffsetsShortcut));

            EdgeLinesButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, NodeMarkupTextures.Atlas, NodeMarkupTextures.EdgeLinesHeaderButton, NodeMarkup.Localize.Panel_CreateEdgeLines, NodeMarkupTool.CreateEdgeLinesShortcut);
            Content.AddButton(EdgeLinesButton);

            CutButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, NodeMarkupTextures.Atlas, NodeMarkupTextures.CutHeaderButton, NodeMarkup.Localize.Panel_CutLinesByCrosswalks, NodeMarkupTool.CutLinesByCrosswalksShortcut);
            Content.AddButton(CutButton);

            BeetwenIntersectionsButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, NodeMarkupTextures.Atlas, NodeMarkupTextures.BeetwenIntersectionsHeaderButton, NodeMarkup.Localize.Panel_ApplyBetweenIntersections, NodeMarkupTool.ApplyBetweenIntersectionsShortcut);
            Content.AddButton(BeetwenIntersectionsButton);

            WholeStreetButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, NodeMarkupTextures.Atlas, NodeMarkupTextures.WholeStreetHeaderButton, NodeMarkup.Localize.Panel_ApplyWholeStreet, NodeMarkupTool.ApplyWholeStreetShortcut);
            Content.AddButton(WholeStreetButton);
        }

        public void Init(float height) => base.Init(height);
        public void Init(MarkupType type)
        {
            Type = type;
            base.Init(null);
        }

        public override void Refresh()
        {
            PasteButton.Enable = !SingletonTool<NodeMarkupTool>.Instance.IsMarkupBufferEmpty;

            EdgeLinesButton.Visible = Type == MarkupType.Node;
            CutButton.Visible = Type == MarkupType.Node;

            BeetwenIntersectionsButton.Visible = Type == MarkupType.Segment;
            WholeStreetButton.Visible = Type == MarkupType.Segment;

            base.Refresh();
        }
    }
}
