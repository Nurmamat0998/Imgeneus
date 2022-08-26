﻿using Microsoft.Extensions.Logging;
using Parsec;
using Parsec.Common;
using Parsec.Shaiya.Item;
using Parsec.Shaiya.NpcQuest;
using Parsec.Shaiya.NpcSkill;
using Parsec.Shaiya.Skill;

namespace Imgeneus.GameDefinitions
{
    public class GameDefinitionsPreloder : IGameDefinitionsPreloder
    {
        private readonly ILogger<GameDefinitionsPreloder> _logger;

        public Dictionary<(byte Type, byte TypeId), DbItem> Items { get; init; } = new();
        public Dictionary<ushort, List<DbItem>> ItemsByGrade { get; init; } = new();

        public Dictionary<(ushort SkillId, byte SkillLevel), DbSkill> Skills { get; private set; } = new();

        public Dictionary<(NpcType Type, short TypeId), BaseNpc> NPCs { get; init; } = new();
        public Dictionary<short, Quest> Quests { get; init; } = new();

        public GameDefinitionsPreloder(ILogger<GameDefinitionsPreloder> logger)
        {
            _logger = logger;
        }

        public void Preload()
        {
            try
            {
                PreloadItems();
                PreloadSkills();
                //PreloadMobs(_database);
                //PreloadMobItems(_database);
                PreloadNpcsAndQuests();
                //PreloadLevels(_database);

                _logger.LogInformation("Game definitions were successfully preloaded.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during preloading game definitions: {ex.Message}");
            }
        }

        /// <summary>
        /// Preloads all available items from DBItemData.SData.
        /// </summary>
        private void PreloadItems()
        {
            var items = Reader.ReadFromFile<DBItemData>("config/SData/DBItemData.SData");

            foreach (var item in items.Records)
            {
                var dbItem = new DbItem(item);
                Items.Add((dbItem.Type, dbItem.TypeId), dbItem);
                if (ItemsByGrade.ContainsKey(dbItem.Grade))
                {
                    ItemsByGrade[dbItem.Grade].Add(dbItem);
                }
                else
                {
                    ItemsByGrade.Add(dbItem.Grade, new List<DbItem>() { dbItem });
                }
            }
        }

        /// <summary>
        /// Preloads all available skills from DBSkillData.SData.
        /// </summary>
        private void PreloadSkills()
        {
            var skills = Reader.ReadFromFile<DBSkillData>("config/SData/DBSkillData.SData");
            for (var i = 0; i < skills.Records.Count; i++)
            {
                var skill = skills.Records[i];
                if (skill.SkillId < 0 || skill.SkillId > ushort.MaxValue)
                {
                    _logger.LogWarning("Skill id is incorrect {id}", skill.SkillId);
                    continue;
                }

                if (skill.SkillLevel < 0 || skill.SkillLevel > byte.MaxValue)
                {
                    _logger.LogWarning("Skill level is incorrect {level}", skill.SkillLevel);
                    continue;
                }

                Skills.Add(((ushort)skill.SkillId, (byte)skill.SkillLevel), new DbSkill(skill));
            }

            var mobSkills = Reader.ReadFromFile<DBNpcSkillData>("config/SData/DBNpcSkillData.SData");
            for (var i = 0; i < mobSkills.Records.Count; i++)
            {
                var skill = mobSkills.Records[i];
                if (skill.SkillId < 0 || skill.SkillId > ushort.MaxValue)
                {
                    _logger.LogWarning("Mob skill id is incorrect {id}", skill.SkillId);
                    continue;
                }

                if (skill.SkillLevel < 0 || skill.SkillLevel > byte.MaxValue)
                {
                    _logger.LogWarning("Mob skill level is incorrect {level}", skill.SkillLevel);
                    continue;
                }

                skill.SkillLevel = 100; // 100 is default level for mob's skills.

                Skills.Add(((ushort)skill.SkillId, (byte)skill.SkillLevel), new DbSkill(skill));
            }
        }

        /// <summary>
        /// Preloads all available quests from NpcQuest.SData.
        /// </summary>
        private void PreloadNpcsAndQuests()
        {
            var npcQuest = Reader.ReadFromFile<NpcQuest>("config/SData/NpcQuest.SData", Episode.EP8);

            foreach (var npc in npcQuest.Merchants)
                NPCs.Add((npc.Type, npc.TypeId), npc);

            foreach (var npc in npcQuest.Gatekeepers)
                NPCs.Add((npc.Type, npc.TypeId), npc);

            foreach (var npc in npcQuest.Blacksmiths)
                NPCs.Add((npc.Type, npc.TypeId), npc);

            foreach (var npc in npcQuest.PvpManagers)
                NPCs.Add((npc.Type, npc.TypeId), npc);

            foreach (var npc in npcQuest.GamblingHouses)
                NPCs.Add((npc.Type, npc.TypeId), npc);

            foreach (var npc in npcQuest.Warehouses)
                NPCs.Add((npc.Type, npc.TypeId), npc);

            foreach (var npc in npcQuest.NormalNpcs)
                NPCs.Add((npc.Type, npc.TypeId), npc);

            foreach (var npc in npcQuest.Guards)
                NPCs.Add((npc.Type, npc.TypeId), npc);

            foreach (var npc in npcQuest.Animals)
                NPCs.Add((npc.Type, npc.TypeId), npc);

            foreach (var npc in npcQuest.Apprentices)
                NPCs.Add((npc.Type, npc.TypeId), npc);

            foreach (var npc in npcQuest.GuildMasters)
                NPCs.Add((npc.Type, npc.TypeId), npc);

            foreach (var npc in npcQuest.DeadNpcs)
                NPCs.Add((npc.Type, npc.TypeId), npc);

            foreach (var npc in npcQuest.CombatCommanders)
                NPCs.Add((npc.Type, npc.TypeId), npc);

            foreach (var quest in npcQuest.Quests)
                Quests.Add(quest.Id, quest);
        }
    }
}