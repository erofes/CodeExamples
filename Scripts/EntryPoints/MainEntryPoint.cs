using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using InanomoUnity.Data;
using InanomoUnity.Domain;
using InanomoUnity.Domain.EntryPoints;
using InanomoUnity.Domain.ServiceLocator;
using InanomoUnity.View;
using InanomoUnity.View.QLogger;
using Game.Data.JWT;
using Game.Data.Repository;
using Game.Data.SignalRHub;
using Game.Domain.GameStateMachine;
using Game.View;
using UnityEngine;

namespace Game.Domain
{
    public class MainEntryPoint : EntryPoint
    {
        public static bool IsLog;
        public static Color LogColor;
        public static int LogPrio;

        private static IGameStateService GameStateService => ServiceLocator.GetService<IGameStateService>();
        private static ILocalizationService LocalizationService => ServiceLocator.GetService<ILocalizationService>();
        private static IJwtTokenRepository JwtTokenRepository => DataSourceLocator.GetDataSource<IJwtTokenRepository>();

        [SerializeField]
        private GameObject _debugCanvas;

        private void Start()
        {
            StartAsync().Forget();
        }

        protected override UniTask InitializeBeforeBindingsAsync()
        {
            if (IsLog) Log.D(LogColor, LogPrio, 8);

#if !UNITY_EDITOR
            Application.targetFrameRate = Screen.currentResolution.refreshRate;
            QualitySettings.vSyncCount = 0;
#endif

            Instantiate(_debugCanvas);

            if (IsLog) Log.D(LogColor, LogPrio, 8);
            
            ReloadDomain();

            return base.InitializeBeforeBindingsAsync();
        }

        protected override async UniTask OnBindCompleted()
        {
            LocalizationService.ChangeLanguage(GetSystemLanguage());
            // Обработка логина. В будущем сделать рефреш токена через любой http-запрос 

            if (JwtTokenRepository.CanTryConnect())
            {
                await JwtTokenRepository.ConnectToken();
                var playerPrefs = new SecurePlayerPrefs();
                var accessToken = playerPrefs.GetString(PlayerPrefsKey.ACCESS_TOKEN);
                var isSignedOut = JWTUtils.IsJwtExpired(accessToken);
                   
                if (!isSignedOut)
                {
                    var eventHub = DataSourceLocator.GetDataSource<IEventsHub>();
                    await eventHub.Connect(accessToken);
                    await GameStateService.ChangeGameStateAsync(new MainGameState());
                }
            }

            await base.OnBindCompleted();
        }

        private void ReloadDomain()
        {
            if (IsLog) Log.D(LogColor, LogPrio, 8);

            DataSourceLocator.RemoveAllDataSources();
            ServiceLocator.RemoveAllServices();
        }

        protected override UniTask BindDataSources()
        {
            if (IsLog) Log.D(LogColor, LogPrio, 8);
            DataSourceLocator.RemoveAllDataSources();

            DataSources = new Dictionary<Type, IDataSource>()
            {
                { typeof(IJwtTokenRepository), new JwtTokenRepository() },
                { typeof(IPlayerPrefs), new SecurePlayerPrefs() },
                { typeof(IEventsHub), new EventsHub() }
            };

            return base.BindDataSources();
        }

        protected override UniTask BindServices()
        {
            if (IsLog) Log.D(LogColor, LogPrio, 8);

            Services = new Dictionary<Type, IService>
            {
                { typeof(IGameStateService), new GameStateService() },
                { typeof(ILocalizationService), new LocalizationService() },
                { typeof(ISpriteLocatorService), new SpriteLocatorService() },
                { typeof(IAddressableService), new AddressableService() },
                { typeof(IDemoUserDataService), new DemoUserDataService() },
                { typeof(IVibrationService), new VibrationService() },
                { typeof(ILocaleService), new LocaleService() },
                { typeof(IAnalyticsService), new AnalyticsService() },
                { typeof(IMainEventService), new MainEventService() }
            };

            AddToServices(GetServiceKeyValue<IAudioService>());
            AddToServices(GetServiceKeyValue<IScreenService>());
            AddToServices(GetServiceKeyValue<IPopupService>());
            AddToServices(GetServiceKeyValue<ITimeService>());
            AddToServices(GetServiceKeyValue<IAncillaryUIService>());

            return base.BindServices();
        }

        private static Language GetSystemLanguage() => Application.systemLanguage switch
        {
            SystemLanguage.Russian => Language.ru,
            SystemLanguage.ChineseTraditional => Language.zn,
            SystemLanguage.Chinese => Language.zn,
            SystemLanguage.ChineseSimplified => Language.zn,
            _ => Language.en
        };
    }
}