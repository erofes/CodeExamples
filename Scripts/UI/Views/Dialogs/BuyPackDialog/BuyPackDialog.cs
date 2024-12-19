using Game.UI.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class BuyPackDialog :
        ConfirmationDialog<BuyPackDialogModelController, BuyPackDialogModel>,
        IBuyPackDialog
    {
        #region View Elements

        [SerializeField]
        private ImageVariants _icon;
        [SerializeField]
        private ImageVariants _iconBg;
        [SerializeField]
        private Text _title;
        [SerializeField]
        private ImageVariants _titleIcon;
        [SerializeField]
        private TextWithIcon[] _costs;
        [SerializeField]
        private GameObject _costsContainer;

        #endregion /View Elements

        #region IBuyPackDialog

        public override void Initialize(BuyPackDialogModelController controller)
        {
            base.Initialize(controller);
            var model = controller.Model;
            if (model.IconUrl != null)
            {
                var iconImage = _icon.GetImage();
                iconImage.LoadFromUrl(model.IconUrl);
            }

            if (model.IconKey != null)
            {
                _iconBg.SetVariant(model.IconKey);
                if (model.IconUrl == null)
                    _icon.SetVariant(model.IconKey);
            }

            _title.text = model.Title;
            var showTitleIcon = !model.TitleIconKey.IsNullOrEmpty();
            _titleIcon.SetActive(showTitleIcon);
            if (showTitleIcon)
                _titleIcon.SetVariant(model.TitleIconKey);

            foreach (var cost in _costs)
                cost.gameObject.TrySetActive(false);
            var showCosts = !model.Costs.IsNullOrEmpty();
            _costsContainer.TrySetActive(showCosts);
            if (showCosts)
            {
                var costsLength = Mathf.Min(model.Costs.Length, _costs.Length);
                for (var i = 0; i < costsLength; i++)
                {
                    _costs[i].gameObject.TrySetActive(true);
                    _costs[i].Initialize(model.Costs[i]);
                }
            }
        }

        #endregion /IBuyPackDialog
    }
}