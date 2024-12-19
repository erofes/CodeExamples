namespace Game.Minimap
{
	using Sirenix.Utilities;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using UniRx;
	using UnityEngine;
	using Zenject;
	using Game.Arena;
	using Game.Managers;
	using Game.Units;


	public class UiMinimapPresenter : IInitializable, IDisposable
	{
		[Inject] IUiMinimapView		_view;
		[Inject] IPlayersManager	_playersManager;

		[Inject] List<TeamBuildingsMonoConfig>		_teamBuildingsMonoConfigs;

		CompositeDisposable _disposables = new();

		ETeam	_localTeam;


		public void Initialize()
		{
			Matrix4x4 w2ui		= CalcW2UiMatrix();
			
			_view.SetWorldToUiMatrix( w2ui );

			_view.ClearItems();
            
			// Wait for LocalPlayer spawn
			_playersManager.LocalPlayer
				.Where( lp => lp != null )
				.Subscribe( lp =>
				{
					_localTeam		= lp.Team;

					AddBuildings();
				})
				.AddTo( _disposables );
		}


		public void Dispose()
		=> 
			_disposables.Dispose();


		void AddBuildings()
		{
			// Towers
			_teamBuildingsMonoConfigs
				.SelectMany( tbmc => tbmc.TowerLines.Values.SelectMany( towers => towers ) )
				.ForEach( AddBuilding );

			// Obelisks
			_teamBuildingsMonoConfigs
				.Select( tbmc => tbmc.Obelisk )
				.ForEach( AddBuilding );
		}


		void AddBuilding( UnitFacade unit )
		{
			bool isAlly		= unit.Team == _localTeam;
			
			_view.AddItem( unit, isAlly );
			
			if (isAlly)
				unit.UnitModel.OnAttacked
					.Subscribe( _ =>
						_view.AddAlert( unit ) )
					.AddTo( _disposables );
		}


		Matrix4x4 CalcW2UiMatrix()
		{
			// TODO: Replace to scene reference bounds
			GameObject minBound				= new GameObject();
			minBound.transform.position		= new Vector3( -64, 0, -64 );
			GameObject maxBound				= new GameObject();
			maxBound.transform.position		= new Vector3( 64, 0, 64 );

			return CalcW2UiMatrix( minBound.transform, maxBound.transform );
		}


		Matrix4x4 CalcW2UiMatrix( Transform worldBoundsMin, Transform worldBoundsMax )
		{
			Vector3 worldMapSize		= worldBoundsMax.position - worldBoundsMin.position;
			Vector3 scaleFactor			= new Vector3( 1 / worldMapSize.x, 0, 1 / worldMapSize.z );
		
			Matrix4x4 translate			= Matrix4x4.Translate( -worldBoundsMin.position );
			Matrix4x4 scale				= Matrix4x4.Scale( scaleFactor );

			return scale * translate;
		}
	}
}

