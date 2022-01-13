﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Imgeneus.Database.Constants;
using Imgeneus.Database.Preload;
using Imgeneus.World.Game.Health;
using Imgeneus.World.Game.Levelling;
using Imgeneus.World.Game.Monster;
using Imgeneus.World.Game.Player;
using Imgeneus.World.Game.Stats;
using Imgeneus.World.Game.Zone;
using MvvmHelpers;

namespace Imgeneus.World.Game
{
    /// <summary>
    /// Abstract entity, that can be killed. Implements common features for killable object.
    /// </summary>
    public abstract class BaseKillable : IKillable, IMapMember, IDisposable
    {
        protected readonly IDatabasePreloader _databasePreloader;
        public IStatsManager StatsManager { get; private set; }
        public IHealthManager HealthManager { get; private set; }
        public ILevelProvider LevelProvider { get; private set; }

        public BaseKillable(IDatabasePreloader databasePreloader, IStatsManager statsManager, IHealthManager healthManager, ILevelProvider levelProvider)
        {
            _databasePreloader = databasePreloader;
            StatsManager = statsManager;
            HealthManager = healthManager;
            LevelProvider = levelProvider;

            ActiveBuffs.CollectionChanged += ActiveBuffs_CollectionChanged;
            PassiveBuffs.CollectionChanged += PassiveBuffs_CollectionChanged;
        }

        public virtual void Dispose()
        {
            ActiveBuffs.CollectionChanged -= ActiveBuffs_CollectionChanged;
            PassiveBuffs.CollectionChanged -= PassiveBuffs_CollectionChanged;
        }

        private int _id;

        /// <inheritdoc />
        public int Id
        {
            get => _id;
            set
            {
                if (_id == 0)
                {
                    _id = value;
                }
                else
                {
                    throw new ArgumentException("Id can not be set twice.");
                }
            }
        }

        #region Map

        private Map _map;
        public Map Map
        {
            get => _map;

            set
            {
                _map = value;

                if (_map != null) // Map is set to null, when character is disposed.
                    OnMapSet();
            }
        }

        /// <summary>
        /// Call it as soon as map set.
        /// </summary>
        protected virtual void OnMapSet()
        {
        }

        public int CellId { get; set; } = -1;

        public int OldCellId { get; set; } = -1;

        #endregion

        #region Active buffs

        /// <summary>
        /// Active buffs, that increase character characteristic, attack, defense etc.
        /// Don't update it directly, use instead "AddActiveBuff".
        /// </summary>
        public ObservableRangeCollection<ActiveBuff> ActiveBuffs { get; private set; } = new ObservableRangeCollection<ActiveBuff>();

        /// <summary>
        /// Event, that is fired, when buff is added.
        /// </summary>
        public event Action<IKillable, ActiveBuff> OnBuffAdded;

        /// <summary>
        /// Event, that is fired, when buff is removed.
        /// </summary>
        public event Action<IKillable, ActiveBuff> OnBuffRemoved;

        /// <summary>
        /// Updates collection of active buffs.
        /// </summary>
        /// <param name="skill">skill, that client sends</param>
        /// <param name="creator">buff creator</param>
        /// <returns>Newly added or updated active buff</returns>
        public ActiveBuff AddActiveBuff(Skill skill, IKiller creator)
        {
            var resetTime = skill.KeepTime == 0 ? DateTime.UtcNow.AddDays(10) : DateTime.UtcNow.AddSeconds(skill.KeepTime);
            ActiveBuff buff;

            if (skill.IsPassive)
            {
                buff = PassiveBuffs.FirstOrDefault(b => b.SkillId == skill.SkillId);
            }
            else
            {
                buff = ActiveBuffs.FirstOrDefault(b => b.SkillId == skill.SkillId);
            }

            if (buff != null) // We already have such buff. Try to update reset time.
            {
                if (buff.SkillLevel > skill.SkillLevel)
                {
                    // Do nothing, if target already has higher lvl buff.
                    return buff;
                }
                else
                {
                    // If buffs are the same level, we should only update reset time.
                    if (buff.SkillLevel == skill.SkillLevel)
                    {
                        buff.ResetTime = resetTime;

                        // Send update of buff.
                        if (!buff.IsPassive)
                            BuffAdded(buff);
                    }

                    if (buff.SkillLevel < skill.SkillLevel)
                    {
                        // Remove old buff.
                        if (buff.IsPassive)
                            PassiveBuffs.Remove(buff);
                        else
                            ActiveBuffs.Remove(buff);

                        // Create new one with a higher level.
                        buff = new ActiveBuff(creator, skill)
                        {
                            ResetTime = resetTime
                        };
                        if (skill.IsPassive)
                            PassiveBuffs.Add(buff);
                        else
                            ActiveBuffs.Add(buff);
                    }
                }
            }
            else
            {
                // It's a new buff.
                buff = new ActiveBuff(creator, skill)
                {
                    ResetTime = resetTime
                };
                if (skill.IsPassive)
                    PassiveBuffs.Add(buff);
                else
                    ActiveBuffs.Add(buff);
            }

            return buff;
        }

        /// <summary>
        /// Fired, when new buff added or old deleted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ActiveBuffs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (ActiveBuff newBuff in e.NewItems)
                {
                    newBuff.OnReset += ActiveBuff_OnReset;
                    ApplyBuffSkill(newBuff);
                }

                // Case, when we are starting up and all skills are added with AddRange call.
                if (e.NewItems.Count != 1)
                {
                    return;
                }

                BuffAdded((ActiveBuff)e.NewItems[0]);
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var buff = (ActiveBuff)e.OldItems[0];
                RelieveBuffSkill(buff);
                BuffRemoved(buff);
            }
        }

        private void ActiveBuff_OnReset(ActiveBuff sender)
        {
            sender.OnReset -= ActiveBuff_OnReset;
            ActiveBuffs.Remove(sender);
        }

        /// <summary>
        /// Call, that notifies about new added buff.
        /// </summary>
        protected virtual void BuffAdded(ActiveBuff buff)
        {
            OnBuffAdded?.Invoke(this, buff);
        }

        /// <summary>
        /// Call, that notifies about buff removed.
        /// </summary>
        protected virtual void BuffRemoved(ActiveBuff buff)
        {
            OnBuffRemoved?.Invoke(this, buff);
        }

        /// <summary>
        /// Call, that notifies about move or attack speed change.
        /// </summary>
        protected abstract void SendMoveAndAttackSpeed();

        /// <summary>
        /// Call, that notifies about extra stats change.
        /// </summary>
        protected abstract void SendAdditionalStats();

        /// <summary>
        /// Call, that notifies about current hp, mp, sp.
        /// </summary>
        protected abstract void SendCurrentHitpoints();

        /// <summary>
        /// ?
        /// </summary>
        public event Action<IKillable, ActiveBuff, AttackResult> OnSkillKeep;

        /// <summary>
        /// Applies buff effect.
        /// </summary>
        protected void ApplyBuffSkill(ActiveBuff buff)
        {
            var skill = _databasePreloader.Skills[(buff.SkillId, buff.SkillLevel)];
            switch (skill.TypeDetail)
            {
                case TypeDetail.Buff:
                case TypeDetail.PassiveDefence:
                    ApplyAbility(skill.AbilityType1, skill.AbilityValue1, true);
                    ApplyAbility(skill.AbilityType2, skill.AbilityValue2, true);
                    ApplyAbility(skill.AbilityType3, skill.AbilityValue3, true);
                    ApplyAbility(skill.AbilityType4, skill.AbilityValue4, true);
                    ApplyAbility(skill.AbilityType5, skill.AbilityValue5, true);
                    ApplyAbility(skill.AbilityType6, skill.AbilityValue6, true);
                    ApplyAbility(skill.AbilityType7, skill.AbilityValue7, true);
                    ApplyAbility(skill.AbilityType8, skill.AbilityValue8, true);
                    ApplyAbility(skill.AbilityType9, skill.AbilityValue9, true);
                    ApplyAbility(skill.AbilityType10, skill.AbilityValue10, true);
                    break;

                case TypeDetail.SubtractingDebuff:
                    ApplyAbility(skill.AbilityType1, skill.AbilityValue1, false);
                    ApplyAbility(skill.AbilityType2, skill.AbilityValue2, false);
                    ApplyAbility(skill.AbilityType3, skill.AbilityValue3, false);
                    ApplyAbility(skill.AbilityType4, skill.AbilityValue4, false);
                    ApplyAbility(skill.AbilityType5, skill.AbilityValue5, false);
                    ApplyAbility(skill.AbilityType6, skill.AbilityValue6, false);
                    ApplyAbility(skill.AbilityType7, skill.AbilityValue7, false);
                    ApplyAbility(skill.AbilityType8, skill.AbilityValue8, false);
                    ApplyAbility(skill.AbilityType9, skill.AbilityValue9, false);
                    ApplyAbility(skill.AbilityType10, skill.AbilityValue10, false);
                    break;

                case TypeDetail.PeriodicalHeal:
                    buff.TimeHealHP = skill.TimeHealHP;
                    buff.TimeHealMP = skill.TimeHealMP;
                    buff.TimeHealSP = skill.TimeHealSP;
                    buff.OnPeriodicalHeal += Buff_OnPeriodicalHeal;
                    buff.StartPeriodicalHeal();
                    break;

                case TypeDetail.PeriodicalDebuff:
                    buff.TimeHPDamage = skill.TimeDamageHP;
                    buff.TimeMPDamage = skill.TimeDamageMP;
                    buff.TimeSPDamage = skill.TimeDamageSP;
                    buff.TimeDamageType = skill.TimeDamageType;
                    buff.OnPeriodicalDebuff += Buff_OnPeriodicalDebuff;
                    buff.StartPeriodicalDebuff();
                    break;

                case TypeDetail.PreventAttack:
                case TypeDetail.Immobilize:
                    SendMoveAndAttackSpeed();
                    break;

                case TypeDetail.Stealth:
                    //_stealthManager.IsStealth = true;

                    var sprinterBuff = ActiveBuffs.FirstOrDefault(b => b.SkillId == 681 || b.SkillId == 114); // 114 (old ep) 681 (new ep) are unique numbers for sprinter buff.
                    if (sprinterBuff != null)
                        sprinterBuff.CancelBuff();
                    break;

                case TypeDetail.WeaponMastery:
                    if (!_weaponSpeedPassiveSkillModificator.ContainsKey(skill.Weapon1))
                        _weaponSpeedPassiveSkillModificator.Add(skill.Weapon1, skill.Weaponvalue);
                    else
                        _weaponSpeedPassiveSkillModificator[skill.Weapon1] = skill.Weaponvalue;

                    if (!_weaponSpeedPassiveSkillModificator.ContainsKey(skill.Weapon2))
                        _weaponSpeedPassiveSkillModificator.Add(skill.Weapon2, skill.Weaponvalue);
                    else
                        _weaponSpeedPassiveSkillModificator[skill.Weapon2] = skill.Weaponvalue;

                    SendMoveAndAttackSpeed();
                    break;

                case TypeDetail.RemoveAttribute:
                    RemoveElement = true;
                    break;

                case TypeDetail.ElementalAttack:
                    var elementSkin = ActiveBuffs.FirstOrDefault(b => b.IsElementalWeapon && b != buff);
                    if (elementSkin != null)
                        elementSkin.CancelBuff();

                    AttackSkillElement = skill.Element;
                    break;

                case TypeDetail.ElementalProtection:
                    var elementWeapon = ActiveBuffs.FirstOrDefault(b => b.IsElementalProtection && b != buff);
                    if (elementWeapon != null)
                        elementWeapon.CancelBuff();

                    DefenceSkillElement = skill.Element;
                    break;

                case TypeDetail.Untouchable:
                    IsUntouchable = true;
                    break;

                default:
                    throw new NotImplementedException("Not implemented buff skill type.");
            }
        }

        /// <summary>
        /// Removes buff effect.
        /// </summary>
        protected void RelieveBuffSkill(ActiveBuff buff)
        {
            var skill = _databasePreloader.Skills[(buff.SkillId, buff.SkillLevel)];
            switch (skill.TypeDetail)
            {
                case TypeDetail.Buff:
                case TypeDetail.PassiveDefence:
                    ApplyAbility(skill.AbilityType1, skill.AbilityValue1, false);
                    ApplyAbility(skill.AbilityType2, skill.AbilityValue2, false);
                    ApplyAbility(skill.AbilityType3, skill.AbilityValue3, false);
                    ApplyAbility(skill.AbilityType4, skill.AbilityValue4, false);
                    ApplyAbility(skill.AbilityType5, skill.AbilityValue5, false);
                    ApplyAbility(skill.AbilityType6, skill.AbilityValue6, false);
                    ApplyAbility(skill.AbilityType7, skill.AbilityValue7, false);
                    ApplyAbility(skill.AbilityType8, skill.AbilityValue8, false);
                    ApplyAbility(skill.AbilityType9, skill.AbilityValue9, false);
                    ApplyAbility(skill.AbilityType10, skill.AbilityValue10, false);
                    break;

                case TypeDetail.SubtractingDebuff:
                    ApplyAbility(skill.AbilityType1, skill.AbilityValue1, true);
                    ApplyAbility(skill.AbilityType2, skill.AbilityValue2, true);
                    ApplyAbility(skill.AbilityType3, skill.AbilityValue3, true);
                    ApplyAbility(skill.AbilityType4, skill.AbilityValue4, true);
                    ApplyAbility(skill.AbilityType5, skill.AbilityValue5, true);
                    ApplyAbility(skill.AbilityType6, skill.AbilityValue6, true);
                    ApplyAbility(skill.AbilityType7, skill.AbilityValue7, true);
                    ApplyAbility(skill.AbilityType8, skill.AbilityValue8, true);
                    ApplyAbility(skill.AbilityType9, skill.AbilityValue9, true);
                    ApplyAbility(skill.AbilityType10, skill.AbilityValue10, true);
                    break;

                case TypeDetail.PeriodicalHeal:
                    buff.OnPeriodicalHeal -= Buff_OnPeriodicalHeal;
                    break;

                case TypeDetail.PeriodicalDebuff:
                    buff.OnPeriodicalDebuff -= Buff_OnPeriodicalDebuff;
                    break;

                case TypeDetail.PreventAttack:
                case TypeDetail.Immobilize:
                    SendMoveAndAttackSpeed();
                    break;

                case TypeDetail.Stealth:
                    //_stealthManager.IsStealth = ActiveBuffs.Any(b => _databasePreloader.Skills[(b.SkillId, b.SkillLevel)].TypeDetail == TypeDetail.Stealth);
                    break;

                case TypeDetail.WeaponMastery:
                    _weaponSpeedPassiveSkillModificator.Remove(skill.Weapon1);
                    _weaponSpeedPassiveSkillModificator.Remove(skill.Weapon2);

                    SendMoveAndAttackSpeed();
                    break;

                case TypeDetail.RemoveAttribute:
                    RemoveElement = false;
                    break;

                case TypeDetail.ElementalAttack:
                    AttackSkillElement = Element.None;
                    break;

                case TypeDetail.ElementalProtection:
                    DefenceSkillElement = Element.None;
                    break;

                case TypeDetail.Untouchable:
                    IsUntouchable = ActiveBuffs.Any(b => b.IsUntouchable);
                    break;

                default:
                    throw new NotImplementedException("Not implemented buff skill type.");
            }
        }

        private void ApplyAbility(AbilityType abilityType, ushort abilityValue, bool addAbility)
        {
            switch (abilityType)
            {
                case AbilityType.None:
                    return;

                case AbilityType.PhysicalAttackRate:
                case AbilityType.ShootingAttackRate:
                    if (addAbility)
                        _skillPhysicalHittingChance += abilityValue;
                    else
                        _skillPhysicalHittingChance -= abilityValue;
                    return;

                case AbilityType.PhysicalEvationRate:
                case AbilityType.ShootingEvationRate:
                    if (addAbility)
                        _skillPhysicalEvasionChance += abilityValue;
                    else
                        _skillPhysicalEvasionChance -= abilityValue;
                    return;

                case AbilityType.MagicAttackRate:
                    if (addAbility)
                        _skillMagicHittingChance += abilityValue;
                    else
                        _skillMagicHittingChance -= abilityValue;
                    return;

                case AbilityType.MagicEvationRate:
                    if (addAbility)
                        _skillMagicEvasionChance += abilityValue;
                    else
                        _skillMagicEvasionChance -= abilityValue;
                    return;

                case AbilityType.CriticalAttackRate:
                    if (addAbility)
                        _skillCriticalHittingChance += abilityValue;
                    else
                        _skillCriticalHittingChance -= abilityValue;
                    return;

                case AbilityType.PhysicalAttackPower:
                case AbilityType.ShootingAttackPower:
                    if (addAbility)
                        _skillPhysicalAttackPower += abilityValue;
                    else
                        _skillPhysicalAttackPower -= abilityValue;
                    return;

                case AbilityType.MagicAttackPower:
                    if (addAbility)
                        _skillMagicAttackPower += abilityValue;
                    else
                        _skillMagicAttackPower -= abilityValue;
                    return;

                case AbilityType.Str:
                    if (addAbility)
                       StatsManager.ExtraStr += abilityValue;
                    else
                        StatsManager.ExtraStr -= abilityValue;

                    SendAdditionalStats();
                    return;

                case AbilityType.Rec:
                    if (addAbility)
                        StatsManager.ExtraRec += abilityValue;
                    else
                        StatsManager.ExtraRec -= abilityValue;

                    SendAdditionalStats();
                    return;

                case AbilityType.Int:
                    if (addAbility)
                        StatsManager.ExtraInt += abilityValue;
                    else
                        StatsManager.ExtraInt -= abilityValue;

                    SendAdditionalStats();
                    return;

                case AbilityType.Wis:
                    if (addAbility)
                        StatsManager.ExtraWis += abilityValue;
                    else
                        StatsManager.ExtraWis -= abilityValue;

                    SendAdditionalStats();
                    return;

                case AbilityType.Dex:
                    if (addAbility)
                        StatsManager.ExtraDex += abilityValue;
                    else
                        StatsManager.ExtraDex -= abilityValue;

                    SendAdditionalStats();
                    return;

                case AbilityType.Luc:
                    if (addAbility)
                        StatsManager.ExtraLuc += abilityValue;
                    else
                        StatsManager.ExtraLuc -= abilityValue;

                    SendAdditionalStats();
                    return;

                case AbilityType.HP:
                    if (addAbility)
                        HealthManager.ExtraHP += abilityValue;
                    else
                        HealthManager.ExtraHP -= abilityValue;
                    break;

                case AbilityType.MP:
                    if (addAbility)
                        HealthManager.ExtraMP += abilityValue;
                    else
                        HealthManager.ExtraMP -= abilityValue;
                    break;

                case AbilityType.SP:
                    if (addAbility)
                        HealthManager.ExtraSP += abilityValue;
                    else
                        HealthManager.ExtraSP -= abilityValue;
                    break;

                case AbilityType.PhysicalDefense:
                case AbilityType.ShootingDefense:
                    if (addAbility)
                        StatsManager.ExtraDefense += abilityValue;
                    else
                        StatsManager.ExtraDefense -= abilityValue;

                    SendAdditionalStats();
                    return;

                case AbilityType.MagicResistance:
                    if (addAbility)
                        StatsManager.ExtraResistance += abilityValue;
                    else
                        StatsManager.ExtraResistance -= abilityValue;

                    SendAdditionalStats();
                    return;

                case AbilityType.MoveSpeed:
                    if (addAbility)
                        MoveSpeed += abilityValue;
                    else
                        MoveSpeed -= abilityValue;
                    return;

                case AbilityType.AttackSpeed:
                    if (addAbility)
                        SetAttackSpeedModifier(abilityValue);
                    else
                        SetAttackSpeedModifier(-1 * abilityValue);
                    return;

                case AbilityType.AbsorptionAura:
                    if (addAbility)
                        StatsManager.Absorption += abilityValue;
                    else
                        StatsManager.Absorption -= abilityValue;
                    return;

                default:
                    throw new NotImplementedException($"Not implemented ability type {abilityType}");
            }
        }

        private void Buff_OnPeriodicalHeal(ActiveBuff buff, AttackResult healResult)
        {
            HealthManager.IncreaseHP(healResult.Damage.HP);
            HealthManager.CurrentMP += healResult.Damage.MP;
            HealthManager.CurrentSP += healResult.Damage.SP;

            OnSkillKeep?.Invoke(this, buff, healResult);
        }

        private void Buff_OnPeriodicalDebuff(ActiveBuff buff, AttackResult debuffResult)
        {
            var damage = debuffResult.Damage;

            if (buff.TimeDamageType == TimeDamageType.Percent)
            {
                damage = new Damage(
                    Convert.ToUInt16(HealthManager.CurrentHP * debuffResult.Damage.HP * 1.0 / 100),
                    Convert.ToUInt16(HealthManager.CurrentSP * debuffResult.Damage.SP * 1.0 / 100),
                    Convert.ToUInt16(HealthManager.CurrentMP * debuffResult.Damage.MP * 1.0 / 100));
            }

            HealthManager.DecreaseHP(damage.HP, buff.BuffCreator);
            HealthManager.CurrentMP -= damage.MP;
            HealthManager.CurrentSP -= damage.SP;

            OnSkillKeep?.Invoke(this, buff, new AttackResult(AttackSuccess.Normal, damage));
        }

        #endregion

        #region Passive buffs

        /// <summary>
        /// Passive buffs, that increase character characteristic, attack, defense etc.
        /// Don't update it directly, use instead "AddPassiveBuff".
        /// </summary>
        public ObservableRangeCollection<ActiveBuff> PassiveBuffs { get; private set; } = new ObservableRangeCollection<ActiveBuff>();

        private void PassiveBuffs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (ActiveBuff newBuff in e.NewItems)
                {
                    newBuff.OnReset += PassiveBuff_OnReset;
                    ApplyBuffSkill(newBuff);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var buff = (ActiveBuff)e.OldItems[0];
                RelieveBuffSkill(buff);
            }
        }

        private void PassiveBuff_OnReset(ActiveBuff sender)
        {
            sender.OnReset -= PassiveBuff_OnReset;
            PassiveBuffs.Remove(sender);
        }

        #endregion

        #region Element

        /// <inheritdoc />
        public abstract Element DefenceElement { get; }

        /// <inheritdoc />
        public abstract Element AttackElement { get; }

        /// <summary>
        /// Indicator, that shows if defence element should be removed.
        /// </summary>
        protected bool RemoveElement { get; private set; }

        /// <summary>
        /// Element set by skill.
        /// </summary>
        protected Element AttackSkillElement { get; private set; }

        /// <summary>
        /// Element set by skill.
        /// </summary>
        protected Element DefenceSkillElement { get; private set; }

        #endregion

        #region Death

        /// <inheritdoc />
        public event Action<IKillable, IKiller> OnDead;

        /// <summary>
        /// Collection of entities, that made damage to this killable.
        /// </summary>
        public ConcurrentDictionary<IKiller, int> DamageMakers { get; private set; } = new ConcurrentDictionary<IKiller, int>();

        /// <summary>
        /// IKiller, that made max damage.
        /// </summary>
        protected IKiller MaxDamageMaker
        {
            get
            {
                IKiller maxDamageMaker = DamageMakers.First().Key;
                int damage = DamageMakers.First().Value;
                foreach (var dmg in DamageMakers)
                {
                    if (dmg.Value > damage)
                    {
                        damage = dmg.Value;
                        maxDamageMaker = dmg.Key;
                    }
                }

                return maxDamageMaker;
            }
        }

        protected bool _isDead;

        /// <inheritdoc />
        public bool IsDead
        {
            get => _isDead;
            protected set
            {
                _isDead = value;

                if (_isDead)
                {
                    var killer = MaxDamageMaker;
                    OnDead?.Invoke(this, killer);
                    DamageMakers.Clear();

                    // Generate drop.
                    var dropItems = GenerateDrop(killer);
                    if (dropItems.Count > 0 && killer is Character)
                    {
                        var dropOwner = killer as Character;
                        if (dropOwner.Party is null)
                        {
                            AddItemsDropOnMap(dropItems, dropOwner);
                        }
                        else
                        {
                            var notDistributedItems = dropOwner.Party.DistributeDrop(dropItems, dropOwner);
                            AddItemsDropOnMap(notDistributedItems, dropOwner);

                        }
                    }

                    // Update quest.
                    if (this is Mob && killer is Character)
                    {
                        var character = killer as Character;
                        var mob = this as Mob;
                        if (character.Party is null)
                        {
                            character.UpdateQuestMobCount(mob.MobId);
                        }
                        else
                        {
                            foreach (var m in character.Party.Members)
                            {
                                if (m.Map == character.Map)
                                    m.UpdateQuestMobCount(mob.MobId);
                            }
                        }
                    }

                    // Clear buffs.
                    var buffs = ActiveBuffs.Where(b => b.ShouldClearAfterDeath).ToList();
                    foreach (var b in buffs)
                        b.CancelBuff();
                }
            }
        }

        /// <summary>
        /// Add items on map.
        /// </summary>
        private void AddItemsDropOnMap(IList<Item> dropItems, Character owner)
        {
            byte i = 0;
            foreach (var itm in dropItems)
            {
                Map.AddItem(new MapItem(itm, owner, PosX + i, PosY, PosZ));
                i++;
            }
        }

        /// <summary>
        /// Generates drop for killer.
        /// </summary>
        protected abstract IList<Item> GenerateDrop(IKiller killer);

        #endregion

        #region Position

        /// <inheritdoc />
        public float PosX { get; set; }

        /// <inheritdoc />
        public float PosY { get; set; }

        /// <inheritdoc />
        public float PosZ { get; set; }

        /// <inheritdoc />
        public ushort Angle { get; protected set; }

        #endregion

        #region Defense & Resistance

        public abstract int Defense { get; }

        public abstract int Resistance { get; }

        #endregion

        #region Hitting chances

        /// <summary>
        /// Possibility to hit enemy.
        /// </summary>
        public double PhysicalHittingChance
        {
            get
            {
                var calculated = 1.0 * StatsManager.TotalDex / 2 + _skillPhysicalHittingChance;
                return calculated > 0 ? calculated : 1;
            }
        }

        /// <summary>
        /// Possibility to escape hit.
        /// </summary>
        public double PhysicalEvasionChance
        {
            get
            {
                var calculated = 1.0 * StatsManager.TotalDex / 2 + _skillPhysicalEvasionChance;
                return calculated > 0 ? calculated : 1;
            }
        }

        /// <summary>
        /// Possibility to make critical hit.
        /// </summary>
        public double CriticalHittingChance
        {
            get
            {
                // each 5 luck is 1% of critical.
                var calculated = 0.2 * StatsManager.TotalLuc + _skillCriticalHittingChance;
                return calculated > 0 ? calculated : 1;
            }
        }

        /// <summary>
        /// Possibility to hit enemy.
        /// </summary>
        public double MagicHittingChance
        {
            get
            {
                var calculated = 1.0 * StatsManager.TotalWis / 2 + _skillMagicHittingChance;
                return calculated > 0 ? calculated : 1;
            }
        }

        /// <summary>
        /// Possibility to escape hit.
        /// </summary>
        public double MagicEvasionChance
        {
            get
            {
                var calculated = 1.0 * StatsManager.TotalWis / 2 + _skillMagicEvasionChance;
                return calculated > 0 ? calculated : 1;
            }
        }

        /// <summary>
        /// Possibility to hit enemy gained from skills.
        /// </summary>
        protected double _skillPhysicalHittingChance;

        /// <summary>
        /// Possibility to escape hit gained from skills.
        /// </summary>
        protected double _skillPhysicalEvasionChance;

        /// <summary>
        /// Possibility to make critical hit.
        /// </summary>
        protected double _skillCriticalHittingChance;

        /// <summary>
        /// Possibility to hit enemy gained from skills.
        /// </summary>
        protected double _skillMagicHittingChance;

        /// <summary>
        /// Possibility to escape hit gained from skills.
        /// </summary>
        protected double _skillMagicEvasionChance;

        /// <summary>
        /// Additional attack power.
        /// </summary>
        protected int _skillPhysicalAttackPower;

        /// <summary>
        /// Additional attack power.
        /// </summary>
        protected int _skillMagicAttackPower;

        /// <summary>
        /// Weapon speed calculated from passive skill. Key is weapon, value is speed modificator.
        /// </summary>
        protected readonly Dictionary<byte, byte> _weaponSpeedPassiveSkillModificator = new Dictionary<byte, byte>();

        #endregion

        #region Move & Attack speed

        /// <summary>
        /// Event, that is fired, when attack or move speed changes.
        /// </summary>
        public event Action<IKillable> OnAttackOrMoveChanged;

        protected void InvokeAttackOrMoveChanged()
        {
            OnAttackOrMoveChanged?.Invoke(this);
        }

        public abstract AttackSpeed AttackSpeed { get; }

        /// <summary>
        /// Attack speed modifier is made of equipment and buffs.
        /// </summary>
        protected int _attackSpeedModifier;

        /// <summary>
        /// Sets attack modifier.
        /// </summary>
        protected void SetAttackSpeedModifier(int speed)
        {
            if (speed == 0)
                return;

            _attackSpeedModifier += speed;
            InvokeAttackOrMoveChanged();
        }

        /// <summary>
        /// How fast killable changes its' position.
        /// </summary>
        public abstract int MoveSpeed { get; protected set; }

        #endregion

        #region Untouchable

        ///  <inheritdoc/>
        public virtual bool IsUntouchable { get; private set; }

        #endregion

        #region Absorption

        public ushort Absorption { get => StatsManager.Absorption; }

        #endregion

        #region Resurrect

        /// <inheritdoc />
        public event Action<IKillable> OnRebirthed;

        /// <inheritdoc />
        public void Rebirth(ushort mapId, float x, float y, float z)
        {
            HealthManager.IncreaseHP(HealthManager.MaxHP);
            HealthManager.CurrentMP = HealthManager.MaxMP;
            HealthManager.CurrentSP = HealthManager.MaxSP;
            IsDead = false;

            PosX = x;
            PosY = y;
            PosZ = z;

            OnRebirthed?.Invoke(this);

            if (mapId != Map.Id)
            {
                (this as Character).Teleport(mapId, x, y, z);
            }
        }

        #endregion

        #region Full Recover

        /// <summary>
        /// Event if fired, when killable is recovered.
        /// </summary>
        public event Action<IKillable> OnFullRecover;

        /// <summary>
        /// Fully recovers hitpoints.
        /// </summary>
        public void FullRecover()
        {
            HealthManager.IncreaseHP(HealthManager.MaxHP);
            HealthManager.CurrentMP = HealthManager.MaxMP;
            HealthManager.CurrentSP = HealthManager.MaxSP;
            OnFullRecover?.Invoke(this);
        }

        #endregion
    }
}
