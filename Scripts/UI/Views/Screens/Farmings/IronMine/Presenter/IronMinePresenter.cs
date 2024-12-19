using Cysharp.Threading.Tasks;
using Game.Data.Models;
using Zenject;

namespace Game.UI.City
{
    public class IronMinePresenter : FarmingPresenter<IIronMineView>, IIronMinePresenter, IIronMineSlotsUpdateDelegate
    {
        #region Dependencies

        [Inject]
        private IIronMineCardSelectorPresenter _ironMineCardSelectorPresenter;

        #endregion /Dependencies

        public IronMinePresenter(IIronMineView view) : base(view, CurrencyValues.IRON) 
        {
            view.InitButtonActions(Hide);
        }

        #region IIronMinePresenter

        public void Initialize()
        {
            UpdateFarmingSlots(CurrencyValues.IRON).Forget();
        }

        #endregion /IIronMinePresenter

        #region IIronMineSlotsUpdateDelegate

        public override void OnWarehouseIsNoLongerFull()
        {
            if (isLog) Log.D(logColor, logPrio, 4);

            OnWarehouseIsNoLongerFull(CurrencyValues.IRON);
        }

        #endregion /IIronMineSlotsUpdateDelegate

        protected async override UniTask<CardSelectorResult> SelectCard(CardSelectorProps props)
        {
            if (isLog) Log.D(logColor, logPrio, 3);

            _ironMineCardSelectorPresenter.SetProps(props);
            _ironMineCardSelectorPresenter.Show();
            return await _ironMineCardSelectorPresenter.GetResult();
        }
    }
}