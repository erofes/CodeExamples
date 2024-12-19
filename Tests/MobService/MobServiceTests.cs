using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FluentAssertions;
using InanomoUnity.Common;
using InanomoUnity.View;
using InanomoUnity.View.Utils;
using Moq;
using NUnit.Framework;
using Game.Data.Models;
using Game.Data.Models.BattleHub;
using Game.View;
using Game.View.Core;
using Game.View.Meta;
using Tests.DataProviders.Data.Models.BattleHub;
using Tests.DataProviders.View.Core.Map.Mob;
using UnityEngine;
using UnityEngine.TestTools;
using AudioSource = Game.View.AudioSource;

namespace Tests.MonoServices
{
    public class MobServiceTests : BattleEventServiceTestFixture<MobService>
    {
        private const string MOB_PREFAB_PREFIX = "Mob/";

        #region Mocks

        private Mock<IPopupService> _popupService;
        private Mock<IAudioService> _audioService;
        private Mock<IMapService> _mapService;
        
        #endregion /Mocks

        #region SetUp

        [SetUp]
        protected override void Setup()
        {
            base.Setup();

            InitService<IMobService>();
            InitService<IBehaviourEffectsService>();
            InitService(out _popupService);
            InitService(out _audioService);
            InitService(out _mapService);

            Sut = Parent.AddComponent<MobService>();
        }

        [TearDown]
        protected override void TearDown()
        {
            Sut.Dispose();
            base.TearDown();
        }

        #endregion /SetUp

        #region Given

        private void Given_MapServiceMiniMapSizeChanged()
        {
            _mapService
                .Setup(instance => instance.MiniMapSizeChanged)
                .Returns(new RxAction<float>());
        }

        private (MobDto mobDto, MobCreatedEvent mobCreatedEvent) Given_GenericSetupMobAndMap()
        {
            Given_MapServiceMiniMapSizeChanged();
            var mobDto = MobDtoProvider.Get();
            var mobCreatedEvent = new MobCreatedEvent(mobDto);
            var mobName = MOB_PREFAB_PREFIX + mobDto.Type;
            Given_AddressableService_GetGameObject(mobName, typeof(MobBehaviour));
            return (mobDto, mobCreatedEvent);
        }

        #endregion /Given

        #region StartAsync

        [Test]
        public void Test_BattleEvent_StartAsync()
        {
            // given
            var battleCancelledEvent = new WaveStageStartedEvent();
            var count = 0;

            void BattleEvent(IBattleEvent battleEvent)
            {
                count++;
            }

            // when
            Sut.StartAsync().Forget();
            BattleSessionRepository.Object.BattleEvent += BattleEvent;
            Given_RaiseBattleEvent(battleCancelledEvent);
            // then
            count.Should().Be(1);
            BattleSessionRepository.Object.BattleEvent -= BattleEvent;
        }

        #endregion

        #region MockSetup

        private void MockSetup_PopupService_ShowMobPopup()
        {
            UniTask<MobPopup> GetResult()
            {
                var go = new GameObject();
                var component = go.AddComponent<MobPopup>();
                return UniTask.FromResult(component);
            }

            _popupService
                .Setup(instance =>
                    instance.ShowAsync<MobPopup>(
                        It.IsAny<MobPopupModel>(), 
                        false, 
                        ViewGroup.Default))
                .Returns(GetResult);
        }

        #endregion /MockSetup

        #region BattleEvent

        [Test]
        public void Test_OnMobCreatedEvent_MobAdded()
        {
            // given
            var (_, mobCreatedEvent) = Given_GenericSetupMobAndMap();
            // when
            Sut.StartAsync().Forget();
            Given_RaiseBattleEvent(mobCreatedEvent);
            // then
            var behaviour = Object.FindObjectOfType<MobBehaviour>();
            var expectedMob = MobProvider.Get(behaviour);
            Sut.TryGetMob(expectedMob.Id, out var mob);
            mob.Should().BeEquivalentTo(expectedMob);
        }

        [Test]
        public void Test_OnMobMovementEvent_PositionChangedToStartOfPath()
        {
            // given
            var (mobDto, mobCreatedEvent) = Given_GenericSetupMobAndMap();
            var mobMovementEvent = new MobMovementEvent(
                mobDto.Id,
                new List<DirectionChange>
                {
                    new(new Point(0f, 1f, 2f), new Vector3(1f, 0f, 0f)),
                    new(new Point(5f, 1f, 2f), new Vector3(0f, 1f, 0f)),
                    new(new Point(5f, 5f, 2f), null)
                },
                10f);
            var expectedPosition = new Vector3(0f, 2f, 1f);
            // when
            Sut.StartAsync().Forget();
            Given_RaiseBattleEvent(mobCreatedEvent);
            Given_RaiseBattleEvent(mobMovementEvent);
            // then
            Sut.TryGetMob(mobDto.Id, out var mob);
            mob.State.Position.Should().Be(expectedPosition);
        }

        [Test]
        public void Test_OnMobHpReducedEvent_MobHpChanged()
        {
            // given
            var (mobDto, mobCreatedEvent) = Given_GenericSetupMobAndMap();
            var mobHpReducedEvent = new MobHpReducedEvent(
                mobDto.Id,
                76f,
                -1f);
            // when
            Sut.StartAsync().Forget();
            Given_RaiseBattleEvent(mobCreatedEvent);
            Given_RaiseBattleEvent(mobHpReducedEvent);
            // then
            Sut.TryGetMob(mobDto.Id, out var mob);
            mob.State.Health.Should().Be(76f);
        }

        [Test]
        public void Test_OnMobHpReducedEvent_PlayMobDamagedSound()
        {
            // given
            var (mobDto, mobCreatedEvent) = Given_GenericSetupMobAndMap();
            var mobHpReducedEvent = new MobHpReducedEvent(
                mobDto.Id,
                76f,
                -1f);
            // when
            Sut.StartAsync().Forget();
            Given_RaiseBattleEvent(mobCreatedEvent);
            Given_RaiseBattleEvent(mobHpReducedEvent);
            // then
            _audioService.Verify(
                instance => instance.Play3DSoundOneShot(
                    It.IsInRange(SoundSource.SoundId.MobDamaged1, SoundSource.SoundId.MobDamaged3, Range.Inclusive),
                    It.IsAny<AudioSource>()),
                Times.Once());
        }

        [Test]
        public void Test_OnMobKilledEvent_PlayMobDeathSound()
        {
            // given
            var (mobDto, mobCreatedEvent) = Given_GenericSetupMobAndMap();
            var mobKilledEvent = new MobKilledEvent(mobDto.Id);
            // when
            Sut.StartAsync().Forget();
            Given_RaiseBattleEvent(mobCreatedEvent);
            Given_RaiseBattleEvent(mobKilledEvent);
            // then
            _audioService.Verify(
                instance => instance.Play3DSoundOneShot(SoundSource.SoundId.MobDeath,
                    It.IsAny<AudioSource>()),
                Times.Once());
        }

        [Test]
        public void Test_OnMobKilledEvent_MobIsRemoved()
        {
            // given
            var (mobDto, mobCreatedEvent) = Given_GenericSetupMobAndMap();
            var mobKilledEvent = new MobKilledEvent(mobDto.Id);
            // when
            Sut.StartAsync().Forget();
            Given_RaiseBattleEvent(mobCreatedEvent);
            Given_RaiseBattleEvent(mobKilledEvent);
            // then
            Sut.TryGetMob(mobDto.Id, out var mob);
            mob.Should().BeNull();
        }

        [Test]
        public void Test_MobHitAtTowerEvent_MobDontHaveAnimatorToPlayAttack()
        {
            // given
            var (_, mobCreatedEvent) = Given_GenericSetupMobAndMap();
            var mobKilledEvent = new MobHitAtTowerEvent(MobHitDtoProvider.Get());
            // when
            Sut.StartAsync().Forget();
            Given_RaiseBattleEvent(mobCreatedEvent);
            Given_RaiseBattleEvent(mobKilledEvent);
            // then
            var behaviour = Object.FindObjectOfType<MobBehaviour>();
            var animator = behaviour.GetComponent<Animator>();
            animator.IsNullOrDead().Should().Be(true);
        }

        #endregion

        #region OnMobClicked

        [UnityTest]
        public IEnumerator Test_OnMobClicked_ShowMobPopup()
        {
            // given
            var (mobDto, mobCreatedEvent) = Given_GenericSetupMobAndMap();
            MockSetup_PopupService_ShowMobPopup();
            // when
            var awaiter = Sut.StartAsync().GetAwaiter();
            while (!awaiter.IsCompleted)
                yield return null;
            Given_RaiseBattleEvent(mobCreatedEvent);
            Sut.TryGetMob(mobDto.Id, out var mob);
            Sut.OnMobClicked(mob);
            // then
            _popupService
                .Verify(
                    instance => instance.ShowAsync<MobPopup>(
                        It.IsAny<MobPopupModel>(), 
                        false, 
                        ViewGroup.Default),
                    Times.Once());
        }

        #endregion /OnMobClicked
    }
}