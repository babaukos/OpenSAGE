﻿using System;
using System.Collections.Generic;
using OpenSage.Content;
using OpenSage.Data.Map;
using OpenSage.Logic.Object;

namespace OpenSage.Logic
{
    internal sealed class GameLogic : IPersistableObject
    {
        private readonly Scene3D _scene3D;
        private readonly ObjectDefinitionLookupTable _objectDefinitionLookupTable;
        private readonly List<GameObject> _objects = new();
        private readonly Dictionary<string, ObjectBuildableType> _techTreeOverrides = new();
        private readonly List<string> _commandSetNamesPrefixedWithCommandButtonIndex = new();

        private uint _currentFrame;

        private uint _rankLevelLimit;

        internal uint NextObjectId;

        public GameLogic(Scene3D scene3D)
        {
            _scene3D = scene3D;
            _objectDefinitionLookupTable = new ObjectDefinitionLookupTable(scene3D.AssetLoadContext.AssetStore.ObjectDefinitions);
        }

        public GameObject GetObjectById(uint id)
        {
            return _objects[(int)id];
        }

        public void Persist(StatePersister reader)
        {
            reader.PersistVersion(9);

            reader.PersistUInt32("CurrentFrame", ref _currentFrame);
            reader.PersistObject("ObjectDefinitions", _objectDefinitionLookupTable);

            var gameObjectsCount = (uint)_objects.Count;
            reader.PersistUInt32("ObjectsCount", ref gameObjectsCount);

            reader.BeginArray("Objects");
            if (reader.Mode == StatePersistMode.Read)
            {
                _objects.Clear();
                _objects.Capacity = (int)gameObjectsCount;

                for (var i = 0; i < gameObjectsCount; i++)
                {
                    reader.BeginObject();

                    ushort objectDefinitionId = 0;
                    reader.PersistUInt16("ObjectDefinitionId", ref objectDefinitionId);
                    var objectDefinition = _objectDefinitionLookupTable.GetById(objectDefinitionId);

                    var gameObject = _scene3D.GameObjects.Add(objectDefinition, _scene3D.LocalPlayer);

                    reader.BeginSegment(objectDefinition.Name);

                    reader.PersistObject("Object", gameObject);

                    while (_objects.Count <= gameObject.ID)
                    {
                        _objects.Add(null);
                    }
                    _objects[(int)gameObject.ID] = gameObject;

                    reader.EndSegment();

                    reader.EndObject();
                }
            }
            else
            {
                foreach (var gameObject in _objects)
                {
                    if (gameObject == null)
                    {
                        continue;
                    }

                    reader.BeginObject();

                    var objectDefinitionId = _objectDefinitionLookupTable.GetId(gameObject.Definition);
                    reader.PersistUInt16("ObjectDefinitionId", ref objectDefinitionId);

                    reader.BeginSegment(gameObject.Definition.Name);

                    reader.PersistObject("Object", gameObject);

                    reader.EndSegment();

                    reader.EndObject();
                }
            }
            reader.EndArray();

            // Don't know why this is duplicated here. It's also loaded by a top-level .sav chunk.
            reader.PersistObject("CampaignManager", reader.Game.CampaignManager);

            var unknown1 = true;
            reader.PersistBoolean("Unknown1", ref unknown1);
            if (!unknown1)
            {
                throw new InvalidStateException();
            }

            reader.SkipUnknownBytes(2);

            var unknown1_1 = true;
            reader.PersistBoolean("Unknown1_1", ref unknown1_1);
            if (!unknown1_1)
            {
                throw new InvalidStateException();
            }

            reader.PersistArrayWithUInt32Length("PolygonTriggers", _scene3D.MapFile.PolygonTriggers.Triggers, static (StatePersister persister, ref PolygonTrigger item) =>
            {
                persister.BeginObject();

                var id = item.UniqueId;
                persister.PersistUInt32("Id", ref id);

                if (id != item.UniqueId)
                {
                    throw new InvalidStateException();
                }

                persister.PersistObject("Value", item);

                persister.EndObject();
            });

            reader.PersistUInt32("RankLevelLimit", ref _rankLevelLimit);

            reader.SkipUnknownBytes(4);

            reader.BeginArray("TechTreeOverrides");
            if (reader.Mode == StatePersistMode.Read)
            {
                while (true)
                {
                    reader.BeginObject();

                    var objectDefinitionName = "";
                    reader.PersistAsciiString("ObjectDefinitionName", ref objectDefinitionName);

                    if (objectDefinitionName == "")
                    {
                        reader.EndObject();
                        break;
                    }

                    ObjectBuildableType buildableStatus = default;
                    reader.PersistEnum("BuildableStatus", ref buildableStatus);

                    _techTreeOverrides.Add(
                        objectDefinitionName,
                        buildableStatus);

                    reader.EndObject();
                }
            }
            else
            {
                foreach (var techTreeOverride in _techTreeOverrides)
                {
                    reader.BeginObject();

                    var objectDefinitionName = techTreeOverride.Key;
                    reader.PersistAsciiString("ObjectDefinitionName", ref objectDefinitionName);

                    var buildableStatus = techTreeOverride.Value;
                    reader.PersistEnum("BuildableStatus", ref buildableStatus);

                    reader.EndObject();
                }

                reader.BeginObject();

                var endString = "";
                reader.PersistAsciiString("ObjectDefinitionName", ref endString);

                reader.EndObject();
            }
            reader.EndArray();

            var unknownBool1 = true;
            reader.PersistBoolean("UnknownBool1", ref unknownBool1);
            if (!unknownBool1)
            {
                throw new InvalidStateException();
            }

            var unknownBool2 = true;
            reader.PersistBoolean("UnknownBool2", ref unknownBool2);
            if (!unknownBool2)
            {
                throw new InvalidStateException();
            }

            var unknownBool3 = true;
            reader.PersistBoolean("UnknownBool3", ref unknownBool3);
            if (!unknownBool3)
            {
                throw new InvalidStateException();
            }

            var unknown3 = uint.MaxValue;
            reader.PersistUInt32("Unknown3", ref unknown3);
            if (unknown3 != uint.MaxValue)
            {
                throw new InvalidStateException();
            }

            // Command button overrides
            reader.BeginArray("CommandButtonOverrides");
            if (reader.Mode == StatePersistMode.Read)
            {
                while (true)
                {
                    var commandSetNamePrefixedWithCommandButtonIndex = "";
                    reader.PersistAsciiStringValue(ref commandSetNamePrefixedWithCommandButtonIndex);

                    if (commandSetNamePrefixedWithCommandButtonIndex == "")
                    {
                        break;
                    }

                    _commandSetNamesPrefixedWithCommandButtonIndex.Add(commandSetNamePrefixedWithCommandButtonIndex);

                    reader.SkipUnknownBytes(1);
                }
            }
            else
            {
                foreach (var commandSetName in _commandSetNamesPrefixedWithCommandButtonIndex)
                {
                    var commandSetNameCopy = commandSetName;
                    reader.PersistAsciiStringValue(ref commandSetNameCopy);

                    reader.SkipUnknownBytes(1);
                }

                var endString = "";
                reader.PersistAsciiStringValue(ref endString);
            }
            reader.EndArray();

            reader.SkipUnknownBytes(4);
        }
    }

    internal sealed class ObjectDefinitionLookupTable : IPersistableObject
    {
        private readonly ScopedAssetCollection<ObjectDefinition> _objectDefinitions;
        private readonly List<ObjectDefinitionLookupEntry> _entries = new();

        public ObjectDefinitionLookupTable(ScopedAssetCollection<ObjectDefinition> objectDefinitions)
        {
            _objectDefinitions = objectDefinitions;
        }

        public ObjectDefinition GetById(ushort id)
        {
            foreach (var entry in _entries)
            {
                if (entry.Id == id)
                {
                    return _objectDefinitions.GetByName(entry.Name);
                }
            }

            throw new InvalidOperationException();
        }

        public ushort GetId(ObjectDefinition objectDefinition)
        {
            foreach (var entry in _entries)
            {
                if (entry.Name == objectDefinition.Name)
                {
                    return entry.Id;
                }
            }

            var newEntry = new ObjectDefinitionLookupEntry
            {
                Name = objectDefinition.Name,
                Id = (ushort)_entries.Count
            };

            _entries.Add(newEntry);

            return newEntry.Id;
        }

        public void Persist(StatePersister reader)
        {
            reader.PersistVersion(1);

            reader.PersistListWithUInt32Count("Entries", _entries, static (StatePersister persister, ref ObjectDefinitionLookupEntry item) =>
            {
                persister.PersistObjectValue(ref item);
            });
        }

        private struct ObjectDefinitionLookupEntry : IPersistableObject
        {
            public string Name;
            public ushort Id;

            public void Persist(StatePersister persister)
            {
                persister.PersistAsciiString("Name", ref Name);
                persister.PersistUInt16("Id", ref Id);
            }
        }
    }
}
