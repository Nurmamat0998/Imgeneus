﻿using Imgeneus.World.Game.PartyAndRaid;
using Imgeneus.World.Game.Player;
using System.Collections.Generic;
using System.ComponentModel;
using Xunit;

namespace Imgeneus.World.Tests.PartyTests
{
    public class PartyTest : BaseTest
    {
        [Fact]
        [Description("First player, that connected party is its' leader.")]
        public void Party_Leader()
        {
            var character1 = new Character(loggerMock.Object, gameWorldMock.Object, config.Object, taskQueuMock.Object, databasePreloader.Object, chatMock.Object)
            {
                Name = "Character1"
            };
            character1.Client = worldClientMock.Object;
            var character2 = new Character(loggerMock.Object, gameWorldMock.Object, config.Object, taskQueuMock.Object, databasePreloader.Object, chatMock.Object)
            {
                Name = "Character2"
            };
            character2.Client = worldClientMock.Object;
            Assert.False(character1.IsPartyLead);

            var party = new Party();
            character1.SetParty(party);
            character2.SetParty(party);

            Assert.True(character1.IsPartyLead);
            Assert.Equal(character1, party.Leader);
        }

        [Fact]
        [Description("Party drop should be for each player, 1 by 1.")]
        public void Party_DropCalculation()
        {
            var character1 = new Character(loggerMock.Object, gameWorldMock.Object, config.Object, taskQueuMock.Object, databasePreloader.Object, chatMock.Object)
            {
                Name = "Character1"
            };
            character1.Client = worldClientMock.Object;
            var character2 = new Character(loggerMock.Object, gameWorldMock.Object, config.Object, taskQueuMock.Object, databasePreloader.Object, chatMock.Object)
            {
                Name = "Character2"
            };
            character2.Client = worldClientMock.Object;

            var party = new Party();
            character1.SetParty(party);
            character2.SetParty(party);

            party.DistributeDrop(new List<Item>()
            {
                new Item(databasePreloader.Object, WaterArmor.Type, WaterArmor.TypeId),
                new Item(databasePreloader.Object, WaterArmor.Type, WaterArmor.TypeId),
                new Item(databasePreloader.Object, FireSword.Type, FireSword.TypeId)
            }, character2);

            Assert.Equal(2, character1.InventoryItems.Count);
            Assert.Single(character2.InventoryItems);

            Assert.Equal(WaterArmor.Type, character1.InventoryItems[0].Type);
            Assert.Equal(FireSword.Type, character1.InventoryItems[1].Type);

            Assert.Equal(WaterArmor.Type, character2.InventoryItems[0].Type);
        }
    }
}
