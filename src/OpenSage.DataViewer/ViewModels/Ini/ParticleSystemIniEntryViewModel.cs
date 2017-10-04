﻿using System.Numerics;
using OpenSage.Data.Ini;
using OpenSage.Graphics.Cameras;
using OpenSage.Graphics.Cameras.Controllers;
using OpenSage.Graphics.ParticleSystems;

namespace OpenSage.DataViewer.ViewModels.Ini
{
    public sealed class ParticleSystemIniEntryViewModel : IniEntryViewModel, IGameViewModel
    {
        private readonly ParticleSystemDefinition _definition;

        public override string GroupName => "Particle Systems";

        public override string Name => _definition.Name;

        public ParticleSystemIniEntryViewModel(ParticleSystemDefinition definition)
        {
            _definition = definition;
        }

        void IGameViewModel.LoadScene(Game game)
        {
            var scene = new Scene();

            var cameraEntity = new Entity();
            cameraEntity.AddComponent(new PerspectiveCameraComponent { FieldOfView = 70 });
            cameraEntity.AddComponent(new ArcballCameraController(Vector3.Zero, 200));
            scene.Entities.Add(cameraEntity);

            var particleSystemEntity = new Entity();
            particleSystemEntity.Components.Add(new ParticleSystem(_definition));
            scene.Entities.Add(particleSystemEntity);

            game.Scene = scene;
        }
    }
}