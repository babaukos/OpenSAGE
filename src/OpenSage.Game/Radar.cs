﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Numerics;
using OpenSage.Content;
using OpenSage.Gui;
using OpenSage.Logic.Object;
using OpenSage.Mathematics;
using Veldrid;

namespace OpenSage
{
    public sealed class Radar : IPersistableObject
    {
        private readonly Scene3D _scene;
        private readonly Texture _miniMapTexture;

        private readonly RadarItemCollection _visibleItems;
        private readonly RadarItemCollection _hiddenItems;

        private readonly List<RadarEvent> _radarEvents;

        private bool _unknown1;
        private uint _unknown2;
        private uint _unknown3;

        internal Radar(Scene3D scene, AssetStore assetStore, string mapPath)
        {
            _scene = scene;

            if (mapPath != null)
            {
                var basePath = Path.Combine(Path.GetDirectoryName(mapPath), Path.GetFileNameWithoutExtension(mapPath));

                // Minimap images drawn by an artist
                var mapArtPath = basePath + "_art.tga";
                _miniMapTexture = assetStore.GuiTextures.GetByName(mapArtPath)?.Texture;

                if (_miniMapTexture == null)
                {
                    // Fallback to minimap images generated by WorldBuilder
                    mapArtPath = basePath + ".tga";
                    _miniMapTexture = assetStore.GuiTextures.GetByName(mapArtPath)?.Texture;
                }
            }

            _visibleItems = new RadarItemCollection();
            _hiddenItems = new RadarItemCollection();

            _radarEvents = new List<RadarEvent>();

            // TODO: Bridges
            // TODO: Fog of war / shroud
        }

        public Vector3 RadarToWorldSpace(Point2D mousePosition, in Mathematics.Rectangle destinationRectangle)
        {
            var miniMapTransform = RectangleF.CalculateTransformForRectangleFittingAspectRatio(
                new RectangleF(0, 0, _miniMapTexture.Width, _miniMapTexture.Height),
                new SizeF(_miniMapTexture.Width, _miniMapTexture.Height),
                destinationRectangle.Size);

            Matrix3x2.Invert(miniMapTransform, out var miniMapTransformInverse);

            // Transform by inverse of miniMapTransform
            var position2D = Vector2.Transform(
                mousePosition.ToVector2(),
                miniMapTransformInverse);

            // Divide by minimap texture size.
            position2D.X /= _miniMapTexture.Width;
            position2D.Y /= _miniMapTexture.Height;

            // Multiply position by map size.
            position2D.X *= _scene.Terrain.HeightMap.Width;
            position2D.Y *= _scene.Terrain.HeightMap.Height;

            // Invert y.
            position2D.Y = _scene.Terrain.HeightMap.Height - position2D.Y;

            return _scene.Terrain.HeightMap.GetPosition((int) position2D.X, (int) position2D.Y);
        }

        public void AddGameObject(GameObject gameObject, uint objectId)
        {
            switch (gameObject.Definition.RadarPriority)
            {
                case RadarPriority.Invalid:
                case RadarPriority.NotOnRadar:
                    return;
            }

            // TODO: Check whether this object is visible to the local player.
            var isVisibleToLocalPlayer = true;

            var items = isVisibleToLocalPlayer
                ? _visibleItems
                : _hiddenItems;

            items.Add(new RadarItem
            {
                ObjectId = objectId,
                Color = gameObject.Owner.Color.ToColorRgba()
            });
        }

        public void RemoveGameObject(GameObject gameObject)
        {
            var objectId = (uint) _scene.GameObjects.GetObjectId(gameObject);

            _visibleItems.Remove(objectId);
            _hiddenItems.Remove(objectId);
        }

        // TODO: Update item color when it changes owner - or remove/add.

        public void Draw(DrawingContext2D drawingContext, in Mathematics.Rectangle destinationRectangle)
        {
            // TODO: Don't draw minimap if player doesn't have radar.

            if (_miniMapTexture == null)
            {
                return;
            }

            var fittedRectangle = RectangleF.CalculateRectangleFittingAspectRatio(
                new RectangleF(0, 0, _miniMapTexture.Width, _miniMapTexture.Height),
                new SizeF(_miniMapTexture.Width, _miniMapTexture.Height),
                destinationRectangle.Size);

            DrawRadarMinimap(drawingContext, fittedRectangle);

            var objectTransform = RectangleF.CalculateTransformForRectangleFittingAspectRatio(
                new RectangleF(0, 0, _miniMapTexture.Width, _miniMapTexture.Height),
                new SizeF(_miniMapTexture.Width, _miniMapTexture.Height),
                destinationRectangle.Size);

            DrawRadarOverlay(drawingContext, objectTransform);
        }

        public void DrawRadarMinimap(DrawingContext2D drawingContext, in Mathematics.Rectangle rectangle, bool flip = false)
        {
            drawingContext.DrawImage(_miniMapTexture, null, rectangle, flip);
        }

        public void DrawRadarOverlay(DrawingContext2D drawingContext, in Mathematics.Rectangle rectangle)
        {
            var rectF = rectangle.ToRectangleF();
            var objectTransform = RectangleF.CalculateTransformForRectangleFittingAspectRatio(
                rectF,
                rectF.Size,
                rectangle.Size);
            DrawRadarOverlay(drawingContext, objectTransform);
        }

        public void DrawRadarOverlay(DrawingContext2D drawingContext, in Matrix3x2 transform)
        {
            foreach (var item in _visibleItems)
            {
                DrawRadarItem(item, drawingContext, transform);
            }

            DrawCameraFrustum(drawingContext, transform);
        }

        private void DrawRadarItem(
            RadarItem item,
            DrawingContext2D drawingContext,
            in Matrix3x2 miniMapTransform)
        {
            // TODO: Use RadarPriority to decide what gets shown when there are multiple
            // things in the same radar position.

            var gameObject = _scene.GameObjects.GetObjectById(item.ObjectId);
            var gameObjectPosition = gameObject.Translation;

            var radarPosition = WorldToRadarSpace(gameObjectPosition, miniMapTransform);
            if (radarPosition == null)
            {
                return;
            }

            // TODO: Use actual object geometry.
            var gameObjectRectangle = new Mathematics.Rectangle(
                (int) radarPosition.Value.X,
                (int) radarPosition.Value.Y,
                2, 2);

            drawingContext.FillRectangle(
                gameObjectRectangle,
                item.Color.ToColorRgbaF());
        }

        private Vector2? WorldToRadarSpace(in Vector3 worldPosition, in Matrix3x2 miniMapTransform)
        {
            var position2D = _scene.Terrain.HeightMap.GetHeightMapPosition(worldPosition);

            // Invert y.
            position2D.Y = _scene.Terrain.HeightMap.Height - position2D.Y;

            // Divide position by map size.
            position2D.X /= _scene.Terrain.HeightMap.Width;
            position2D.Y /= _scene.Terrain.HeightMap.Height;

            // Multiply by minimap texture size.
            position2D.X *= _miniMapTexture.Width;
            position2D.Y *= _miniMapTexture.Height;

            // Transform by minimapTransform
            return Vector2.Transform(position2D, miniMapTransform);
        }

        private void DrawCameraFrustum(DrawingContext2D drawingContext, Matrix3x2 miniMapTransform)
        {
            // Create rays from camera position through each corner of the near plane.
            var frustumCorners = _scene.Camera.BoundingFrustum.Corners;
            var cameraPosition = _scene.Camera.Position;

            var groundPlane = new Plane(Vector3.UnitZ, 0);

            Vector3? GetGroundIntersectionPoint(int index)
            {
                var ray = new Ray(cameraPosition, Vector3.Normalize(frustumCorners[index] - cameraPosition));

                var distance = ray.Intersects(groundPlane);
                if (distance == null)
                {
                    return null;
                }

                return ray.Position + ray.Direction * distance.Value;
            }

            var terrain0 = GetGroundIntersectionPoint(0);
            var terrain1 = GetGroundIntersectionPoint(1);
            var terrain2 = GetGroundIntersectionPoint(2);
            var terrain3 = GetGroundIntersectionPoint(3);

            if (terrain0 == null || terrain1 == null || terrain2 == null || terrain3 == null)
            {
                return;
            }

            void DrawFrustumLine(in Vector3 v0, in Vector3 v1)
            {
                var v0Radar = WorldToRadarSpace(v0, miniMapTransform);
                var v1Radar = WorldToRadarSpace(v1, miniMapTransform);

                if (v0Radar == null || v1Radar == null)
                {
                    return;
                }

                // TODO: Clip lines to destination rectangle.

                drawingContext.DrawLine(
                    new Line2D(v0Radar.Value, v1Radar.Value),
                    2,
                    new ColorRgbaF(1, 1, 0, 1));
            }

            DrawFrustumLine(terrain0.Value, terrain1.Value);
            DrawFrustumLine(terrain1.Value, terrain2.Value);
            DrawFrustumLine(terrain2.Value, terrain3.Value);
            DrawFrustumLine(terrain3.Value, terrain0.Value);
        }

        public void Persist(StatePersister reader)
        {
            reader.PersistVersion(1);

            reader.SkipUnknownBytes(1);

            reader.PersistBoolean("Unknown1", ref _unknown1);
            reader.PersistObject("VisibleItems", _visibleItems);
            reader.PersistObject("HiddenItems", _hiddenItems);

            reader.PersistList("RadarEvents", _radarEvents, static (StatePersister persister, ref RadarEvent item) =>
            {
                item ??= new RadarEvent();
                persister.PersistObjectValue(item);
            });

            reader.PersistUInt32("Unknown2", ref _unknown2);
            reader.PersistUInt32("Unknown3", ref _unknown3);
        }
    }

    internal sealed class RadarItemCollection : KeyedCollection<uint, RadarItem>, IPersistableObject
    {
        public void Persist(StatePersister reader)
        {
            reader.PersistVersion(1);

            var count = (ushort) Count;
            reader.PersistUInt16("Count", ref count);

            reader.BeginArray("Items");
            if (reader.Mode == StatePersistMode.Read)
            {
                Clear();

                for (var i = 0; i < count; i++)
                {
                    var item = new RadarItem();
                    reader.PersistObjectValue(item);
                    Add(item);
                }
            }
            else
            {
                foreach (var item in this)
                {
                    reader.PersistObjectValue(item);
                }
            }
            reader.EndArray();
        }

        protected override uint GetKeyForItem(RadarItem item) => item.ObjectId;
    }

    internal sealed class RadarItem : IPersistableObject
    {
        public uint ObjectId;
        public ColorRgba Color;

        public void Persist(StatePersister reader)
        {
            reader.PersistVersion(1);

            reader.PersistObjectID("ObjectId", ref ObjectId);
            reader.PersistColorRgba("Color", ref Color);
        }
    }

    internal sealed class RadarEvent : IPersistableObject
    {
        public RadarEventType Type;
        public Vector3 Position;

        private bool _unknown1;
        private uint _unknown2;
        private uint _unknown3;
        private uint _unknown4;
        private ColorRgba _color1;
        private ColorRgba _color2;
        private uint _unknown5;
        private uint _unknown6;
        private bool _unknown7;

        public void Persist(StatePersister reader)
        {
            reader.PersistEnum("Type", ref Type);
            reader.PersistBoolean("Unknown1", ref _unknown1);
            reader.PersistUInt32("Unknown2", ref _unknown2);
            reader.PersistUInt32("Unknown3", ref _unknown3);
            reader.PersistUInt32("Unknown4", ref _unknown4);
            reader.PersistColorRgbaInt("Color1", ref _color1);
            reader.PersistColorRgbaInt("Color2", ref _color2);
            reader.PersistVector3("Position", ref Position);
            reader.PersistUInt32("Unknown5", ref _unknown5);
            reader.PersistUInt32("Unknown6", ref _unknown6);
            reader.PersistBoolean("Unknown7", ref _unknown7);
        }
    }

    internal enum RadarEventType
    {
        Invalid = 0,
        Construction = 1,
        Upgrade = 2,
        UnderAttack = 3,
        Information = 4,

        StealUnitDiscovered = 8,
        UnitLost = 10,
    }
}
