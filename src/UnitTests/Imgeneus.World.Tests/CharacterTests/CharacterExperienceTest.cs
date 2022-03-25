using System.ComponentModel;
using System.Threading.Tasks;
using Imgeneus.Database.Entities;
using Xunit;

namespace Imgeneus.World.Tests.CharacterTests
{
    public class CharacterExperienceTest : BaseTest
    {
        [Fact]
        [Description("Character level should be updated with experience changes.")]
        public void LevelFromExperienceTest()
        {
            var character = CreateCharacter();

            character.AdditionalInfoManager.Grow = Mode.Ultimate;
            character.LevelingManager.TryChangeLevel(1);
            character.LevelingManager.TryChangeExperience(0);

            Assert.Equal((uint)0, character.LevelingManager.Exp);

            character.LevelingManager.TryChangeExperience(200);

            Assert.Equal((uint)200, character.LevelingManager.Exp);
            Assert.Equal(2, character.LevelProvider.Level);
        }

        [Fact]
        [Description("Character shouldn't receive experience if already at maximum level")]
        public void ExperienceBoundaryTest()
        {
            var character = CreateCharacter();

            var maxLevel = config.Object.GetMaxLevelConfig(Mode.Ultimate).Level;

            character.AdditionalInfoManager.Grow = Mode.Ultimate;
            character.LevelingManager.TryChangeLevel(maxLevel);

            Assert.Equal(maxLevel, character.LevelProvider.Level);

            var maxLevelExp = databasePreloader.Object.Levels[(character.AdditionalInfoManager.Grow, character.LevelProvider.Level)].Exp;

            Assert.Equal(maxLevelExp, character.LevelingManager.Exp);
            character.LevelingManager.AddMobExperience(maxLevel, 10);
            Assert.Equal(maxLevelExp, character.LevelingManager.Exp);
        }

        [Fact]
        [Description("Character should receive experience by killing a mob according to the level difference.")]
        public void ExperienceGainFromMobTest()
        {
            var map = testMap;
            var mob = CreateMob(Wolf.Id, map);

            var character = CreateCharacter();

            character.AdditionalInfoManager.Grow = Mode.Ultimate;
            character.LevelProvider.Level = mob.LevelProvider.Level;

            var previousExp = character.LevelingManager.Exp;

            map.LoadPlayer(character);
            map.AddMob(mob);
            mob.HealthManager.DecreaseHP(20000000, character);

            Assert.True(mob.HealthManager.IsDead);

            var expectedExperience = previousExp + 120;

            Assert.Equal(expectedExperience, character.LevelingManager.Exp);
        }
    }
}
