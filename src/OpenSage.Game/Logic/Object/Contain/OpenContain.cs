﻿using OpenSage.Data.Ini;
using OpenSage.Mathematics;

namespace OpenSage.Logic.Object
{
    public abstract class OpenContainModule : UpdateModule
    {
        internal override void Load(SaveFileReader reader)
        {
            reader.ReadVersion(1);

            base.Load(reader);

            var numObjectsInside = reader.ReadUInt32();
            for (var i = 0; i < numObjectsInside; i++)
            {
                var objectId = reader.ReadObjectID();
            }

            reader.SkipUnknownBytes(2);

            var frameSomething = reader.ReadUInt32();

            var frameSomething2 = reader.ReadUInt32();

            reader.SkipUnknownBytes(8);

            var modelConditionFlags = reader.ReadBitArray<ModelConditionFlag>();

            // Where does the 32 come from?
            for (var i = 0; i < 32; i++)
            {
                var someTransform = reader.ReadMatrix4x3Transposed();
            }

            var unknown6 = reader.ReadInt32();
            if (unknown6 != -1)
            {
                throw new InvalidStateException();
            }

            var nextFirePointIndex = reader.ReadUInt32();
            var numFirePoints = reader.ReadUInt32();

            var hasNoFirePoints = reader.ReadBoolean();

            reader.SkipUnknownBytes(13);

            var unknown9Count = reader.ReadUInt16();
            for (var i = 0; i < unknown9Count; i++)
            {
                var objectId = reader.ReadObjectID();

                var unknown10 = reader.ReadInt32();
            }

            var unknown8 = reader.ReadInt32();
        }
    }

    public abstract class OpenContainModuleData : UpdateModuleData
    {
        internal static readonly IniParseTable<OpenContainModuleData> FieldParseTable = new IniParseTable<OpenContainModuleData>
        {
            { "AllowInsideKindOf", (parser, x) => x.AllowInsideKindOf = parser.ParseEnumBitArray<ObjectKinds>() },
            { "ForbidInsideKindOf", (parser, x) => x.ForbidInsideKindOf = parser.ParseEnumBitArray<ObjectKinds>() },
            { "ContainMax", (parser, x) => x.ContainMax = parser.ParseInteger() },
            { "EnterSound", (parser, x) => x.EnterSound = parser.ParseAssetReference() },
            { "ExitSound", (parser, x) => x.ExitSound = parser.ParseAssetReference() },
            { "DamagePercentToUnits", (parser, x) => x.DamagePercentToUnits = parser.ParsePercentage() },
            { "PassengersInTurret", (parser, x) => x.PassengersInTurret = parser.ParseBoolean() },
            { "NumberOfExitPaths", (parser, x) => x.NumberOfExitPaths = parser.ParseInteger() },
            { "AllowAlliesInside", (parser, x) => x.AllowAlliesInside = parser.ParseBoolean() },
            { "AllowNeutralInside", (parser, x) => x.AllowNeutralInside = parser.ParseBoolean() },
            { "AllowEnemiesInside", (parser, x) => x.AllowEnemiesInside = parser.ParseBoolean() },
            { "ShouldDrawPips", (parser, x) => x.ShouldDrawPips = parser.ParseBoolean() },
        };

        public BitArray<ObjectKinds> AllowInsideKindOf { get; private set; }
        public BitArray<ObjectKinds> ForbidInsideKindOf { get; private set; }
        public int ContainMax { get; private set; }
        public string EnterSound { get; private set; }
        public string ExitSound { get; private set; }
        public Percentage DamagePercentToUnits { get; private set; }
        public bool PassengersInTurret { get; private set; }
        public int NumberOfExitPaths { get; private set; }
        public bool AllowAlliesInside { get; private set; }
        public bool AllowNeutralInside { get; private set; }
        public bool AllowEnemiesInside { get; private set; }
        public bool ShouldDrawPips { get; private set; }
    }
}
