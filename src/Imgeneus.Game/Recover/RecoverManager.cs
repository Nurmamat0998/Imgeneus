﻿using Imgeneus.Game.Blessing;
using Imgeneus.World.Game.Country;
using Imgeneus.World.Game.Health;
using Imgeneus.World.Game.Movement;
using Microsoft.Extensions.Logging;
using System;
using System.Timers;

namespace Imgeneus.Game.Recover
{
    public class RecoverManager : IRecoverManager
    {
        private readonly ILogger<RecoverManager> _logger;
        private readonly IHealthManager _healthManager;
        private readonly IMovementManager _movementManager;
        private readonly ICountryProvider _countryProvider;
        private readonly IBlessManager _blessManager;
        private uint _ownerId;

        public RecoverManager(ILogger<RecoverManager> logger, IHealthManager healthManager, IMovementManager movementManager, ICountryProvider countryProvider, IBlessManager blessManager)
        {
            _logger = logger;
            _healthManager = healthManager;
            _movementManager = movementManager;
            _countryProvider = countryProvider;
            _blessManager = blessManager;
            _recoverTimer.Elapsed += RecoverTimer_Elapsed;
            _recoverTimer.Start();
#if DEBUG
            _logger.LogDebug("RecoverManager {hashcode} created", GetHashCode());
#endif
        }


#if DEBUG
        ~RecoverManager()
        {
            _logger.LogDebug("RecoverManager {hashcode} collected by GC", GetHashCode());
        }
#endif

        #region Init & Clear

        public void Init(uint ownerId)
        {
            _ownerId = ownerId;
        }

        public void Dispose()
        {
            _recoverTimer.Elapsed -= RecoverTimer_Elapsed;
        }

        #endregion

        #region Recover

        private readonly Timer _recoverTimer = new Timer() { AutoReset = true, Interval = 10000 };

        private void RecoverTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_healthManager.IsDead)
                return;

            if (_healthManager.CurrentHP == _healthManager.MaxHP && _healthManager.CurrentMP == _healthManager.MaxMP && _healthManager.CurrentSP == _healthManager.MaxSP)
                return;

            byte recoverHPPercent = 2;
            byte recoverMPSPPercent = 2;

            if (_movementManager.Motion == Database.Constants.Motion.Sit)
            {
                recoverHPPercent += 5;
                recoverMPSPPercent += 5;

                if (_countryProvider.Country == CountryType.Light && _blessManager.LightAmount > IBlessManager.SP_MP_SIT)
                    recoverMPSPPercent += 3;

                if (_countryProvider.Country == CountryType.Dark && _blessManager.DarkAmount > IBlessManager.SP_MP_SIT)
                    recoverMPSPPercent += 3;

                if (_countryProvider.Country == CountryType.Light && _blessManager.LightAmount > IBlessManager.HP_SIT)
                    recoverHPPercent += 3;

                if (_countryProvider.Country == CountryType.Dark && _blessManager.DarkAmount > IBlessManager.HP_SIT)
                    recoverHPPercent += 3;
            }
            else
            {
                if (_countryProvider.Country == CountryType.Light && _blessManager.LightAmount > IBlessManager.HP_SP_MP_BATTLE)
                {
                    recoverHPPercent += 3;
                    recoverMPSPPercent += 3;
                }

                if (_countryProvider.Country == CountryType.Dark && _blessManager.DarkAmount > IBlessManager.HP_SP_MP_BATTLE)
                {
                    recoverHPPercent += 3;
                    recoverMPSPPercent += 3;
                }
            }

            int hp = 0;
            if (_healthManager.CurrentHP < _healthManager.MaxHP)
                hp = _healthManager.MaxHP * recoverHPPercent / 100;

            int mp = 0;
            if (_healthManager.CurrentMP < _healthManager.MaxMP)
                mp = _healthManager.MaxMP * recoverMPSPPercent / 100;

            int sp = 0;
            if (_healthManager.CurrentSP < _healthManager.MaxSP)
                sp = _healthManager.MaxSP * recoverMPSPPercent / 100;

            _healthManager.Recover(hp, mp, sp);
        }

        #endregion
    }
}
