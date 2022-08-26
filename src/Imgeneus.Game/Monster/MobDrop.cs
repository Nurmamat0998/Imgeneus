﻿using Imgeneus.Database.Entities;
using Imgeneus.World.Game.Inventory;
using Imgeneus.World.Game.NPCs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Imgeneus.World.Game.Monster
{
    public partial class Mob
    {
        private Random _dropRandom = new Random();

        public override IList<Item> GenerateDrop(IKiller killer)
        {
            if (killer is Npc)
                return new List<Item>();

            var items = new List<Item>();

            for (byte i = 1; i <= 9; i++)
            {
                if (!_databasePreloader.MobItems.ContainsKey((MobId, i)))
                {
                    _logger.LogWarning("Mob {id} doesn't contain drop. Is it expected?", MobId);
                    continue;
                }

                var dropItem = _databasePreloader.MobItems[(MobId, i)];
                if (dropItem.Grade != 0 && dropItem.DropRate != 0)
                {
                    var item = GenerateDropItem(dropItem);
                    if (item != null)
                        items.Add(item);
                }
            }

            if (_dbMob.MoneyMax > _dbMob.MoneyMin && _dropRandom.Next(1, 101) <= 40)
            {
                var money = _dropRandom.Next(_dbMob.MoneyMin, _dbMob.MoneyMax);
                var item = new Item(money);
                items.Add(item);
            }

            return items;
        }

        /// <summary>
        /// Generates item from grade, based on drop rate.
        /// </summary>
        /// <returns>item or null if drop rate was too small</returns>
        private Item GenerateDropItem(DbMobItems dropItem)
        {
            if (_dropRandom.Next(1, 101) <= dropItem.DropRate)
            {
                if (!_definitionsPreloader.ItemsByGrade.ContainsKey(dropItem.Grade))
                {
                    _logger.LogWarning("Mob {id} has unknown grade {grade}.", MobId, dropItem.Grade);
                    return null;
                }
                var availableItems = _definitionsPreloader.ItemsByGrade[dropItem.Grade];
                var randomItem = availableItems[_dropRandom.Next(0, availableItems.Count - 1)];
                return new Item(_definitionsPreloader, _enchantConfig, _itemCreateConfig, randomItem.Type, randomItem.TypeId);
            }
            else
            {
                return null;
            }
        }
    }
}
