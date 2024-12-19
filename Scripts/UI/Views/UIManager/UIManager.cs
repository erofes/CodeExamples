using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Game.UI.City;
using Game.UI.Smithy;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

namespace Game.UI
{
    public abstract class UIManager : MonoBehaviour, IUIManager
    {
        public static bool isLog;
        public static Color logColor;
        public static int logPrio;

        #region Cache

        [SerializeField]
        private BackgroundView _backgroundView;

        private List<BaseView> _baseViews => _db.BaseViews;
        [SerializeField]
        private UIManagerDatabase _db;

        [SerializeField]
        [NaughtyAttributes.ReadOnly]
        protected CanvasType _canvasType;

        protected Canvas Canvas;

        protected readonly HashSet<IBaseView> Cache = new HashSet<IBaseView>();
        protected IBaseView CurrentUIObject;
        protected IBaseView PendingUIObject;

        private Dictionary<IBaseView, UnityEvent> _onPreHideEvent = new Dictionary<IBaseView, UnityEvent>();
        private Dictionary<IBaseView, UnityEvent> _onAfterHideEvent = new Dictionary<IBaseView, UnityEvent>();
        private Stack<IBaseView> _stack = new Stack<IBaseView>();

        [NaughtyAttributes.ShowNativeProperty]
        private string CurrentUI => (CurrentUIObject == null ? "null" : CurrentUIObject.GetType().Name);
        [NaughtyAttributes.ShowNativeProperty]
        private BaseView CurrentUIView => (CurrentUIObject == null ? null : CurrentUIObject as BaseView);
        [NaughtyAttributes.ShowNativeProperty]
        private Canvas CurrentCanvas => Canvas;

        #endregion /Cache

        public void SetBackgroundView(BackgroundView backgroundView)
        {
            _backgroundView = backgroundView;
        }

        public bool IsActive<T>() where T : IBaseView
        {
            T uiObject = GetCached<T>();

            if (uiObject != null)
                return (uiObject as BaseView).gameObject.activeSelf;
            return false;
        }

        public bool IsInsideStack<T>() where T : IBaseView
        {
            var hasStack = HasStacked();
            if (!hasStack) return false;

            T uiObject = GetCached<T>();

            if (uiObject != null)
                return _stack.Contains(uiObject);
            return false;
        }

        public bool TrySetCanvas(Canvas canvas)
        {
            if (isLog) Log.D(logColor, logPrio, 1);
            if (Canvas != null) return false;
            if (canvas == null) return false;
            Canvas = canvas;
            return true;
        }

        public void ClearCache()
        {
            if (isLog) Log.D(logColor, logPrio, 2);
            Cache.Clear();
            CurrentUIObject = null;
            PendingUIObject = null;
            _onPreHideEvent = new Dictionary<IBaseView, UnityEvent>();
            _onAfterHideEvent = new Dictionary<IBaseView, UnityEvent>();
            _stack = new Stack<IBaseView>();
        }

        public T Show<T>(T _) where T : IBaseView => Show<T>();
        public T Show<T>() where T : IBaseView
        {
            if (isLog) Log.D($"Selected: {typeof(T)}, Current: {(CurrentUIObject == null ? "null" : (CurrentUIObject as BaseView).name)}", logColor, logPrio, 3);

            T uiObject;
            try
            {
                uiObject = Get<T>();
            }
            catch (Exception e)
            {
                Log.E($"Error while showing {typeof(T)} view, check list:\n1.Do you pick wrong UIManager instance: {gameObject.name}?\n2.Do you add Dependency to your class: {typeof(T)} in CitySceneInstaller with new Addressable binding class?\n{e}");
                throw;
            }

            if (uiObject == null)
            {
                Log.E($"Object of type {typeof(T)} not found");
                return default;
            }

            if (uiObject.IsOptionSet(ScreenOptions.Free))
            {
                uiObject.Show();
                uiObject.OnShow();
                return uiObject;
            }

            if (CurrentUIObject != null)
            {
                if (CurrentUIObject == (IBaseView)uiObject)
                {
                    if (isLog) Log.D($"Same opening", logColor, logPrio, 3);
                    return uiObject;
                }

                if (!uiObject.IsOptionSet(ScreenOptions.Stackable) && !PeekStackable())
                {
                    PopStacks();
                }

                PendingUIObject = uiObject;
                TryHide(CurrentUIObject);
                if (isLog && _stack.Count == 0)
                    Log.D($"New root UI: {typeof(T)}", logColor, logPrio, 4);
                PushStack(uiObject);
                PendingUIObject = null;
            }

            CurrentUIObject = uiObject;

            if (PeekStackable())
            {
                uiObject.ShowSlideContentInstant();
                uiObject.Fade(false);
            }
            else
                uiObject.Show();

            if (_stack.Count == 0)
            {
                if (isLog) Log.D($"New root UI: {typeof(T)}", logColor, logPrio, 4);
                PushStack(uiObject);
            }

            TryShowBackground((BaseView)CurrentUIObject, true);
            uiObject.OnShow();
            return uiObject;
        }

        // TODO: Convert all references to ShowAsync<T> and Initialize(TModel) style
        public T Show<T, TModel>(TModel model) where T : IBaseViewT<TModel>
            where TModel : IBaseViewModel
        {
            if (isLog) Log.D($"{typeof(T)}", logColor, logPrio, 4);
            T uiObject;

            try
            {
                uiObject = Get<T>();
            }
            catch (Exception e)
            {
                Log.E($"Error while showing {typeof(T)} view, check list:\n1.Do you pick wrong UIManager instance: {gameObject.name}?\n2.Do you add Dependency to your class: {typeof(T)} in CitySceneInstaller with new Addressable binding class?\n{e}");
                throw;
            }

            if (uiObject == null)
            {
                Log.E($"Object of type {typeof(T)} not found");
                return default;
            }

            if (CurrentUIObject != null)
            {
                if (CurrentUIObject == (IBaseView)uiObject)
                {
                    if (isLog) Log.D($"Current same as {typeof(T)}, just return the instance of this view", logColor, logPrio, 4);
                    return uiObject;
                }
                TryHide(CurrentUIObject);
            }

            uiObject.SetModel(model);
            CurrentUIObject = uiObject;
            uiObject.Show();
            uiObject.OnShow();
            return uiObject;
        }

        public V Get<V>() where V : IBaseView
        {
            if (isLog) Log.D($"{typeof(V)}", logColor, logPrio, 5);

            var cached = GetCached<V>();
            if (cached != null)
                return cached;

            return PreLoad<V>();
        }

        public V GetCached<V>() where V : IBaseView
        {
            if (isLog) Log.D($"{typeof(V)}", logColor, logPrio, 5);
            var cached = Cache
                .OfType<V>()
                .FirstOrDefault();

            return cached;
        }

        public V PreLoad<V>() where V : IBaseView
        {
            if (isLog) Log.D($"{typeof(V)}", logColor, logPrio, 6);
            var prefab = _baseViews.Find(item => item.GetComponent<IBaseView>() is V);
            if (prefab == null)
                Log.E($"prefab of type {typeof(V)} is null!\nCheck if you call right canvas? {Canvas.gameObject.name}");
            if (Canvas == null)
                Log.E("Canvas is null!");
            var uiGameObject = Instantiate(prefab, Canvas.transform);
            uiGameObject.gameObject.TrySetActive(false);
            if (!uiGameObject.TryGetComponent<V>(out var uiObject))
                throw new NullReferenceException($"not component {typeof(V).Name} on UI!");
            Cache.Add(uiObject);
            uiObject.OnLoad();
            uiObject.ChangeLayer(_canvasType);
            return uiObject;
        }

        public void PreLoadExisting<V>(V view) where V : IBaseView
        {
            if (isLog) Log.D($"{typeof(V)} null:{view == null}", logColor, logPrio, 6);
            if (view == null)
                Log.E($"Probably, you didn't set link to existing view of {typeof(V)} in CitySceneManager inspector window");
            Cache.Add(view);
            view.OnLoad();
            view.ChangeLayer(_canvasType);
        }

        public bool TryHide<T>(T _) where T : IBaseView => TryHide<T>();
        public bool TryHide<T>() where T : IBaseView
        {
            if (isLog) Log.D($"{typeof(T)}", logColor, logPrio, 7);

            if (CurrentUIObject == null || (CurrentUIObject != null && !(CurrentUIObject is T)))
            {
                if (isLog) Log.D($"Requested type:{typeof(T)}, {(CurrentUIObject == null ? $"current is null" : $"current is [{CurrentUIObject.GetType()}]")}", logColor, logPrio, 7);
                HideFreeScreen<T>();
                return false;
            }

            return TryHideCurrent();
        }

        private void HideFreeScreen<T>() where T : IBaseView
        {
            T uiObject;

            try
            {
                uiObject = GetCached<T>();
            }
            catch (Exception e)
            {
                Log.E($"Error while showing {typeof(T)} view, check list:\n1.Do you pick wrong UIManager instance: {gameObject.name}?\n2.Do you add Dependency to your class: {typeof(T)} in CitySceneInstaller with new Addressable binding class?\n{e}");
                throw;
            }

            if (uiObject != null && uiObject.IsOptionSet(ScreenOptions.Free))
            {
                if (isLog) Log.D($"Hide {typeof(T)}", logColor, logPrio, 7);
                uiObject.Hide();
                uiObject.OnShow();
            }
        }

        public bool TryHideAll()
        {
            if (isLog) Log.D(logColor, logPrio, 4);
            PopStacks();
            if (_backgroundView != null)
                _backgroundView.Hide();
            return TryHideCurrent();
        }

        public bool TryHideCurrent()
        {
            IBaseView tempCurrentUI;

            if (CurrentUIObject == null)
            {
                if (isLog) Log.D($"Nothing to hide", logColor, logPrio, 7);
                return false;
            }

            if (PendingUIObject == null)
                PreHide(CurrentUIObject);

            if (PendingUIObject != null && PendingUIObject.IsOptionSet(ScreenOptions.Stackable))
            {
                if (isLog) Log.D($"Keep background for {PendingUIObject.GetType()}, cant hide background for current: {CurrentUIObject.GetType()}", logColor, logPrio, 7);
                CurrentUIObject.Fade(true);

                CurrentUIObject = null;

                return true;
            }
            else
            {
                CurrentUIObject.Hide();
                if (!TryShowBackground((BaseView)CurrentUIObject, false))
                {
                    if (isLog) Log.D($"Ignore hide background:{CurrentUIObject.GetType()}", logColor, logPrio, 7);

                    tempCurrentUI = CurrentUIObject;
                    CurrentUIObject = null;
                    if (PendingUIObject == null)
                        AfterHide(tempCurrentUI);

                    if (PendingUIObject == null && HasStacked())
                    {
                        var relation = GetRelation(false);
                        if (relation == tempCurrentUI)
                        {
                            GetRelation(true);
                            if (HasStacked())
                            {
                                if (isLog) Log.D($"Own relation skip: {(relation as BaseView).name}", logColor, logPrio, 7);
                                relation = GetRelation(false);
                            }
                            else if (isLog) Log.D($"Self close dialog: {(relation as BaseView).name}", logColor, logPrio, 7);
                        }
                        if (isLog) Log.D($"Show relation:{tempCurrentUI.GetType()}->{(relation as BaseView).name}", logColor, logPrio, 7);

                        CurrentUIObject = relation;

                        if (PeekNotStackable())
                        {
                            relation.ShowSlideContentInstant();
                            relation.Fade(false);
                        }
                        else
                        {
                            if (CurrentUIObject.Layer == CanvasType.Screen)
                                relation.Show();
                            else
                            {
                                if (HasStacked())
                                    relation.Show();
                                else
                                {
                                    CurrentUIObject = null;
                                    return true;
                                }
                            }
                        }

                        TryShowBackground((BaseView)relation, true);
                        relation.OnShow();
                    }

                    return true;
                }
            }

            if (isLog) Log.D($"Successful hide background:{CurrentUIObject.GetType()}", logColor, logPrio, 7);

            tempCurrentUI = CurrentUIObject;
            CurrentUIObject = null;
            if (PendingUIObject == null)
                AfterHide(tempCurrentUI);

            if (PendingUIObject == null)
            {
                if (isLog) Log.D($"Pending is null - refresh", logColor, logPrio, 7);
                PopStacks();
            }

            return true;
        }

        private void PreHide(IBaseView uIObject)
        {
            if (_onPreHideEvent.ContainsKey(uIObject))
            {
                if (isLog) Log.D($"Call pre hide actions for:{uIObject.GetType()}", logColor, logPrio, 7);
                var preHideEvent = _onPreHideEvent[uIObject];
                _onPreHideEvent.Remove(uIObject);
                preHideEvent.Invoke();
                preHideEvent.RemoveAllListeners();
            }
        }

        private void AfterHide(IBaseView uIObject)
        {
            if (_onAfterHideEvent.ContainsKey(uIObject))
            {
                if (isLog) Log.D($"Call after hide actions for:{uIObject.GetType()}", logColor, logPrio, 7);
                var preHideEvent = _onAfterHideEvent[uIObject];
                _onAfterHideEvent.Remove(uIObject);
                preHideEvent.Invoke();
                preHideEvent.RemoveAllListeners();
            }
        }

        private bool TryShowBackground(BaseView view, bool value)
        {
            if (view == null)
            {
                if (isLog) Log.D($"Show:{value}, null", logColor, logPrio, 8);
                return false;
            }
            if (view.Layer != CanvasType.Screen)
            {
                if (isLog) Log.D($"Show:{value}, skip not screen layer: {view.name}", logColor, logPrio, 8);
                return false;
            }

            if (view.IsOptionSet(ScreenOptions.Stackable))
            {
                if (isLog) Log.D($"Show:{value}, skip stackable: {view.name}", logColor, logPrio, 8);
                return false;
            }
            else
            {
                if (_backgroundView != null)
                {
                    if (isLog) Log.D($"Show:{value}, ok: {view.name}", logColor, logPrio, 8);
                    if (value)
                    {
                        if (view != null)
                        {
                            _backgroundView.ChangeDirection(view.SlideDirection);
                            _backgroundView.Show();
                        }
                    }
                    else
                        _backgroundView.Hide();
                    return true;
                }
                if (isLog) Log.D($"Show:{value}, skip no hider fone manager: {view.name}", logColor, logPrio, 8);
                return false;
            }
        }

        public void OnPreHide<IView>(UnityAction action) where IView : IBaseView
        {
            if (isLog) Log.D($"Add action on hide for:{typeof(IView)}", logColor, logPrio, 8);

            IView uiObject;

            try
            {
                uiObject = Get<IView>();
            }
            catch (Exception e)
            {
                Log.E($"Error while showing {typeof(IView)} view, check list:\n1.Do you pick wrong UIManager instance: {gameObject.name}?\n2.Do you add Dependency to your class: {typeof(IView)} in CitySceneInstaller with new Addressable binding class?\n{e}");
                throw;
            }

            var baseView = uiObject as BaseView;
            if (baseView == null)
            {
                Log.E($"Error while adding {typeof(IView)} view, not found BaseView component");
                return;
            }

            if (isLog) Log.D($"Add action on view:{baseView.name}", logColor, logPrio, 8);

            if (!_onPreHideEvent.ContainsKey(baseView))
                _onPreHideEvent.Add(baseView, new UnityEvent());
            _onPreHideEvent[baseView].AddListener(action);
        }

        public void OnAfterHide<IView>(UnityAction action) where IView : IBaseView
        {
            if (isLog) Log.D($"Add action on hide for:{typeof(IView)}", logColor, logPrio, 8);

            IView uiObject;

            try
            {
                uiObject = Get<IView>();
            }
            catch (Exception e)
            {
                Log.E($"Error while showing {typeof(IView)} view, check list:\n1.Do you pick wrong UIManager instance: {gameObject.name}?\n2.Do you add Dependency to your class: {typeof(IView)} in CitySceneInstaller with new Addressable binding class?\n{e}");
                throw;
            }

            var baseView = uiObject as BaseView;
            if (baseView == null)
            {
                Log.E($"Error while adding {typeof(IView)} view, not found BaseView component");
                return;
            }

            if (isLog) Log.D($"Add action on view:{baseView.name}", logColor, logPrio, 8);

            if (!_onAfterHideEvent.ContainsKey(baseView))
                _onAfterHideEvent.Add(baseView, new UnityEvent());
            _onAfterHideEvent[baseView].AddListener(action);
        }

        public bool IsCurrent<IView>() where IView : IBaseView
        {
            return CurrentUIObject is IView;
        }

        public async UniTask<bool> ShowConfirmDialog(string text = null)
        {
            var result = new TaskCompletionSource<bool>();
            var model = new TextConfirmationDialogModel(text);
            var controller = new TextConfirmationDialogModelController(
                model,
                () =>
                {
                    result.SetResult(true);
                    TryHide<ITextConfirmationDialog>();
                },
                () =>
                {
                    result.SetResult(false);
                    TryHide<ITextConfirmationDialog>();
                }
            );
            var view = Show<ITextConfirmationDialog>();
            view.Initialize(controller);
            return await result.Task;
        }
        
        public void SetParent<V>(Transform parent) where V : IBaseView
        {
            var view = GetCached<V>();
            if (view is BaseView baseView)
                baseView.transform.SetParent(parent);
        }

        #region Private Methods

        private bool PeekStackable()
        {
            return HasStacked() && _stack.Peek().IsOptionSet(ScreenOptions.Stackable);
        }

        private bool PeekNotStackable()
        {
            return HasStacked() && !_stack.Peek().IsOptionSet(ScreenOptions.Stackable);
        }

        private bool HasStacked() => _stack.Count > 0;

        private IBaseView GetRelation(bool isPop)
        {
            if (isLog) Log.D($"Pop:{isPop}, View:{(_stack.Peek() as BaseView).name}, New stack size: {(isPop ? _stack.Count - 1 : _stack.Count)}", logColor, logPrio, 4);
            if (isPop)
                return _stack.Pop();
            return _stack.Peek();
        }

        private void PushStack(IBaseView old)
        {
            _stack.Push(old);
            if (isLog) Log.D($"New stack size: {_stack.Count}", logColor, logPrio, 4);
        }

        private void PopStacks()
        {
            if (isLog) Log.D($"Removing {_stack.Count} relations", logColor, logPrio, 4);
            while (_stack.Count > 0)
            {
                var relation = _stack.Pop();
                OnPopStack(relation);
            }
        }

        private void OnPopStack(IBaseView relation)
        {
            PreHide(relation);

            relation.Hide();

            AfterHide(relation);
        }

        #endregion /Private Methods
    }
}