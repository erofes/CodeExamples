using UnityEngine.Events;

namespace Game.UI
{
    public class BuyPackDialogModelController : IConfirmationDialogModelController<BuyPackDialogModel>
    {
        public BuyPackDialogModel Model { get; }
        public UnityAction OnConfirmAction { get; }
        public UnityAction OnDeclineAction { get; }

        public BuyPackDialogModelController(
            BuyPackDialogModel model,
            UnityAction onConfirmAction,
            UnityAction onDeclineAction = null)
        {
            Model = model;
            OnConfirmAction = onConfirmAction;
            OnDeclineAction = onDeclineAction;
        }
    }
}