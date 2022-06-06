﻿using Imgeneus.Database.Entities;
using Imgeneus.Game.Skills;
using Imgeneus.GameDefinitions;
using Imgeneus.World.Game.Attack;
using Parsec.Shaiya.Skill;
using System;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Imgeneus.World.Game.Buffs
{
    public class Buff
    {
        private static uint Counter = 1;

        public uint Id { get; private set; }

        private object SyncObj = new object();

        public int CountDownInSeconds { get => (int)ResetTime.Subtract(DateTime.UtcNow).TotalSeconds; }

        public Skill Skill { get; init; }

        public bool CanBeActivatedAndDisactivated => Skill.CanBeActivated;

        /// <summary>
        /// Who has created this buff.
        /// </summary>
        public IKiller BuffCreator { get; }

        public Buff(IKiller maker, Skill skill)
        {
            lock (SyncObj)
            {
                Id = Counter++;
            }

            Skill = skill;
            BuffCreator = maker;

            _resetTimer.Elapsed += ResetTimer_Elapsed;
            _periodicalHealTimer.Elapsed += PeriodicalHealTimer_Elapsed;
            _periodicalDebuffTimer.Elapsed += PeriodicalDebuffTimer_Elapsed;
            _periodicalDamageTimer.Elapsed += PeriodicalDamageTimer_Elapsed;
        }

        #region IsDebuff

        /// <summary>
        /// Indicator, that shows if this buff is "bad".
        /// </summary>
        public bool IsDebuff
        {
            get
            {
                switch (Skill.Type)
                {
                    case TypeDetail.EnergyDrain:
                    case TypeDetail.PeriodicalDebuff:
                    case TypeDetail.SubtractingDebuff:
                    case TypeDetail.DeathTouch:
                    case TypeDetail.Stun:
                    case TypeDetail.Immobilize:
                    case TypeDetail.Sleep:
                    case TypeDetail.PreventAttack:
                    case TypeDetail.RemoveAttribute:
                    case TypeDetail.EnergyBackhole:
                    case TypeDetail.MentalStormConfusion:
                    case TypeDetail.SoulMenace:
                    case TypeDetail.MentalStormDistortion:
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Is skill elemental skin buff?
        /// </summary>
        public bool IsElementalProtection
        {
            get
            {
                return Skill.Type == TypeDetail.ElementalProtection;
            }
        }

        /// <summary>
        /// Is skill elemental weapon buff?
        /// </summary>
        public bool IsElementalWeapon
        {
            get
            {
                return Skill.Type == TypeDetail.ElementalAttack;
            }
        }

        /// <summary>
        /// Is skill makes entity untouchable?
        /// </summary>
        public bool IsUntouchable
        {
            get
            {
                return Skill.Type == TypeDetail.Untouchable;
            }
        }

        /// <summary>
        /// Is skill makes invisible?
        /// </summary>
        public bool IsStealth
        {
            get
            {
                return Skill.Type == TypeDetail.Stealth;
            }
        }

        /// <summary>
        /// Does skill block magic attack?
        /// </summary>
        public bool IsBlockMagicAttack
        {
            get
            {
                return Skill.Type == TypeDetail.BlockMagicAttack;
            }
        }

        /// <summary>
        /// Buff should be canceled, when player is moving?
        /// </summary>
        public bool IsCanceledWhenMoving
        {
            get
            {
                return Skill.Type == TypeDetail.PersistBarrier;
            }
        }

        /// <summary>
        /// Buff should be canceled, when player used any other skill?
        /// </summary>
        public bool IsCanceledWhenUsingSkill
        {
            get
            {
                return Skill.Type == TypeDetail.PersistBarrier;
            }
        }

        #endregion

        #region Buff reset

        private DateTime _resetTime;
        /// <summary>
        /// Time, when buff is going to turn off.
        /// </summary>
        public DateTime ResetTime
        {
            get => _resetTime;
            set
            {
                _resetTime = value;

                // Set up timer.
                _resetTimer.Stop();
                _resetTimer.Interval = _resetTime.Subtract(DateTime.UtcNow).TotalMilliseconds > int.MaxValue ? int.MaxValue : _resetTime.Subtract(DateTime.UtcNow).TotalMilliseconds;
                _resetTimer.Start();
            }
        }

        /// <summary>
        /// Timer, that is called when it's time to remove buff.
        /// </summary>
        private readonly Timer _resetTimer = new Timer();

        private void ResetTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CancelBuff();
        }

        /// <summary>
        /// Event, that is fired, when it's time to remove buff.
        /// </summary>
        public event Action<Buff> OnReset;

        /// <summary>
        /// Removes buff from character.
        /// </summary>
        public void CancelBuff()
        {
            _resetTimer.Elapsed -= ResetTimer_Elapsed;
            _resetTimer.Stop();

            _periodicalHealTimer.Elapsed -= PeriodicalHealTimer_Elapsed;
            _periodicalHealTimer.Stop();

            _periodicalDebuffTimer.Elapsed -= PeriodicalDebuffTimer_Elapsed;
            _periodicalDebuffTimer.Stop();

            _periodicalDamageTimer.Elapsed -= PeriodicalDamageTimer_Elapsed;
            _periodicalDamageTimer.Stop();

            OnReset?.Invoke(this);
        }

        #endregion

        #region Periodical Heal

        /// <summary>
        /// Timer, that is called when it's time to make periodical heal (every 3 seconds).
        /// </summary>
        private readonly Timer _periodicalHealTimer = new Timer(3000);

        /// <summary>
        /// Event, that is fired, when it's time to make periodical heal.
        /// </summary>
        public event Action<Buff, AttackResult> OnPeriodicalHeal;

        public ushort TimeHealHP;

        public ushort TimeHealSP;

        public ushort TimeHealMP;

        private void PeriodicalHealTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            OnPeriodicalHeal?.Invoke(this, new AttackResult(AttackSuccess.Normal, new Damage(TimeHealHP, TimeHealSP, TimeHealMP)));
        }

        /// <summary>
        /// Starts periodical healing.
        /// </summary>
        public void StartPeriodicalHeal()
        {
            _periodicalHealTimer.Start();
        }

        #endregion

        #region Periodical debuff

        /// <summary>
        /// Timer, that is called when it's time to make periodical debuff (every second).
        /// </summary>
        private readonly Timer _periodicalDebuffTimer = new Timer(1200);

        /// <summary>
        /// Event, that is fired, when it's time to make periodical debuff.
        /// </summary>
        public event Action<Buff, AttackResult> OnPeriodicalDebuff;

        public ushort TimeHPDamage;

        public ushort TimeSPDamage;

        public ushort TimeMPDamage;

        public TimeDamageType TimeDamageType;

        public int RepeatTime
        {
            set
            {
                _periodicalDebuffTimer.Interval = value * 1000;
            }
        }

        private void PeriodicalDebuffTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            OnPeriodicalDebuff?.Invoke(this, new AttackResult(AttackSuccess.Normal, new Damage(TimeHPDamage, TimeSPDamage, TimeMPDamage)));
        }

        public void StartPeriodicalDebuff()
        {
            _periodicalDebuffTimer.Start();
        }

        #endregion

        #region Periodical damage

        /// <summary>
        /// HP damage, that is made within X meters.
        /// </summary>
        public ushort PeriodicalHP;

        /// <summary>
        /// SP damage, that is made within X meters.
        /// </summary>
        public ushort PeriodicalSP;

        /// <summary>
        /// MP damage, that is made within X meters.
        /// </summary>
        public ushort PeriodicalMP;

        /// <summary>
        /// X meters, where periodical damage is made.
        /// </summary>
        public byte PeriodicalDamageRange;

        /// <summary>
        /// Timer, that is called when it's time to make periodical damage in some area.
        /// </summary>
        private readonly Timer _periodicalDamageTimer = new Timer(1000);

        /// <summary>
        /// Starts periodical damage within X meters.
        /// </summary>
        public void StartPeriodicalDamage()
        {
            _periodicalDamageTimer.Start();
        }

        /// <summary>
        /// Event, that is fired, when it's time to make periodical damage.
        /// </summary>
        public event Action<Buff, AttackResult> OnPeriodicalDamage;

        private void PeriodicalDamageTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            OnPeriodicalDamage?.Invoke(this, new AttackResult(AttackSuccess.Normal, new Damage(PeriodicalHP, PeriodicalSP, PeriodicalMP)));
        }


        #endregion

        public static Buff FromDbCharacterActiveBuff(DbCharacterActiveBuff buff, DbSkill dbSkill)
        {
            return new Buff(null, new Skill(dbSkill, 0, 0))
            {
                ResetTime = buff.ResetTime
            };
        }
    }
}
