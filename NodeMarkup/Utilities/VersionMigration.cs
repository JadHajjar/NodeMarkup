﻿using ColossalFramework;
using NodeMarkup.Manager;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.Utilities
{
    public static class VersionMigration
    {
        public static ObjectsMap Befor1_2(Manager.NodeMarkup markup, ObjectsMap map)
        {
            if (map == null)
                map = new ObjectsMap();

            foreach (var enter in markup.Enters)
            {
                foreach (var point in enter.Points.Skip(1).Take(enter.PointCount - 2))
                {
                    switch (point.Source.Location)
                    {
                        case MarkupPoint.LocationType.LeftEdge:
                            map.AddPoint(point.Id, point.Id - (1 << 16));
                            break;
                        case MarkupPoint.LocationType.RightEdge:
                            map.AddPoint(point.Id, point.Id + (1 << 16));
                            break;
                    }
                }
            }

            return map;
        }
        public static ObjectsMap Befor1_9(Markup markup, ObjectsMap map)
        {
            if (map == null)
                map = new ObjectsMap();

            foreach (var enter in markup.Enters)
            {
                ref var segment = ref enter.GetSegment();
                if (segment.Info.m_vehicleTypes.IsFlagSet(VehicleInfo.VehicleType.Plane))
                {
                    var sourceId = MarkupPoint.GetId(enter.Id, 2, MarkupPoint.PointType.Enter);
                    var targetId = MarkupPoint.GetId(enter.Id, 3, MarkupPoint.PointType.Enter);
                    map.AddPoint(sourceId, targetId);

                    sourceId = MarkupPoint.GetId(enter.Id, 2, MarkupPoint.PointType.Crosswalk);
                    targetId = MarkupPoint.GetId(enter.Id, 3, MarkupPoint.PointType.Crosswalk);
                    map.AddPoint(sourceId, targetId);

                    sourceId = MarkupPoint.GetId(enter.Id, 2, MarkupPoint.PointType.Normal);
                    targetId = MarkupPoint.GetId(enter.Id, 3, MarkupPoint.PointType.Normal);
                    map.AddPoint(sourceId, targetId);
                }
            }

            return map;
        }

        private static Dictionary<byte, byte> Correction01Dic { get; } = new Dictionary<byte, byte>();
        public static Color32 CorrectColor01(Color32 color)
        {
            if (!Correction01Dic.TryGetValue(color.a, out byte newAlpha))
            {
                newAlpha = (byte)(Mathf.Pow(Mathf.Max(Mathf.Pow((float)color.a / byte.MaxValue, 2f) - 0.015f, 0f) / 0.985f, 0.25f) * byte.MaxValue);
                Correction01Dic[color.a] = newAlpha;
            }

            color.a = newAlpha;
            return color;
        }
    }
}
