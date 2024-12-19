using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Airplane
{
    public class PerlinNoiseBotPilot : AirplanePilot
    {
        private readonly BotConfig _config;
        private readonly DateTime _creationTime = DateTime.Now;
        private readonly double _yawSeed = Random.Range( -100f, 100f );
        private readonly double _accelSeed = Random.Range( -100f, 100f );
        private readonly double _yawNoiseFactor = Random.Range( 0.1f, 1f );
        private readonly double _accelNoiseFactor = Random.Range( 0.1f, 1f );

        private readonly int _id;

        public override BotConfig BotConfig => _config;
        public override string DebugName => "BotPilot " + _id;
        public override bool IsUser => false;

        public PerlinNoiseBotPilot( BotConfig config, int id )
        {
            _id = id;
            _config = config;
        }

        public override float GetYaw()
        {
            float noise = Mathf.PerlinNoise1D( GetNoiseInput( _yawSeed, _yawNoiseFactor ) );

            return Mathf.Lerp( _config.JawMinMax.x, _config.JawMinMax.y, noise );
        }

        public override float GetAcceleration()
        {
            float noise = Mathf.PerlinNoise1D( GetNoiseInput( _accelSeed, _accelNoiseFactor ) );

            return Mathf.Lerp( _config.AccelerationMinMax.x, _config.AccelerationMinMax.y, noise );
        }

        protected override void OnDispose() { }

        private float GetNoiseInput( double seed, double factor )
        {
            DateTime now = DateTime.Now;
            TimeSpan span = now - _creationTime;
            double seconds = span.TotalSeconds;
            double result = seconds * factor + seed;

            return ( float )result;
        }
    }
}
