using System.Collections.Generic;

namespace Game.UI.ECS
{
    #region EcsSystem

    public class EcsSystem : IEcsSystem
    {
        private readonly List<IBaseSystem> _allSystems;
        private int _allSystemsCount;

        private IInitSystem[] _initSystems;
        private IAwakeSystem[] _awakeSystems;
        private IStartSystem[] _startSystems;
        private IPauseSystem[] _pauseSystems;
        private IDestroySystem[] _destroySystems;
        private IUpdateSystem[] _updateSystems;
        private IFixedUpdateSystem[] _fixedUpdateSystems;
        private ILateUpdateSystem[] _lateUpdateSystems;
        private IOneSecondUpdateSystem[] _oneSecondUpdateSystems;
        private ISceneChangedSystem[] _sceneChangedSystems;

        // Init Data
        private int _initSystemsSize;
        private int _awakeSystemsSize;
        private int _startSystemsSize;
        private int _pauseSystemsSize;
        private int _destroySystemsSize;
        private int _updateSystemsSize;
        private int _fixedUpdateSystemsSize;
        private int _lateUpdateSystemsSize;
        private int _oneSecondUpdateSystemsSize;
        private int _sceneChangedSystemsSize;

        public EcsSystem(params IBaseSystem[] systems)
        {
            _allSystems = new List<IBaseSystem>();
            if (systems == null)
            {
                Log.E("systems is null!");
                return;
            }
            var count = systems.Length;
            for (int i = 0; i < count; i++)
                Add(systems[i]);
        }

        private IEcsSystem Add(IBaseSystem system)
        {
            _allSystems.Add(system);
            _allSystemsCount++;

            if (system is IInitSystem)
                _initSystemsSize++;
            if (system is IAwakeSystem)
                _awakeSystemsSize++;
            if (system is IStartSystem)
                _startSystemsSize++;
            if (system is IPauseSystem)
                _pauseSystemsSize++;
            if (system is IDestroySystem)
                _destroySystemsSize++;
            if (system is IUpdateSystem)
                _updateSystemsSize++;
            if (system is IFixedUpdateSystem)
                _fixedUpdateSystemsSize++;
            if (system is ILateUpdateSystem)
                _lateUpdateSystemsSize++;
            if (system is IOneSecondUpdateSystem)
                _oneSecondUpdateSystemsSize++;
            if (system is ISceneChangedSystem)
                _sceneChangedSystemsSize++;

            return this;
        }

        public void Init()
        {
            if (_allSystemsCount == 0) return;

            var initId = 0;
            var awakeId = 0;
            var startId = 0;
            var pauseId = 0;
            var enableId = 0;
            var updateId = 0;
            var fixedUpdateId = 0;
            var lateUpdateId = 0;
            var oneSecondUpdateId = 0;
            var scenedChangeId = 0;

            if (_initSystemsSize > 0)
                _initSystems = new IInitSystem[_initSystemsSize];
            if (_awakeSystemsSize > 0)
                _awakeSystems = new IAwakeSystem[_awakeSystemsSize];
            if (_startSystemsSize > 0)
                _startSystems = new IStartSystem[_startSystemsSize];
            if (_pauseSystemsSize > 0)
                _pauseSystems = new IPauseSystem[_pauseSystemsSize];
            if (_destroySystemsSize > 0)
                _destroySystems = new IDestroySystem[_destroySystemsSize];
            if (_updateSystemsSize > 0)
                _updateSystems = new IUpdateSystem[_updateSystemsSize];
            if (_fixedUpdateSystemsSize > 0)
                _fixedUpdateSystems = new IFixedUpdateSystem[_fixedUpdateSystemsSize];
            if (_lateUpdateSystemsSize > 0)
                _lateUpdateSystems = new ILateUpdateSystem[_lateUpdateSystemsSize];
            if (_oneSecondUpdateSystemsSize > 0)
                _oneSecondUpdateSystems = new IOneSecondUpdateSystem[_oneSecondUpdateSystemsSize];
            if (_sceneChangedSystemsSize > 0)
                _sceneChangedSystems = new ISceneChangedSystem[_sceneChangedSystemsSize];

            for (int i = 0; i < _allSystemsCount; i++)
            {
                var system = _allSystems[i];
                if (system is IInitSystem initSystem)
                    _initSystems[initId++] = initSystem;
                if (system is IAwakeSystem awakeSystem)
                    _awakeSystems[awakeId++] = awakeSystem;
                if (system is IStartSystem startSystem)
                    _startSystems[startId++] = startSystem;
                if (system is IPauseSystem pauseSystem)
                    _pauseSystems[pauseId++] = pauseSystem;
                if (system is IDestroySystem destroySystem)
                    _destroySystems[enableId++] = destroySystem;
                if (system is IUpdateSystem updateSystem)
                    _updateSystems[updateId++] = updateSystem;
                if (system is IFixedUpdateSystem fixedUpdateSystem)
                    _fixedUpdateSystems[fixedUpdateId++] = fixedUpdateSystem;
                if (system is ILateUpdateSystem lateUpdateSystem)
                    _lateUpdateSystems[lateUpdateId++] = lateUpdateSystem;
                if (system is IOneSecondUpdateSystem oneSecondUpdateSystem)
                    _oneSecondUpdateSystems[oneSecondUpdateId++] = oneSecondUpdateSystem;
                if (system is ISceneChangedSystem sceneChangedSystem)
                    _sceneChangedSystems[scenedChangeId++] = sceneChangedSystem;
            }
        }

        public void OnInit()
        {
            if (_initSystemsSize == 0) return;
            for (int i = 0; i < _initSystemsSize; i++)
                _initSystems[i].OnInit(this);
        }

        public void OnAwake()
        {
            if (_awakeSystemsSize == 0) return;
            for (int i = 0; i < _awakeSystemsSize; i++)
                _awakeSystems[i].OnAwake(this);
        }

        public void OnStart()
        {
            if (_startSystemsSize == 0) return;
            for (int i = 0; i < _startSystemsSize; i++)
                _startSystems[i].OnStart(this);
        }

        public void OnPause(bool onPause)
        {
            if (_pauseSystemsSize == 0) return;
            for (int i = 0; i < _pauseSystemsSize; i++)
                _pauseSystems[i].OnPause(this, onPause);
        }

        public void OnDestroy()
        {
            if (_destroySystemsSize == 0) return;
            for (int i = 0; i < _destroySystemsSize; i++)
                _destroySystems[i].OnDestroy(this);
        }

        public void OnUpdate()
        {
            if (_updateSystemsSize == 0) return;
            for (int i = 0; i < _updateSystemsSize; i++)
                _updateSystems[i].OnUpdate(this);
        }

        public void OnFixedUpdate()
        {
            if (_fixedUpdateSystemsSize == 0) return;
            for (int i = 0; i < _fixedUpdateSystemsSize; i++)
                _fixedUpdateSystems[i].OnFixedUpdate(this);
        }

        public void OnLateUpdate()
        {
            if (_lateUpdateSystemsSize == 0) return;
            for (int i = 0; i < _lateUpdateSystemsSize; i++)
                _lateUpdateSystems[i].OnLateUpdate(this);
        }

        public void OnOneSecondUpdate()
        {
            if (_oneSecondUpdateSystemsSize == 0) return;
            for (int i = 0; i < _oneSecondUpdateSystemsSize; i++)
                _oneSecondUpdateSystems[i].OnOneSecondUpdate(this);
        }

        public void OnSceneChanged()
        {
            if (_sceneChangedSystemsSize == 0) return;
            for (int i = 0; i < _sceneChangedSystemsSize; i++)
                _sceneChangedSystems[i].OnSceneChanged(this);
        }
    }

    #endregion /EcsSystem

    #region System

    public interface IBaseSystem { }

    public interface IInitSystem
    {
        void OnInit(EcsSystem system);
    }

    public interface IAwakeSystem
    {
        void OnAwake(EcsSystem system);
    }

    public interface IStartSystem
    {
        void OnStart(EcsSystem system);
    }

    public interface IPauseSystem
    {
        void OnPause(EcsSystem system, bool onPause);
    }

    public interface IDestroySystem
    {
        void OnDestroy(EcsSystem system);
    }

    public interface IUpdateSystem
    {
        void OnUpdate(EcsSystem system);
    }

    public interface IFixedUpdateSystem
    {
        void OnFixedUpdate(EcsSystem system);
    }

    public interface ILateUpdateSystem
    {
        void OnLateUpdate(EcsSystem system);
    }

    public interface IOneSecondUpdateSystem
    {
        void OnOneSecondUpdate(EcsSystem system);
    }

    /// <summary>Has effect only on global system</summary>
    public interface ISceneChangedSystem
    {
        void OnSceneChanged(EcsSystem system);
    }

    #endregion /System
}