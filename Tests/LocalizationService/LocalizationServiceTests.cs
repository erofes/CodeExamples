using FluentAssertions;
using InanomoUnity.Data;
using InanomoUnity.Domain;
using Moq;
using NUnit.Framework;
using Game.Domain;
using Tests.MonoServices;
using UnityEngine;
using UnityEngine.Localization;

namespace Tests.Domain
{
    public class LocalizationServiceTests : ServiceTestFixture<LocalizationService>
    {
        #region Mocks

        private Mock<IPlayerPrefs> _playerPrefs;
        private Mock<ILocaleService> _localeService;

        #endregion /Mocks

        #region Setup

        protected override void Setup()
        {
            InitDataSource(out _playerPrefs);
            InitService(out _localeService);
            InitService<IAddressableService>();
            Sut = new LocalizationService();
            base.Setup();
        }

        #endregion /Setup

        #region Given

        private void Given_LocaleService_GetLocale(Language language)
        {
            var languageString = language.ToString();
            Locale GetLocale()
            {
                var locale = ScriptableObject.CreateInstance<Locale>();
                locale.Identifier = languageString;
                return locale;
            }

            System.Func<LocaleIdentifier, string, bool> verify = (instance, code) =>
                instance.Code == code;
            
            _localeService
                .Setup(instance =>
                    instance
                        .GetLocale(It.Is<LocaleIdentifier>(localeIdentifier => 
                            verify(localeIdentifier, languageString))))
                .Returns(GetLocale);
        }

        private void Given_LocaleService_GetText(LocalizationKey key, string value)
        {
            _localeService
                .Setup(instance =>
                    instance.GetText(key.ToString()))
                .Returns(value);
        }

        #endregion /Given

        #region ChangeLanguage

        [Test]
        public void Test_ChangeLanguage_English()
        {
            // given
            const Language language = Language.en;
            // when
            Sut.ChangeLanguage(language);
            // then
            _playerPrefs
                .Verify(instance => 
                    instance.SetInt(PlayerPrefsKey.LANGUAGE_KEY, (int)language), 
                    Times.Once());
        }
        
        [Test]
        public void Test_ChangeLanguage_Russian()
        {
            // given
            const Language language = Language.ru;
            // when
            Sut.ChangeLanguage(language);
            // then
            _playerPrefs
                .Verify(instance => 
                    instance.SetInt(PlayerPrefsKey.LANGUAGE_KEY, (int)language), 
                    Times.Once());
        }
        
        [Test]
        public void Test_ChangeLanguage_EnglishLocaleSelected()
        {
            // given
            const Language language = Language.en;
            Given_LocaleService_GetLocale(language);
            System.Func<Locale, string, bool> verifyLocale =
                (locale, id) => locale.Identifier == id;
            // when
            Sut.ChangeLanguage(language);
            // then
            _localeService
                .Verify(instance => 
                        instance.SetSelectedLocale(It.Is<Locale>(locale => verifyLocale(locale, language.ToString()))),
                    Times.Once());
        }
        
        [Test]
        public void Test_ChangeLanguage_RussianLocaleSelected()
        {
            // given
            const Language language = Language.ru;
            Given_LocaleService_GetLocale(language);
            System.Func<Locale, string, bool> verifyLocale =
                (locale, id) => locale.Identifier == id;
            // when
            Sut.ChangeLanguage(language);
            // then
            _localeService
                .Verify(instance => 
                        instance.SetSelectedLocale(It.Is<Locale>(locale => verifyLocale(locale, language.ToString()))),
                    Times.Once());
        }

        #endregion /ChangeLanguage

        #region GetText

        [Test]
        public void Test_GetText_CalledOnce()
        {
            // given
            const string expectedText = "SomeText";
            Given_LocaleService_GetText(LocalizationKey.NotEnoughGoldText, expectedText);
            // when
            Sut.GetText(LocalizationKey.NotEnoughGoldText);
            // then
            _localeService
                .Verify(instance => 
                        instance.GetText(LocalizationKey.NotEnoughGoldText.ToString()), 
                    Times.Once());
        }
        
        [Test]
        public void Test_GetText_ExactExpected()
        {
            // given
            const string expectedText = "SomeText";
            Given_LocaleService_GetText(LocalizationKey.NotEnoughGoldText, expectedText);
            // when
            var text = Sut.GetText(LocalizationKey.NotEnoughGoldText);
            // then
            text.Should().Be(expectedText);
        }

        #endregion /GetText
    }
}