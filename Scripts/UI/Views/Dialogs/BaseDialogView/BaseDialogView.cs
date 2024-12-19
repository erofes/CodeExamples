using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI
{
    public abstract class BaseDialogView : BaseView, IBaseDialogView
    {
        public static bool isLog;
        public static Color logColor;
        public static int logPrio;
        
        #region Buttons

        [SerializeField]
        protected Button _confirmButton;
        [SerializeField]
        protected Button _declineButton;

        #endregion /Buttons
        
        #region Cache

        private UnityAction _confirmAction;
        private UnityAction _declineAction;

        #endregion /Cache

        #region Life Cycle

        protected override void Awake()
        {
            if (isLog) Log.D(logColor, logPrio, 1);
            
            base.Awake();
            
            _confirmButton.onClick.SetListener(ConfirmAction);
            _declineButton.onClick.SetListener(DeclineAction);
        }

        #endregion /Life Cycle

        #region IBaseDialogView

        protected void Initialize(
            UnityAction confirmAction,
            UnityAction declineAction)
        {
            if (isLog) Log.D(logColor, logPrio, 2);

            _confirmAction = confirmAction;
            _declineAction = declineAction;
        }

        #endregion /IBaseDialogView

        #region Private Methods

        private void ConfirmAction()
        {
            if (isLog) Log.D(logColor, logPrio, 3);
            
            _confirmAction?.Invoke();
        }
        
        private void DeclineAction()
        {
            if (isLog) Log.D(logColor, logPrio, 3);
            
            _declineAction?.Invoke();
        }

        #endregion /Private Methods
    }
}