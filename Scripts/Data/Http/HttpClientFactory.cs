using System;
using System.Net.Http;
using UnityEngine;
using Zenject;
using Game.Data.Repositories;
using System.Collections.Generic;

namespace Game.Data
{
    public class HttpClientFactory : IHttpClientFactory
    {
        [Inject]
        private IPlayerPrefs _playerPrefs;
        [Inject]
        private IOAuthTokenRepository _oauthRepository;
        [Inject]
        private List<IJwtRefreshDelegate> _jwtRefreshDelegates;

        public HttpClient Create()
        {
            var decompressingHandler = new HttpClientHandler()
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip
            };
            var sessionHandler = new SessionHandler(
                decompressingHandler,
                _playerPrefs,
                _oauthRepository,
                _jwtRefreshDelegates);
            var loggingHandler = new DelegatingLoggingHandler(sessionHandler);
            var errorHandler = new ErrorHandler(loggingHandler);
            var httpClient = new HttpClient(errorHandler)
            {
                BaseAddress = new Uri(EnvironmentConst.ApiBaseUrl),
                Timeout = TimeSpan.FromSeconds(60)
            };
            var languageCode = GetSystemLanguageCode();
            httpClient.DefaultRequestHeaders.Add("Accept-Language", languageCode);
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
            return httpClient;
        }

        private string GetSystemLanguageCode()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Russian:
                    return "ru-RU";
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                    return "zh-CHS";
                case SystemLanguage.ChineseTraditional:
                    return "zh-CHT";
                case SystemLanguage.English:
                default:
                    return "en-EN";
            }
        }
    }
}