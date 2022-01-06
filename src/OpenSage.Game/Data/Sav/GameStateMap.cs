﻿using System.IO;
using OpenSage.Network;

namespace OpenSage.Data.Sav
{
    internal sealed class GameStateMap : IPersistableObject
    {
        private string _mapPath1;
        private string _mapPath2;
        private GameType _gameType;
        private uint _nextObjectId;
        private uint _nextDrawableId;

        public void Persist(StatePersister reader)
        {
            reader.PersistVersion(2);

            reader.PersistAsciiString("MapPath1", ref _mapPath1);
            reader.PersistAsciiString("MapPath2", ref _mapPath2);
            reader.PersistEnum("GameType", ref _gameType);

            var mapSize = reader.BeginSegment("EmbeddedMap");

            if (reader.SageGame >= SageGame.Bfme)
            {
                var unknown4 = 0u;
                reader.PersistUInt32("Unknown4", ref unknown4);

                var unknown5 = 0u;
                reader.PersistUInt32("Unknown5", ref unknown5);

                mapSize -= 8;
            }

            var game = reader.Game;

            // TODO: Delete this temporary map when ending the game.
            var mapPathInSaveFolder = Path.Combine(
                game.ContentManager.UserDataFileSystem.RootDirectory,
                _mapPath1);
            var saveFolder = Path.GetDirectoryName(mapPathInSaveFolder);
            if (!Directory.Exists(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);
            }

            var mapBytes = (reader.Mode == StatePersistMode.Read)
                ? new byte[mapSize]
                : File.ReadAllBytes(mapPathInSaveFolder);

            reader.PersistSpan(mapBytes);

            if (reader.Mode == StatePersistMode.Read)
            {
                File.WriteAllBytes(mapPathInSaveFolder, mapBytes);
            }

            reader.EndSegment();

            reader.PersistUInt32("NextObjectId", ref _nextObjectId);
            reader.PersistUInt32("NextDrawableId", ref _nextDrawableId);

            if (reader.SageGame >= SageGame.Bfme)
            {
                var unknown6 = false;
                reader.PersistBoolean("Unknown6", ref unknown6);
            }

            if (reader.Mode == StatePersistMode.Read)
            {
                if (_gameType == GameType.Skirmish)
                {
                    game.SkirmishManager = new LocalSkirmishManager(game);

                    reader.PersistObject("SkirmishGameSettings", game.SkirmishManager.Settings);

                    game.SkirmishManager.Settings.MapName = _mapPath1;

                    game.SkirmishManager.HandleStartButtonClickAsync().Wait();

                    game.SkirmishManager.StartGame();
                }
                else
                {
                    game.StartSinglePlayerGame(_mapPath1);
                }

                game.Scene3D.PartitionCellManager.OnNewGame();
            }
            else
            {
                if (_gameType == GameType.Skirmish)
                {
                    reader.PersistObject("SkirmishGameSettings", game.SkirmishManager.Settings);
                }
            }
        }
    }
}
