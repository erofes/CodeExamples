namespace Game.Minimap
{
	using Sirenix.OdinInspector;
	using System;
	using DG.Tweening;
	using Game.Server;
	using UniRx;
	using UnityEngine;
	using UnityEngine.UI;
	using Zenject;
	using Game.Units;


	public class MinimapItem : MonoBehaviour
	{
		[Title( "Settings" )]
		[SerializeField] Sprite			_towerBgSprite;
		[SerializeField] Sprite			_towerBlueSprite;
		[SerializeField] Sprite			_towerRedSprite;
		[SerializeField] Sprite			_obeliskBgSprite;
		[SerializeField] Sprite			_obeliskBlueSprite;
		[SerializeField] Sprite			_obeliskRedSprite;
		[SerializeField] float			_healthChangeDuration;

		[Title( "Refs" )]
		[SerializeField] RectTransform	_rectTransform;
		[SerializeField] Image			_backgroundImage;
		[SerializeField] Image			_fillImage;

		[Inject] UnitFacade		_unitFacade;
		[Inject] bool			_isAlly;

		Tween _tween;

		public RectTransform RectTransform		=> _rectTransform;


		void Start()
		{
			SetLayout();
		
			HandleState();
		}


		void HandleState()
		{
			_unitFacade.UnitModel.OnAttacked
				.Subscribe( SetHealth )
				.AddTo( this );

			_unitFacade.UnitModel.IsDead
				.Subscribe( SetDead )
				.AddTo( this );
		}


		void SetHealth( DamageEventData _ )
		{
			float health    = _unitFacade.UnitModel.GetCurStat( EStat.Health );
			float maxHealth = _unitFacade.UnitModel.GetMaxStat( EStat.Health );
			float fill      = health / maxHealth;
			
			_tween?.Kill();
			_tween = _fillImage
				.DOFillAmount( fill, _healthChangeDuration )
				.SetLink( gameObject );
		}


		void SetDead( bool value )
		=>
			gameObject.SetActive( !value );


		void SetLayout()
		{
			switch (_unitFacade.Category)
			{
				case ECategory.Obelisk:
					_backgroundImage.sprite		= _obeliskBgSprite;
					_fillImage.sprite			= _isAlly ? _obeliskBlueSprite : _obeliskRedSprite;
					break;
				
				case ECategory.Tower:
					_backgroundImage.sprite		= _towerBgSprite;
					_fillImage.sprite			= _isAlly ? _towerBlueSprite : _towerRedSprite;
					break;
				
				default:
					throw new NotImplementedException( $"Layout for {_unitFacade.Category} is not implemented" );
			}
			
			_backgroundImage.SetNativeSize();
			_fillImage.SetNativeSize();
		}


		public class Factory : PlaceholderFactory<UnitFacade, bool, MinimapItem> {}
	}
}

