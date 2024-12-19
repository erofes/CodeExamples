using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Game.Data.Models;
using Game.Data.Repositories;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Game.Data
{
    public class SessionHandler : DelegatingHandler
    {
        public static bool isLog;
        public static UnityEngine.Color debugColor;
        public static int logPrio;

        private IPlayerPrefs _playerPrefs;
        private IOAuthTokenRepository _oauthRepository;
        private List<IJwtRefreshDelegate> _jwtRefreshDelegates;

        public SessionHandler(
                    HttpMessageHandler innerHandler,
                    IPlayerPrefs playerPrefs,
                    IOAuthTokenRepository oauthRepository,
                    List<IJwtRefreshDelegate> jwtRefreshDelegates) : base(innerHandler)
        {
            _playerPrefs = playerPrefs;
            _oauthRepository = oauthRepository;
            _jwtRefreshDelegates = jwtRefreshDelegates;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var hasJwt = _playerPrefs.HasKey(PlayerPrefsKey.ACCESS_TOKEN);
            if (hasJwt)
            {
                var jwt = _playerPrefs.GetString(PlayerPrefsKey.ACCESS_TOKEN);
                using var jwtService = new JWTService();
                var needRefresh = jwtService.IsJwtExpired(jwt);
                if (needRefresh)
                {
                    var refreshToken = _playerPrefs.GetString(PlayerPrefsKey.REFRESH_TOKEN);
                    OAuthResponse responseData;
                    try
                    {
                        responseData = await _oauthRepository.RefreshAccessToken(refreshToken);
                    }
                    catch (Exception)
                    {
                        _playerPrefs.DeleteValue(PlayerPrefsKey.ACCESS_TOKEN);
                        _playerPrefs.DeleteValue(PlayerPrefsKey.REFRESH_TOKEN);
                        throw;
                    }

                    if (responseData != null)
                    {
                        if (isLog) Log.D("Got new JWT");
                        if (isLog) Log.D($"Delegates count: {_jwtRefreshDelegates.Count}");
                        _playerPrefs.SetString(PlayerPrefsKey.ACCESS_TOKEN, responseData.AccessToken);
                        _playerPrefs.SetString(PlayerPrefsKey.REFRESH_TOKEN, responseData.RefreshToken);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", responseData.AccessToken);
                        foreach (var jwtRefreshDelegate in _jwtRefreshDelegates)
                            jwtRefreshDelegate.OnRefreshJwt();
                    }
                }
                else
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            }

            var result = await base.SendAsync(request, cancellationToken);
            return result;
        }
    }
}