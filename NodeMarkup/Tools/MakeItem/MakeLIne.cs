﻿using ColossalFramework.Math;
using ModsCommon;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.Tools
{
    public class MakeLineToolMode : BaseMakeItemToolMode
    {
        public override ToolModeType Type => ToolModeType.MakeLine;

        public override string GetToolInfo()
        {
            var tips = new List<string>();

            if (!IsSelectPoint)
            {
                tips.Add(Localize.Tool_InfoSelectLineStartPoint);
                tips.Add(Settings.HoldCtrlToMovePoint ? string.Format(Localize.Tool_InfoStartDragPointMode, LocalizeExtension.Ctrl.AddInfoColor()) : Localize.Tool_InfoDragPointMode);
                if ((Markup.Support & Markup.SupportType.Fillers) != 0)
                    tips.Add(string.Format(Localize.Tool_InfoStartCreateFiller, LocalizeExtension.Alt.AddInfoColor()));
                if ((Markup.Support & Markup.SupportType.Croswalks) != 0)
                    tips.Add(string.Format(Localize.Tool_InfoStartCreateCrosswalk, LocalizeExtension.Shift.AddInfoColor()));
            }
            else if (!IsHoverPoint)
            {
                if (SelectPoint.Type == MarkupPoint.PointType.Lane)
                {
                    tips.Add(Localize.Tool_InfoSelectLaneEndPoint);
                }
                else
                {
                    if ((SelectPoint.Markup.SupportLines & LineType.Stop) == 0)
                        tips.Add(Localize.Tool_InfoSelectLineEndPoint);
                    else
                        tips.Add(Localize.Tool_InfoSelectLineEndPointStop);

                    if ((SelectPoint.Enter.SupportPoints & MarkupPoint.PointType.Normal) != 0)
                        tips.Add(Localize.Tool_InfoSelectLineEndPointNormal);
                }
            }
            else
                tips.Add(base.GetToolInfo());

            return string.Join("\n", tips.ToArray());
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (IsSelectPoint)
                return;

            if (!Tool.Panel.IsHover)
            {
                if (Utility.OnlyAltIsPressed && (Markup.Support & Markup.SupportType.Fillers) != 0)
                {
                    Tool.SetMode(ToolModeType.MakeFiller);
                    if (Tool.NextMode is MakeFillerToolMode fillerToolMode)
                        fillerToolMode.DisableByAlt = true;
                }
                else if (Utility.OnlyShiftIsPressed && (Markup.Support & Markup.SupportType.Croswalks) != 0)
                    Tool.SetMode(ToolModeType.MakeCrosswalk);
            }
        }

        public override void OnMouseDrag(Event e)
        {
            if ((!Settings.HoldCtrlToMovePoint || Utility.OnlyCtrlIsPressed) && !IsSelectPoint && IsHoverPoint && HoverPoint.Type == MarkupPoint.PointType.Enter)
                Tool.SetMode(ToolModeType.DragPoint);
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            if (!IsHoverPoint)
                return;

            if (!IsSelectPoint)
                base.OnPrimaryMouseClicked(e);
            else
            {
                var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);

                if (Tool.Markup.TryGetLine(pointPair, out MarkupLine line))
                {
                    if (Utility.OnlyCtrlIsPressed)
                        Panel.SelectLine(line);
                    else
                        Tool.DeleteItem(line, OnDelete);
                }
                else if (pointPair.IsStopLine)
                {
                    var style = Tool.GetStyleByModifier<StopLineStyle, StopLineStyle.StopLineType>(NetworkType.Road, LineType.Stop, StopLineStyle.StopLineType.Solid);
                    var newLine = Tool.Markup.AddStopLine(pointPair, style);
                    Panel.SelectLine(newLine);
                }
                else if (pointPair.IsLane)
                {
                    var style = Tool.GetStyleByModifier<RegularLineStyle, RegularLineStyle.RegularLineType>(pointPair.NetworkType, LineType.Lane, RegularLineStyle.RegularLineType.Prop, true);
                    var newLine = Tool.Markup.AddRegularLine(pointPair, style);
                    Panel.SelectLine(newLine);

                    if (Settings.CreateLaneEdgeLines && pointPair.First is MarkupLanePoint lanePointS && pointPair.Second is MarkupLanePoint lanePointE)
                    {
                        lanePointS.Source.GetPoints(out var leftPointS, out var rightPointS);
                        lanePointE.Source.GetPoints(out var leftPointE, out var rightPointE);

                        var pairA = new MarkupPointPair(leftPointS, rightPointE);
                        if (!Markup.TryGetLine(pairA, out MarkupLine lineA))
                        {
                            lineA = Markup.AddRegularLine(pairA, null);
                            Panel.AddLine(lineA);
                        }

                        var pairB = new MarkupPointPair(leftPointE, rightPointS);
                        if (!Markup.TryGetLine(pairB, out MarkupLine lineB))
                        {
                            lineB = Markup.AddRegularLine(pairB, null);
                            Panel.AddLine(lineB);
                        }
                    }
                }
                else
                {
                    var style = Tool.GetStyleByModifier<RegularLineStyle, RegularLineStyle.RegularLineType>(pointPair.NetworkType, LineType.Regular, RegularLineStyle.RegularLineType.Dashed, true);
                    var newLine = Tool.Markup.AddRegularLine(pointPair, style);
                    Panel.SelectLine(newLine);
                }

                SelectPoint = null;
                SetTarget();
            }
        }
        private void OnDelete(MarkupLine line)
        {
            var fillers = Markup.GetLineFillers(line).ToArray();

            if (line is MarkupCrosswalkLine crosswalkLine)
                Panel.DeleteCrosswalk(crosswalkLine.Crosswalk);
            foreach (var filler in fillers)
                Panel.DeleteFiller(filler);

            Panel.DeleteLine(line);
            Tool.Markup.RemoveLine(line);
        }
        protected override IEnumerable<MarkupPoint> GetTarget(Enter enter, MarkupPoint ignore)
        {
            var allow = enter.Points.Select(i => 1).ToArray();

            if (ignore == null)
            {
                foreach (var point in enter.Points)
                    yield return point;
                if (Markup.EntersCount > 1)
                {
                    foreach (var point in enter.LanePoints)
                        yield return point;
                }
            }
            else if (ignore.Type == MarkupPoint.PointType.Enter)
            {
                if (ignore != null && ignore.Enter == enter)
                {
                    if ((Markup.SupportLines & LineType.Stop) == 0)
                        yield break;

                    var ignoreIdx = ignore.Index - 1;
                    var leftIdx = ignoreIdx;
                    var rightIdx = ignoreIdx;

                    foreach (var line in enter.Markup.Lines.Where(l => l.Type == LineType.Stop && l.Start.Enter == enter))
                    {
                        var from = Math.Min(line.Start.Index, line.End.Index) - 1;
                        var to = Math.Max(line.Start.Index, line.End.Index) - 1;
                        if (from < ignore.Index - 1 && ignore.Index - 1 < to)
                            yield break;

                        allow[from] = 2;
                        allow[to] = 2;

                        for (var i = from + 1; i <= to - 1; i += 1)
                            allow[i] = 0;

                        if (line.ContainsPoint(ignore))
                        {
                            var otherIdx = line.PointPair.GetOther(ignore).Index - 1;
                            if (otherIdx < ignoreIdx)
                                leftIdx = otherIdx;
                            else if (otherIdx > ignoreIdx)
                                rightIdx = otherIdx;
                        }
                    }

                    SetNotAllow(allow, leftIdx == ignoreIdx ? Find(allow, ignoreIdx, -1) : leftIdx, -1);
                    SetNotAllow(allow, rightIdx == ignoreIdx ? Find(allow, ignoreIdx, 1) : rightIdx, 1);
                    allow[ignoreIdx] = 0;
                }

                foreach (var point in enter.Points)
                {
                    if (allow[point.Index - 1] != 0)
                        yield return point;
                }
            }
            else if (ignore.Type == MarkupPoint.PointType.Lane)
            {
                if (enter != ignore.Enter)
                {
                    foreach (var point in enter.LanePoints)
                        yield return point;
                }
            }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            Panel.Render(cameraInfo);

            if (IsHoverPoint)
            {
                if (Utility.CtrlIsPressed)
                    HoverPoint.Render(new OverlayData(cameraInfo) { Width = 0.53f });
                else
                    HoverPoint.Render(new OverlayData(cameraInfo) { Color = Colors.Hover, Width = 0.53f });
            }

            RenderPointsOverlay(cameraInfo, !Utility.CtrlIsPressed);

            if (IsSelectPoint)
            {
                if (IsHoverPoint)
                {
                    if (SelectPoint.Type == MarkupPoint.PointType.Normal)
                        RenderNormalConnectLine(cameraInfo);
                    else if (SelectPoint.Type == MarkupPoint.PointType.Lane)
                        RenderLaneConnectionLine(cameraInfo);
                    else
                        RenderRegularConnectLine(cameraInfo);
                }
                else
                {
                    if (SelectPoint.Type == MarkupPoint.PointType.Lane)
                        RenderNotConnectedLane(cameraInfo);
                    else
                        RenderNotConnectLine(cameraInfo);
                }
            }
#if DEBUG
            if (Settings.ShowNodeContour && Tool.Markup is Manager.NodeMarkup markup)
            {
                foreach (var line in markup.Contour)
                    line.Render(new OverlayData(cameraInfo));
            }
#endif
        }

        private void RenderRegularConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            var startPos = SelectPoint.MarkerPosition;
            var endPos = HoverPoint.MarkerPosition;
            var startDir = HoverPoint.Enter == SelectPoint.Enter ? HoverPoint.MarkerPosition - SelectPoint.MarkerPosition : SelectPoint.Direction;
            var endDir = HoverPoint.Enter == SelectPoint.Enter ? SelectPoint.MarkerPosition - HoverPoint.MarkerPosition : HoverPoint.Direction;
            var smoothStart = SelectPoint.Enter.IsSmooth;
            var smoothEnd = HoverPoint.Enter.IsSmooth;
            var bezier = new BezierTrajectory(startPos, startDir, endPos, endDir, true, smoothStart, smoothEnd);

            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
            var color = Tool.Markup.ExistLine(pointPair) ? (Utility.OnlyCtrlIsPressed ? Colors.Yellow : Colors.Red) : Colors.Green;

            bezier.Render(new OverlayData(cameraInfo) { Color = color });
        }
        private void RenderNormalConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
            var color = Tool.Markup.ExistLine(pointPair) ? (Utility.OnlyCtrlIsPressed ? Colors.Yellow : Colors.Red) : Colors.Purple;

            var lineBezier = new Bezier3()
            {
                a = SelectPoint.MarkerPosition,
                b = HoverPoint.MarkerPosition,
                c = SelectPoint.MarkerPosition,
                d = HoverPoint.MarkerPosition,
            };
            lineBezier.RenderBezier(new OverlayData(cameraInfo) { Color = color });

            var normal = SelectPoint.Direction.Turn90(false);

            var normalBezier = new Bezier3
            {
                a = SelectPoint.MarkerPosition + SelectPoint.Direction,
                d = SelectPoint.MarkerPosition + normal
            };
            normalBezier.b = normalBezier.a + normal / 2;
            normalBezier.c = normalBezier.d + SelectPoint.Direction / 2;
            normalBezier.RenderBezier(new OverlayData(cameraInfo) { Color = color, Width = 2f, Cut = true });
        }
        private void RenderLaneConnectionLine(RenderManager.CameraInfo cameraInfo)
        {
            if (SelectPoint is MarkupLanePoint pointA && HoverPoint is MarkupLanePoint pointB)
            {
                var trajectories = new List<ITrajectory>()
                {
                    new StraightTrajectory(pointA.SourcePointA.Position, pointA.SourcePointB.Position),
                    new BezierTrajectory(pointA.SourcePointB.Position, pointA.SourcePointB.Direction, pointB.SourcePointA.Position, pointB.SourcePointA.Direction, false, pointA.Enter.IsSmooth, pointB.Enter.IsSmooth),
                    new StraightTrajectory(pointB.SourcePointA.Position, pointB.SourcePointB.Position),
                    new BezierTrajectory(pointB.SourcePointB.Position, pointB.SourcePointB.Direction, pointA.SourcePointA.Position, pointA.SourcePointA.Direction, false, pointB.Enter.IsSmooth, pointA.Enter.IsSmooth),
                };

                var pointPair = new MarkupPointPair(pointA, pointB);
                var color = Tool.Markup.ExistLine(pointPair) ? (Utility.OnlyCtrlIsPressed ? Colors.Yellow : Colors.Red) : Colors.Green;

                var triangles = Triangulator.TriangulateSimple(trajectories, out var points, minAngle: 5, maxLength: 10f);
                points.RenderArea(triangles, new OverlayData(cameraInfo) { Color = color, AlphaBlend = false });
            }
        }

        private void RenderNotConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            Vector3 endPosition;
            if (Markup is SegmentMarkup segmentMarkup)
            {
                segmentMarkup.Trajectory.GetHitPosition(SingletonTool<NodeMarkupTool>.Instance.Ray, out _, out _, out endPosition);
                endPosition = SingletonTool<NodeMarkupTool>.Instance.Ray.GetRayPosition(endPosition.y, out _);
            }
            else
                endPosition = SingletonTool<NodeMarkupTool>.Instance.Ray.GetRayPosition(Markup.Position.y, out _);

            new BezierTrajectory(SelectPoint.MarkerPosition, SelectPoint.Direction, endPosition).Render(new OverlayData(cameraInfo) { Color = Colors.Hover });
        }
        private void RenderNotConnectedLane(RenderManager.CameraInfo cameraInfo)
        {
            if (SelectPoint is MarkupLanePoint lanePoint)
            {
                var halfWidth = lanePoint.Width * lanePoint.Enter.TranformCoef * 0.5f;

                Vector3 endPosition;
                if (Markup is SegmentMarkup segmentMarkup)
                {
                    segmentMarkup.Trajectory.GetHitPosition(SingletonTool<NodeMarkupTool>.Instance.Ray, out _, out _, out endPosition);
                    endPosition = SingletonTool<NodeMarkupTool>.Instance.Ray.GetRayPosition(endPosition.y, out _);
                }
                else
                    endPosition = SingletonTool<NodeMarkupTool>.Instance.Ray.GetRayPosition(Markup.Position.y, out _);

                if ((lanePoint.Position - endPosition).sqrMagnitude < 4f * halfWidth * halfWidth)
                {
                    var normal = (lanePoint.Position - endPosition).MakeFlatNormalized().Turn90(true);
                    var area = new Quad3()
                    {
                        a = lanePoint.Position + normal * halfWidth,
                        b = lanePoint.Position - normal * halfWidth,
                        c = endPosition - normal * halfWidth,
                        d = endPosition + normal * halfWidth,
                    };

                    area.RenderQuad(new OverlayData(cameraInfo) { Color = Colors.Hover, AlphaBlend = false });
                }
                else
                {
                    var trajectory = new BezierTrajectory(lanePoint.MarkerPosition, lanePoint.Direction, endPosition);

                    var normal = trajectory.EndDirection.MakeFlatNormalized().Turn90(false);
                    var pointA = lanePoint.Markup.Type == MarkupType.Node ? lanePoint.SourcePointA : lanePoint.SourcePointB;
                    var pointB = lanePoint.Markup.Type == MarkupType.Node ? lanePoint.SourcePointB : lanePoint.SourcePointA;

                    var trajectories = new List<ITrajectory>()
                    { 
                        new BezierTrajectory(pointA.Position, pointA.Direction, trajectory.EndPosition + normal * halfWidth, trajectory.EndDirection, false, true, true),
                        new StraightTrajectory(trajectory.EndPosition + normal * halfWidth, trajectory.EndPosition - normal * halfWidth),
                        new BezierTrajectory(trajectory.EndPosition - normal * halfWidth, trajectory.EndDirection, pointB.Position, pointB.Direction, false, true, true),
                        new StraightTrajectory(pointB.Position, pointA.Position),
                    };

                    var triangles = Triangulator.TriangulateSimple(trajectories, out var points, minAngle: 5, maxLength: 10f);
                    points.RenderArea(triangles, new OverlayData(cameraInfo) { Color = Colors.Hover, AlphaBlend = false });
                }
            }
        }
    }
}
