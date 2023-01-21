﻿using ColossalFramework;
using ModsCommon;
using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using static NetInfo;

namespace NodeMarkup.Manager
{
    public static class MarkingManager
    {
        public static int Errors { get; set; } = 0;
        public static bool HasErrors => Errors != 0;
        private static ushort[] NeedUpdateNodeIds { get; set; }
        private static ushort[] NeedUpdateSegmentIds { get; set; }

        public static void Clear()
        {
            SingletonManager<NodeMarkingManager>.Instance.Clear();
            SingletonManager<SegmentMarkingManager>.Instance.Clear();
        }
        public static void Destroy()
        {
            SingletonManager<NodeMarkingManager>.Destroy();
            SingletonManager<SegmentMarkingManager>.Destroy();
        }

        public static void NetNodeRenderInstancePostfix(RenderManager.CameraInfo cameraInfo, ushort nodeID, ref RenderManager.Instance data) => SingletonManager<NodeMarkingManager>.Instance.Render(cameraInfo, nodeID, ref data);

        public static void NetSegmentRenderInstancePostfix(RenderManager.CameraInfo cameraInfo, ushort segmentID, ref RenderManager.Instance data) => SingletonManager<SegmentMarkingManager>.Instance.Render(cameraInfo, segmentID, ref data);

        public static void NetManagerReleaseNodeImplementationPrefix(ushort node) => SingletonManager<NodeMarkingManager>.Instance.Remove(node);
        public static void NetManagerReleaseSegmentImplementationPrefix(ushort segment) => SingletonManager<SegmentMarkingManager>.Instance.Remove(segment);

        public static void GetToUpdate()
        {
            NeedUpdateNodeIds = NetManager.instance.GetUpdateNodes().ToArray();
            NeedUpdateSegmentIds = NetManager.instance.GetUpdateSegments().ToArray();
        }
        public static void Update()
        {
            SingletonManager<NodeMarkingManager>.Instance.Update(NeedUpdateNodeIds);
            SingletonManager<SegmentMarkingManager>.Instance.Update(NeedUpdateSegmentIds);
        }
        public static void UpdateNode(ushort nodeId) => SingletonManager<NodeMarkingManager>.Instance.Update(nodeId);
        public static void UpdateSegment(ushort segmentId) => SingletonManager<SegmentMarkingManager>.Instance.Update(segmentId);
        public static void UpdateAll()
        {
            SingletonManager<NodeMarkingManager>.Instance.UpdateAll();
            SingletonManager<SegmentMarkingManager>.Instance.UpdateAll();
        }

        public static void NetInfoInitNodeInfoPostfix_Rail(Node info)
        {
            if (info.m_nodeMaterial.shader.name == "Custom/Net/TrainBridge")
                info.m_nodeMaterial.renderQueue = 2470;
        }
        public static void NetInfoInitNodeInfoPostfix_LevelCrossing(Node info)
        {
            if (info.m_flagsRequired.IsFlagSet(NetNode.Flags.LevelCrossing))
                info.m_nodeMaterial.renderQueue = 2470;
        }
        public static void NetInfoInitSegmentInfoPostfix(Segment info)
        {
            if (info.m_segmentMaterial.shader.name == "Custom/Net/TrainBridge")
                info.m_segmentMaterial.renderQueue = 2470;
        }

        public static void Import(XElement config)
        {
            Clear();
            FromXml(config, new ObjectsMap(), true);
        }
        public static XElement ToXml()
        {
            var config = new XElement(nameof(NodeMarking));
            config.AddAttr("V", SingletonMod<Mod>.Version);

            Errors = 0;

            SingletonManager<NodeMarkingManager>.Instance.ToXml(config);
            SingletonManager<SegmentMarkingManager>.Instance.ToXml(config);

            return config;
        }
        public static void FromXml(XElement config, ObjectsMap map, bool needUpdate)
        {
            Errors = 0;

            var version = GetVersion(config);

            SingletonManager<NodeMarkingManager>.Instance.FromXml(config, map, version, needUpdate);
            SingletonManager<SegmentMarkingManager>.Instance.FromXml(config, map, version, needUpdate);
        }
        public static Version GetVersion(XElement config)
        {
            try { return new Version(config.Attribute("V").Value); }
            catch { return SingletonMod<Mod>.Version; }
        }
        public static void SetFailed()
        {
            Clear();
            Errors = -1;
        }
    }
    public abstract class MarkingManager<TypeMarking> : IManager, IEnumerable<TypeMarking>
        where TypeMarking : Marking
    {
        protected Dictionary<ushort, TypeMarking> Markings { get; } = new Dictionary<ushort, TypeMarking>();
        protected abstract MarkingType Type { get; }
        protected abstract string XmlName { get; }
        protected abstract ObjectsMap.TryGetDelegate<ushort> MapTryGet(ObjectsMap map);


        private static PropManager PropManager => Singleton<PropManager>.instance;

        public bool Exist(ushort id) => Markings.ContainsKey(id);
        public bool TryGetMarking(ushort id, out TypeMarking marking) => Markings.TryGetValue(id, out marking);
        public TypeMarking GetOrCreateMarking(ushort id)
        {
            if (!Markings.TryGetValue(id, out TypeMarking marking))
            {
                marking = NewMarking(id);
                Markings[id] = marking;
            }

            return marking;
        }
        public TypeMarking this[ushort id] => GetOrCreateMarking(id);
        protected abstract TypeMarking NewMarking(ushort id);

        public void Update(params ushort[] ids)
        {
            foreach (var id in ids)
            {
                if (Markings.TryGetValue(id, out TypeMarking marking))
                    marking.Update();
            }
        }
        public void UpdateAll()
        {
            SingletonMod<Mod>.Logger.Debug($"Update all {Type} markings");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var toUpdate = Markings.Keys.ToArray();
            Update(toUpdate);
            sw.Stop();

            SingletonMod<Mod>.Logger.Debug($"{toUpdate.Length} {Type} markings updated in {sw.ElapsedMilliseconds}ms");
        }

        public void Render(RenderManager.CameraInfo cameraInfo, ushort id, ref RenderManager.Instance data)
        {
            try
            {
                if (data.m_nextInstance != ushort.MaxValue)
                    return;

                if (!cameraInfo.CheckRenderDistance(data.m_position, Settings.RenderDistance))
                    return;

                if (!TryGetMarking(id, out TypeMarking marking))
                    return;

                if (marking.NeedRecalculateDrawData)
                    marking.RecalculateDrawData();

                bool infoView = (cameraInfo.m_layerMask & (3 << 24)) == 0;

                foreach (var drawData in marking.DrawData.Values)
                    drawData.Render(cameraInfo, data, infoView);
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error($"Error while rendering {Type} #{id} marking", error);
            }
        }

        public virtual void Remove(ushort id) => Markings.Remove(id);
        public void Clear()
        {
            SingletonMod<Mod>.Logger.Debug($"{typeof(TypeMarking).Name} {nameof(Clear)}");
            Markings.Clear();
        }
        public void ToXml(XElement config)
        {
            foreach (var marking in Markings.Values.OrderBy(m => m.Id))
            {
                try
                {
                    config.Add(marking.ToXml());
                }
                catch (Exception error)
                {
                    SingletonMod<Mod>.Logger.Error($"Could not save {Type} #{marking.Id} markup", error);
                    MarkingManager.Errors += 1;
                }
            }
        }
        public void FromXml(XElement config, ObjectsMap map, Version version, bool needUpdate)
        {
            var tryGet = MapTryGet(map);
            foreach (var markingConfig in config.Elements(XmlName))
            {
                var id = markingConfig.GetAttrValue<ushort>(nameof(Marking.Id));
                if (id == 0)
                    continue;
                try
                {
                    while (tryGet(id, out var targetId))
                    {
                        id = targetId;
                        if (map.IsSimple)
                            break;
                    }

                    var marking = this[id];

                    marking.FromXml(version, markingConfig, map, needUpdate);
                }
                catch (NotExistItemException error)
                {
                    SingletonMod<Mod>.Logger.Error($"Could not load {error.Type} #{error.Id} marking: {error.Type} not exist");
                    MarkingManager.Errors += 1;
                }
                catch (NotExistEnterException error)
                {
                    SingletonMod<Mod>.Logger.Error($"Could not load {Type} #{id} marking: {error.Type} enter #{error.Id} not exist");
                    MarkingManager.Errors += 1;
                }
                catch (Exception error)
                {
                    SingletonMod<Mod>.Logger.Error($"Could not load {Type} #{id} marking", error);
                    MarkingManager.Errors += 1;
                }
            }
        }

        public IEnumerator<TypeMarking> GetEnumerator() => Markings.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class NodeMarkingManager : MarkingManager<NodeMarking>
    {
        public NodeMarkingManager()
        {
            SingletonMod<Mod>.Logger.Debug("Create node marking manager");
        }

        protected override NodeMarking NewMarking(ushort id) => new NodeMarking(id);
        protected override MarkingType Type => MarkingType.Node;
        protected override string XmlName => NodeMarking.XmlName;
        protected override ObjectsMap.TryGetDelegate<ushort> MapTryGet(ObjectsMap map) => map.TryGetNode;
    }
    public class SegmentMarkingManager : MarkingManager<SegmentMarking>
    {
        public static IntersectionTemplate RemovedMarking { get; private set; }
        public SegmentMarkingManager()
        {
            SingletonMod<Mod>.Logger.Debug("Create segment marking manager");
        }

        protected override SegmentMarking NewMarking(ushort id) => new SegmentMarking(id);
        protected override MarkingType Type => MarkingType.Segment;
        protected override string XmlName => SegmentMarking.XmlName;
        protected override ObjectsMap.TryGetDelegate<ushort> MapTryGet(ObjectsMap map) => map.TryGetSegment;

        public override void Remove(ushort id)
        {
            if (TryGetMarking(id, out var marking))
                RemovedMarking = new IntersectionTemplate(marking);
            else
                RemovedMarking = null;

            base.Remove(id);
        }
    }
}
