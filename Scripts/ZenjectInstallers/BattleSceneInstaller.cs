using Game.Data.Repositories;
using Game.Data.SignalRHub.Pve;
using Game.UI.Battle;
using Game.UI.ECS;
using UnityEngine;
using IPveSessionRepository = Game.Data.Repositories.Pve.IPveSessionRepository;
using PveSessionRepository = Game.Data.Repositories.Pve.PveSessionRepository;
using static Game.Injections.ZenjectInstallerAddon;

namespace Game.Injections.Battle
{
    public class BattleSceneInstaller : SceneInstaller
    {
        [SerializeField]
        private BattleView _battleView;
        [SerializeField]
        private BattleResultView _battleResultView;

        public override void InstallBindings()
        {
            if (isLog) Log.D(logColor, logPrio, 1);

            InitUI();
            InitBindings();
            InitEcsManager();
        }

        private void InitBindings()
        {
            var DiAddressables = new DiAddressablesContainer();

            DiAddressables.SimpleBind<EquipmentRepository, IEquipmentRepository>();
            DiAddressables.SimpleBind<PveHub, IPveHub>();
            DiAddressables.SimpleBind<BossFightRepository, IBossFightRepository>();
            DiAddressables.SimpleBind<ContestsRepository, IContestsRepository>();
            DiAddressables.FactoryBind<PveSessionLoggerFactory, IPveSessionLogger>();
            DiAddressables.SimpleBind<PveSessionRepository, IPveSessionRepository>();
            DiAddressables.SimpleBind<HeroesRepository, IHeroesRepository>();

            InitEcsSystems(DiAddressables);
            
            DiAddressables
                .PresenterBind<BattlePresenter>(typeof(IBattlePresenter))
                .ViewBind<BattleView, IBattleView>()
                .FromExistingView(_battleView, _screenManager);
            DiAddressables
                .PresenterBind<BattleResultPresenter>(typeof(IBattleResultPresenter), typeof(IBattleResultSetter))
                .ViewBind<BattleResultView, IBattleResultView>()
                .FromExistingView(_battleResultView, _screenManager);
            
            DiAddressables.Bind(Container);
        }

        private void InitEcsSystems(DiAddressablesContainer DiAddressables)
        {
            DiAddressables.SimpleBind<EcsService, IEcsService>();
        }
    }
}