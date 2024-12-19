namespace Game.Minimap
{
	using System.Collections.Generic;
	using System.Linq;
	using UniRx;
	using UniRx.Triggers;
	using UnityEngine;
	using Zenject;
	using Game.Units;
	using Game.Utilities;


	public interface IUiMinimapView
	{
		void SetWorldToUiMatrix( Matrix4x4 w2ui );

		void AddItem( UnitFacade unitFacade, bool isAlly );
		void AddAlert( UnitFacade unitFacade );
		void ClearItems();
	}


	public class UiMinimapView : MonoBehaviour, IUiMinimapView
	{
		[Inject(Id = BindId.MinimapParent)] Transform	_parent;

		[Inject] MinimapItem.Factory		_minimapItemFactory;
		[Inject] MinimapAlert.Factory		_minimapAlertFactory;

		Matrix4x4	_w2ui;

		List<MinimapAlert>	_alerts		= new();


		public void SetWorldToUiMatrix( Matrix4x4 w2ui )		=> _w2ui = w2ui;


		public void AddItem( UnitFacade unitFacade, bool isAlly )
		=> 
			CreateItem( unitFacade, isAlly );


		public void AddAlert( UnitFacade unitFacade )
		{
			MinimapAlert alert = _alerts.FirstOrDefault( a => a.UnitFacade == unitFacade );

			if (alert != null) return;

			alert = CreateAlert( unitFacade );

			alert
				.OnDestroyAsObservable()
				.Subscribe( _ => _alerts.Remove( alert ) )
				.AddTo( this );

			_alerts.Add( alert );
		}


		void CreateItem( UnitFacade unitFacade, bool isAlly )
		{
			MinimapItem item = _minimapItemFactory.Create( unitFacade, isAlly );
			SetPosition( item.RectTransform, unitFacade.transform.position );
		}


		MinimapAlert CreateAlert( UnitFacade unitFacade )
		{
			MinimapAlert alert = _minimapAlertFactory.Create( unitFacade );
			SetPosition( alert.RectTransform, unitFacade.transform.position );
			alert.transform.SetAsFirstSibling();
			return alert;
		}


		void SetPosition( RectTransform rectTransform, Vector3 worldPosition )
		{
			Vector2 anchor   = (_w2ui * worldPosition.xyz1()).xz();

			rectTransform.anchorMin = anchor;
			rectTransform.anchorMax = anchor;
		}


		public void ClearItems()
		=>
			Utils.DestroyChildren( _parent );
	}
}

