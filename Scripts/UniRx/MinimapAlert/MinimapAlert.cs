namespace Game.Minimap
{
	using DG.Tweening;
	using System;
	using UniRx;
	using UnityEngine;
	using UnityEngine.UI;
	using Zenject;
	using Game.Server;
	using Game.Units;


	public class MinimapAlert : MonoBehaviour
	{
		[Header( "Settings" )]
		[SerializeField] float		_duration				= 5;
		[SerializeField] float		_animationFrequency		= 1;
		[SerializeField] float		_animationScale			= 1;

		[Header( "Refs" )]
		[SerializeField] RectTransform	_rectTransform;
		[SerializeField] RectTransform	_circle1;
		[SerializeField] RectTransform	_circle2;
		[SerializeField] RectTransform	_center;
		[SerializeField] Image			_circleImage1;
		[SerializeField] Image			_circleImage2;
		[SerializeField] Image			_centerImage;

		[Inject] UnitFacade			_unitFacade;

		public RectTransform	RectTransform	=> _rectTransform;
		public UnitFacade		UnitFacade		=> _unitFacade;


		void Start()
		{
			RunAnimation();

			_unitFacade.UnitModel.OnAttacked
				.StartWith( default(DamageEventData) )
				.Throttle( TimeSpan.FromSeconds( _duration ) )
				.Subscribe( Remove )
				.AddTo( this );
		}


		void RunAnimation()
		{
			float halfPeriod	= _animationFrequency * .5f;
			float quartPeriod	= _animationFrequency * .25f;

			_circle1.localScale = Vector3.zero;
			_circle2.localScale = Vector3.zero;
			_center.localScale	= Vector3.zero;

			_circle1
				.DOScale( _animationScale, _animationFrequency )
				.SetLoops( -1 )
				.SetLink( gameObject );

			_circle2
				.DOScale( _animationScale, _animationFrequency )
				.SetDelay( halfPeriod )
				.SetLoops( -1 )
				.SetLink( gameObject );

			_circleImage1
				.DOFade( 0, _animationFrequency )
				.SetDelay( _animationFrequency )
				.SetLoops( -1 )
				.SetEase( Ease.InQuart )
				.SetLink( gameObject );

			_circleImage2
				.DOFade( 0, _animationFrequency )
				.SetDelay( halfPeriod )
				.SetLoops( -1 )
				.SetEase( Ease.InQuart )
				.SetLink( gameObject );

			DOTween.Sequence()
				.Append( _center.DOScale( 1, quartPeriod ) )
				.Append( _centerImage.DOFade( 0, quartPeriod ) )
				.SetLoops( -1 )
				.SetLink( gameObject );
		}


		void Remove( DamageEventData _ )
		=>
			Destroy( gameObject );


		public class Factory : PlaceholderFactory<UnitFacade, MinimapAlert> {}
	}
}