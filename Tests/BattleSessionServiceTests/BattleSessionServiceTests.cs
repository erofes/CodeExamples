using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BestHTTP.SignalRCore;
using Cysharp.Threading.Tasks;
using FluentAssertions;
using InanomoUnity.Domain;
using InanomoUnity.View;
using InanomoUnity.View.UI;
using Moq;
using NUnit.Framework;
using Game.Data.Models;
using Game.Data.Models.BattleHub;
using Game.Data.SignalRHub;
using Game.Domain;
using Game.View;
using Game.View.Meta;
using Game.View.Meta.DisconnectedPlayersPopup;
using Tests.DataProviders.Common;
using Tests.DataProviders.Data.Models.BattleHub;
using Tests.DataProviders.Data.Models.BattleHub.BuildStageStarted;
using Tests.DataProviders.Data.Models.EventsHub;
using UnityEngine;

namespace Tests.Domain
{
    public class BattleSessionServiceTests : BattleEventServiceTestFixture<BattleSessionService>
    {
        #region Mocks

        private Mock<IBattleHub> _battleHub;
        private Mock<IPopupService> _popupService;
        private Mock<IGameStateService> _gameStateService;
        private Mock<IAudioService> _audioService;
        private Mock<IMobService> _mobService;
        private Mock<IMapService> _mapService;
        private Mock<ITowerService> _towerService;
        private Mock<IPlayerHudService> _playerHudService;

        #endregion

        #region SetUp

        protected override void Setup()
        {
            InitDataSource(out _battleHub);
            InitService(out _gameStateService);
            InitService(out _popupService);
            InitService(out _audioService);
            InitService(out _mobService);
            InitService(out _mapService);
            InitService(out _towerService);
            InitService<ITowerSummonService>();
            InitService(out _playerHudService);
            Sut = new BattleSessionService();
            base.Setup();
        }

        #endregion

        #region Given

        private void Given_Setup_FindPlayersPopup()
        {
            _popupService
                .Setup(instance => instance.ShowAsync<FindPlayersPopup>(
                    It.IsAny<EmptyPopupModel>(),
                    false,
                    ViewGroup.Default))
                .Returns(UniTask.FromResult<FindPlayersPopup>(null));
        }

        private BattlePlayerSignedConfiguration Given_Setup_BattleConfiguration()
        {
            var battleSessionConfiguration = BattlePlayerSignedConfigurationProvider.Get();
            BattleSessionRepository
                .Setup(instance => instance.CreateWaveBattleSession())
                .Returns(UniTask.FromResult(battleSessionConfiguration));
            BattleSessionRepository
                .Setup(instance => instance.ConnectSession(battleSessionConfiguration))
                .Returns(UniTask.CompletedTask);
            return battleSessionConfiguration;
        }

        private Mock<IHubConnection> Given_Setup_BattleHubConnectionMock()
        {
            var connection = new Mock<IHubConnection>();
            _battleHub
                .SetupGet(instance => instance.Connection)
                .Returns(connection.Object);
            return connection;
        }

        private void Given_Setup_BattleSessionRepository_ReconnectSession()
        {
            BattleSessionRepository
                .Setup(
                    instance => instance
                        .ReconnectSession(It.IsAny<BattlePlayerSignedConfiguration>()))
                .Returns(UniTask.FromResult(BattleStateReconnectDtoProvider.Get()));
        }

        private void Then_ErrorLogAppears()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new Regex("BattleSessionService: ExitWithError"));
        }

        #endregion

        #region StartSession

        [Test]
        public void Test_PlayerIdSet_OnStartAsync()
        {
            // given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleConfiguration();
            Given_Setup_BattleHubConnectionMock();
            // when
            Sut.StartAsync();
            Sut.StartWaveSession();
            // then
            Sut.PlayerId.Should().Be(Guid.Parse("08da8d4a-6d85-4675-a293-baf34a72d893"));
        }

        [Test]
        public void Test_MyTeamIdSet_OnStartAsync()
        {
            // given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleConfiguration();
            Given_Setup_BattleHubConnectionMock();
            // when
            Sut.StartAsync();
            Sut.StartWaveSession();
            // then
            Sut.MyTeamId.Should().Be(Guid.Parse("08dace20-e66d-461a-ad52-a9e76ca886ec"));
        }

        #endregion

        #region BuildStageStartedEvent

        [Test]
        public void Test_BuildStageStartedEvent_StateChanged()
        {
            //given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleConfiguration();
            Given_Setup_BattleHubConnectionMock();
            var buildStageStartedEvent = BuildStageStartedEventProvider.Get();
            //when
            Sut.StartAsync();
            Sut.StartWaveSession();
            Given_RaiseBattleEvent(buildStageStartedEvent);
            //then
            Sut.StateChanged.GetCache().Should().Be(BattleState.Build);
        }

        [Test]
        public void Test_BuildStageStartedEvent_CloseUIObjectAsync_FindPlayersPopup()
        {
            //given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleConfiguration();
            Given_Setup_BattleHubConnectionMock();
            var buildStageStartedEvent = BuildStageStartedEventProvider.Get();
            //when
            Sut.StartAsync();
            Sut.StartWaveSession();
            Given_RaiseBattleEvent(buildStageStartedEvent);
            //then
            _popupService.Verify(
                instance => instance.CloseCurrentUIObject<FindPlayersPopup>(),
                Times.Once());
        }

        [Test]
        public void Test_BuildStageStartedEvent_PlaySoundOneShot()
        {
            //given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleConfiguration();
            Given_Setup_BattleHubConnectionMock();
            //when
            Sut.StartAsync();
            Sut.StartWaveSession();
            Given_RaiseBattleEvent(WaveStageStartedEventProvider.Get());
            Given_RaiseBattleEvent(BuildStageStartedEventProvider.Get());
            //then
            _audioService.Verify(instance => instance.PlaySoundOneShot(SoundSource.SoundId.WaveEnd));
        }

        #endregion /BuildStageStartedEvent

        #region PlayerHandUpdatedEvent

        [Test]
        public void Test_PlayerHandUpdatedEvent()
        {
            //given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleConfiguration();
            Given_Setup_BattleHubConnectionMock();

            var hand = new List<AssetDto>
            {
                new(Guid.NewGuid(), AssetType.Tower, 100, "Card1", "Tower"),
                new(Guid.NewGuid(), AssetType.Spell, 25, "Card2", "Spell"),
                new(Guid.NewGuid(), AssetType.Tower, 67, "Card3", "Tower3")
            };
            var handTowers = new List<TowerAssetDto>
            {
                new(Guid.NewGuid(), AssetType.Tower, 100, "Card1", "Tower", false),
                new(Guid.NewGuid(), AssetType.Tower, 67, "Card3", "Tower3", false)
            };
            var handSpells = new List<SpellAssetDto>
            {
                new(Guid.NewGuid(),
                    AssetType.Spell,
                    25,
                    "Card2",
                    "Spell",
                    10,
                    SpellTargetType.Mob,
                    false,
                    SpellDamageType.Civilization,
                    RarityType.Uncommon,
                    SpellDurationType.Seconds,
                    0m,
                    0m,
                    0m,
                    0m,
                    0m,
                    0m,
                    0,
                    0m,
                    0m),
            };
            var playerHandUpdatedEvent = new PlayerHandUpdatedEvent(hand, handTowers, handSpells);
            //when
            Sut.StartAsync();
            Sut.StartWaveSession();
            Given_RaiseBattleEvent(playerHandUpdatedEvent);
            //then
            Sut.PlayerHandCards.Should().BeEquivalentTo(hand);
        }

        #endregion

        #region PlayerDisconnetedEvent

        [Test]
        public void Test_PlayerDisconnectedEvent()
        {
            //given
            Given_Setup_BattleConfiguration();
            Given_Setup_BattleHubConnectionMock();

            var playerDisconnectedEvent = new PlayerDisconnectedEvent(
                new PlayerPublicDto(Guid.NewGuid())
            );

            _popupService.Setup(instance => instance.ShowAsync<DisconnectedPlayersPopup>(
                It.IsAny<EmptyPopupModel>(), false, ViewGroup.Default));
            //when
            Sut.StartAsync();
            Sut.StartWaveSession();
            Given_RaiseBattleEvent(playerDisconnectedEvent);
            //then
            _popupService.Verify(instance => instance.ShowAsync<DisconnectedPlayersPopup>(
                It.IsAny<EmptyPopupModel>(), false, ViewGroup.Default), Times.Once());
        }

        [Test]
        public void Test_PlayerDisconnectedAndConnectedEvent()
        {
            //given
            Given_Setup_BattleConfiguration();
            Given_Setup_BattleHubConnectionMock();

            var guid = Guid.NewGuid();

            var playerDisconnectedEvent = new PlayerDisconnectedEvent(
                new PlayerPublicDto(guid)
            );

            var playerConnectedEvent = new PlayerConnectedEvent(new PlayerPublicDto(guid));

            _popupService.Setup(instance => instance.ShowAsync<DisconnectedPlayersPopup>(
                It.IsAny<EmptyPopupModel>(), false, ViewGroup.Default));

            _popupService.Setup(instance => instance
                .CloseGroupUIObjects(ViewGroup.Default));
            //when
            Sut.StartAsync();
            Sut.StartWaveSession();
            Given_RaiseBattleEvent(playerDisconnectedEvent);

            Given_RaiseBattleEvent(playerConnectedEvent);
            //then
            _popupService.Verify(instance => instance
                .CloseCurrentUIObject<DisconnectedPlayersPopup>(), Times.Once());
        }

        #endregion

        #region BattleFinishedEvent

        private static List<PlayerRateChangeInfoDto> GetRateChange()
        {
            return new List<PlayerRateChangeInfoDto>
            {
                new(Guid.NewGuid(), 10)
            };
        }

        [Test]
        public void Test_BattleFinishedEvent()
        {
            //given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleConfiguration();
            Given_Setup_BattleHubConnectionMock();
            var winnerTeamId = Guid.NewGuid();
            var rateChange = GetRateChange();
            var battleFinishedEvent = new BattleFinishedEvent(Guid.NewGuid(), rateChange);
            FinishBattlePopupModel resultModel = null;
            _popupService.Setup(instance => instance.ShowAsync<FinishBattlePopup>(
                    It.IsAny<FinishBattlePopupModel>(), false, ViewGroup.Default))
                .Callback<UIModel, bool, int>((model, _, _) => { resultModel = model as FinishBattlePopupModel; });
            //when
            Sut.StartAsync();
            Sut.StartWaveSession();
            Given_RaiseBattleEvent(battleFinishedEvent);
            //then
            var expectedModel = new FinishBattlePopupModel(
                Sut.MyTeamId == winnerTeamId);
            resultModel.Should().BeEquivalentTo(expectedModel);
        }

        [Test]
        public void Test_BattleFinishedEvent_PlaySoundOneShot_Win()
        {
            //given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleConfiguration();
            Given_Setup_BattleHubConnectionMock();
            var rateChange = GetRateChange();
            var battleFinishedEvent = new BattleFinishedEvent(GuidProvider.ConfigurationPlayerTeamId, rateChange);
            //when
            Sut.StartAsync();
            Sut.StartWaveSession();
            Given_RaiseBattleEvent(battleFinishedEvent);
            //then
            _audioService.Verify(instance => instance.PlaySoundOneShot(SoundSource.SoundId.BattleWin));
        }

        [Test]
        public void Test_BattleFinishedEvent_PlaySoundOneShot_Lose()
        {
            //given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleConfiguration();
            Given_Setup_BattleHubConnectionMock();
            var rateChange = GetRateChange();
            var battleFinishedEvent = new BattleFinishedEvent(Guid.NewGuid(), rateChange);
            //when
            Sut.StartAsync();
            Sut.StartWaveSession();
            Given_RaiseBattleEvent(battleFinishedEvent);
            //then
            _audioService.Verify(instance => instance.PlaySoundOneShot(SoundSource.SoundId.BattleDefeat));
        }

        #endregion

        #region BattleCancelledEvent

        [Test]
        public void Test_BattleCancelledEvent_InfoPopup()
        {
            //given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleConfiguration();
            Given_Setup_BattleHubConnectionMock();
            var battleCancelledEvent = new BattleCancelledEvent("cancelled");
            _gameStateService
                .Setup(instance => instance.ChangeGameStateAsync(It.IsAny<IGameState>()));
            //when
            Sut.StartAsync();
            Sut.StartWaveSession();
            Given_RaiseBattleEvent(battleCancelledEvent);
            //then
            _popupService.Verify(instance => instance.ShowAsync<InfoPopup>(
                    It.Is<InfoPopupModel>(popup => popup.Text == "cancelled"),
                    false,
                    ViewGroup.Default),
                Times.Once());
            Then_ErrorLogAppears();
        }

        [Test]
        public void Test_BattleCancelledEvent_PlaySoundOneShot()
        {
            //given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleConfiguration();
            Given_Setup_BattleHubConnectionMock();
            var battleCancelledEvent = new BattleCancelledEvent("cancelled");
            _gameStateService
                .Setup(instance => instance.ChangeGameStateAsync(It.IsAny<IGameState>()));
            //when
            Sut.StartAsync();
            Sut.StartWaveSession();
            Given_RaiseBattleEvent(battleCancelledEvent);
            //then
            _audioService.Verify(instance => instance.PlaySoundOneShot(SoundSource.SoundId.UIFailure), Times.Once());
            Then_ErrorLogAppears();
        }

        #endregion

        #region BattleFailedEvent

        [Test]
        public void Test_BattleFailedEvent_InfoPopup()
        {
            //given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleConfiguration();
            Given_Setup_BattleHubConnectionMock();
            var battleFailedEvent = new BattleFailedEvent("failed");
            _gameStateService
                .Setup(instance => instance.ChangeGameStateAsync(It.IsAny<IGameState>()));
            //when
            Sut.StartAsync();
            Sut.StartWaveSession();
            Given_RaiseBattleEvent(battleFailedEvent);
            //then
            _popupService.Verify(instance => instance.ShowAsync<InfoPopup>(
                    It.Is<InfoPopupModel>(popup => popup.Text == "failed"),
                    false,
                    ViewGroup.Default),
                Times.Once());
            Then_ErrorLogAppears();
        }

        [Test]
        public void Test_BattleFailedEvent_PlaySoundOneShot()
        {
            //given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleConfiguration();
            Given_Setup_BattleHubConnectionMock();
            var battleFailedEvent = new BattleFailedEvent("failed");
            _gameStateService
                .Setup(instance => instance.ChangeGameStateAsync(It.IsAny<IGameState>()));
            //when
            Sut.StartAsync();
            Sut.StartWaveSession();
            Given_RaiseBattleEvent(battleFailedEvent);
            //then
            _audioService.Verify(instance => instance.PlaySoundOneShot(SoundSource.SoundId.UIFailure), Times.Once());
            Then_ErrorLogAppears();
        }

        #endregion

        #region InvalidCommandEvent

        [Test]
        public void Test_InvalidCommandEvent_InfoPopup()
        {
            //given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleConfiguration();
            Given_Setup_BattleHubConnectionMock();
            var invalidCommandEvent = new InvalidCommandEvent("invalid command");
            _gameStateService
                .Setup(instance => instance.ChangeGameStateAsync(It.IsAny<IGameState>()));
            //when
            Sut.StartAsync();
            Sut.StartWaveSession();
            Given_RaiseBattleEvent(invalidCommandEvent);
            //then
            _popupService.Verify(instance => instance.ShowAsync<InfoPopup>(
                    It.Is<InfoPopupModel>(popup => popup.Text == "invalid command"),
                    false,
                    ViewGroup.Default),
                Times.Once());
        }

        [Test]
        public void Test_InvalidCommandEvent_PlaySoundOneShot()
        {
            //given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleConfiguration();
            Given_Setup_BattleHubConnectionMock();
            var invalidCommandEvent = new InvalidCommandEvent("invalid command");
            _gameStateService
                .Setup(instance => instance.ChangeGameStateAsync(It.IsAny<IGameState>()));
            //when
            Sut.StartAsync();
            Sut.StartWaveSession();
            Given_RaiseBattleEvent(invalidCommandEvent);
            //then
            _audioService.Verify(instance => instance.PlaySoundOneShot(SoundSource.SoundId.UIFailure), Times.Once());
        }

        #endregion /InvalidCommandEvent

        #region Reconnection

        [Test]
        public void Test_ShowBattleReconnectionPopup_OnReconnecting()
        {
            // given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleConfiguration();
            var connection = Given_Setup_BattleHubConnectionMock();
            // when
            Sut.StartAsync();
            Sut.StartWaveSession();
            connection
                .Raise(
                    instance => instance.OnReconnecting += null,
                    null, "Reconnecting...");
            // then
            _popupService
                .Verify(
                    instance => instance.ShowAsync<BattleReconnectionPopup>(
                        It.IsAny<EmptyPopupModel>(),
                        false,
                        ViewGroup.Default),
                    Times.Once());
        }

        [Test]
        public void Test_StopMobs_OnReconnecting()
        {
            // given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleConfiguration();
            var connection = Given_Setup_BattleHubConnectionMock();
            // when
            Sut.StartAsync();
            Sut.StartWaveSession();
            connection
                .Raise(
                    instance => instance.OnReconnecting += null,
                    null, "Reconnecting...");
            // then
            _mobService
                .Verify(
                    instance => instance.StopAllMobsMovement(),
                    Times.Once());
        }

        [Test]
        public void Test_ReconnectSessionCall_OnReconnected()
        {
            // given
            Given_Setup_FindPlayersPopup();
            var configuration = Given_Setup_BattleConfiguration();
            var connection = Given_Setup_BattleHubConnectionMock();
            // when
            Sut.StartAsync();
            Sut.StartWaveSession();
            connection
                .Raise(
                    instance => instance.OnReconnected += null,
                    new object[] {null});
            // then
            Then_ErrorLogAppears();
            BattleSessionRepository
                .Verify(
                    instance => instance.ReconnectSession(configuration),
                    Times.Once());
        }

        [Test]
        public void Test_CloseBattleReconnectionPopup_OnReconnected()
        {
            // given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleConfiguration();
            var connection = Given_Setup_BattleHubConnectionMock();
            // when
            Sut.StartAsync();
            Sut.StartWaveSession();
            connection
                .Raise(
                    instance => instance.OnReconnected += null,
                    new object[] {null});
            // then
            Then_ErrorLogAppears();
            _popupService
                .Verify(
                    instance => instance.CloseCurrentUIObject<BattleReconnectionPopup>(),
                    Times.Exactly(2));
        }

        [Test]
        public void Test_OnReconnected_MobServiceRestoreAfterReconnect()
        {
            // given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleSessionRepository_ReconnectSession();
            Given_Setup_BattleConfiguration();
            var connection = Given_Setup_BattleHubConnectionMock();
            // when
            Sut.StartAsync();
            Sut.StartWaveSession();
            connection
                .Raise(
                    instance => instance.OnReconnected += null,
                    new object[] {null});
            // then
            _mobService
                .Verify(
                    instance => instance.UpdateMobs(It.IsAny<IReadOnlyList<MobDto>>()),
                    Times.Once());
        }

        [Test]
        public void Test_OnReconnected_MapServiceRestoreAfterReconnect()
        {
            // given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleSessionRepository_ReconnectSession();
            Given_Setup_BattleConfiguration();
            var connection = Given_Setup_BattleHubConnectionMock();
            // when
            Sut.StartAsync();
            Sut.StartWaveSession();
            connection
                .Raise(
                    instance => instance.OnReconnected += null,
                    new object[] {null});
            // then
            _mapService
                .Verify(
                    instance => instance.UpdateBattleField(It.IsAny<BattleFieldDto>()),
                    Times.Once());
        }

        [Test]
        public void Test_OnReconnected_TowerServiceRestoreAfterReconnect()
        {
            // given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleSessionRepository_ReconnectSession();
            Given_Setup_BattleConfiguration();
            var connection = Given_Setup_BattleHubConnectionMock();
            // when
            Sut.StartAsync();
            Sut.StartWaveSession();
            connection
                .Raise(
                    instance => instance.OnReconnected += null,
                    new object[] {null});
            // then
            _towerService
                .Verify(
                    instance => instance.UpdateTowers(It.IsAny<IReadOnlyList<TowerDto>>()),
                    Times.Once());
        }

        [Test]
        public void Test_OnReconnected_PlayerHudServiceRestoreAfterReconnect()
        {
            // given
            Given_Setup_FindPlayersPopup();
            Given_Setup_BattleSessionRepository_ReconnectSession();
            Given_Setup_BattleConfiguration();
            var connection = Given_Setup_BattleHubConnectionMock();
            // when
            Sut.StartAsync();
            Sut.StartWaveSession();
            connection
                .Raise(
                    instance => instance.OnReconnected += null,
                    new object[] {null});
            // then
            _playerHudService
                .Verify(
                    instance => instance.RestoreAfterReconnect(It.IsAny<BattleStateReconnectDto>()),
                    Times.Once());
        }

        #endregion /Reconnection
    }
}