﻿using Imgeneus.Database.Constants;
using Imgeneus.Database.Entities;
using Imgeneus.Database.Preload;
using Imgeneus.DatabaseBackgroundService;
using Imgeneus.World.Game.Monster;
using Imgeneus.World.Game.Player;
using Imgeneus.World.Game.Zone;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;

namespace Imgeneus.World.Tests
{
    public abstract class BaseTest
    {
        protected Mock<ILogger<Character>> loggerMock = new Mock<ILogger<Character>>();
        protected Mock<IBackgroundTaskQueue> taskQueuMock = new Mock<IBackgroundTaskQueue>();
        protected Mock<IDatabasePreloader> databasePreloader = new Mock<IDatabasePreloader>();
        protected Mock<ICharacterConfiguration> config = new Mock<ICharacterConfiguration>();
        protected Mock<ILogger<Map>> mapLoggerMock = new Mock<ILogger<Map>>();
        protected Mock<ILogger<Mob>> mobLoggerMock = new Mock<ILogger<Mob>>();

        public BaseTest()
        {
            config.Setup((conf) => conf.GetConfig(It.IsAny<int>()))
                .Returns(new Character_HP_SP_MP() { HP = 100, MP = 200, SP = 300 });

            databasePreloader
                .SetupGet((preloader) => preloader.Mobs)
                .Returns(new Dictionary<ushort, DbMob>()
                {
                    { 1, Wolf },
                    { 3041, CrypticImmortal }
                });

            databasePreloader
                .SetupGet((preloader) => preloader.Skills)
                .Returns(new Dictionary<(ushort SkillId, byte SkillLevel), DbSkill>()
                {
                    { (1, 1) , StrengthTraining },
                    { (14, 1), ManaTraining },
                    { (15, 1), SharpenWeaponMastery_Lvl1 },
                    { (15, 2), SharpenWeaponMastery_Lvl2 },
                    { (35, 1), MagicRoots_Lvl1 }
                });
        }

        #region Test mobs

        protected DbMob Wolf = new DbMob()
        {
            Id = 1,
            MobName = "Small Ruined Wolf",
            AI = Database.Constants.MobAI.Combative,
            Level = 38,
            HP = 2765,
            Element = Database.Constants.Element.Wind1,
            AttackSpecial3 = Database.Constants.MobRespawnTime.TestEnv,
            NormalTime = 1
        };

        protected DbMob CrypticImmortal = new DbMob()
        {
            Id = 3041,
            MobName = "Cryptic the Immortal",
            AI = Database.Constants.MobAI.CrypticImmortal,
            Level = 75,
            HP = 35350000,
            AttackOk1 = 1,
            Attack1 = 8822,
            AttackPlus1 = 3222,
            AttackRange1 = 5,
            AttackTime1 = 2500,
            NormalTime = 1,
            ChaseTime = 1
        };

        #endregion

        #region Skills

        protected DbSkill StrengthTraining = new DbSkill()
        {
            SkillId = 1,
            SkillLevel = 1,
            TypeDetail = TypeDetail.PassiveDefence,
            SkillName = "Strength Training Lv1",
            TypeAttack = TypeAttack.Passive,
            AbilityType1 = AbilityType.PhysicalAttackPower,
            AbilityValue1 = 18
        };

        protected DbSkill ManaTraining = new DbSkill()
        {
            SkillId = 14,
            SkillLevel = 1,
            TypeDetail = TypeDetail.PassiveDefence,
            SkillName = "Mana Training",
            TypeAttack = TypeAttack.Passive,
            AbilityType1 = AbilityType.MP,
            AbilityValue1 = 110
        };

        protected DbSkill SharpenWeaponMastery_Lvl1 = new DbSkill()
        {
            SkillId = 15,
            SkillLevel = 1,
            TypeDetail = TypeDetail.WeaponMastery,
            SkillName = "Sharpen Weapon Mastery Lvl 1",
            TypeAttack = TypeAttack.Passive,
            Weapon1 = 1,
            Weapon2 = 3,
            Weaponvalue = 1
        };

        protected DbSkill SharpenWeaponMastery_Lvl2 = new DbSkill()
        {
            SkillId = 15,
            SkillLevel = 2,
            TypeDetail = TypeDetail.WeaponMastery,
            SkillName = "Sharpen Weapon Mastery Lvl 2",
            TypeAttack = TypeAttack.Passive,
            Weapon1 = 1,
            Weapon2 = 3,
            Weaponvalue = 2
        };

        protected DbSkill MagicRoots_Lvl1 = new DbSkill()
        {
            SkillId = 35,
            SkillLevel = 1,
            TypeDetail = TypeDetail.Immobilize,
            SkillName = "Magic Roots",
            DamageHP = 42,
            TypeAttack = TypeAttack.MagicAttack,
            ResetTime = 10,
            KeepTime = 5,
            DamageType = DamageType.PlusExtraDamage,
        };

        #endregion
    }
}
