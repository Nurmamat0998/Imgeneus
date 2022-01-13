﻿using Imgeneus.Database;
using Imgeneus.Database.Preload;
using Imgeneus.DatabaseBackgroundService;
using Imgeneus.World.Game.Chat;
using Imgeneus.World.Game.Dyeing;
using Imgeneus.World.Game.Linking;
using Imgeneus.World.Game.Monster;
using Imgeneus.World.Game.NPCs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Imgeneus.World.Game.Notice;
using Imgeneus.World.Game.Zone.MapConfig;
using Imgeneus.World.Game.Guild;
using Imgeneus.World.Game.Player.Config;
using Imgeneus.World.Game.Inventory;
using Imgeneus.World.Game.Stats;
using Imgeneus.World.Game.Session;
using Imgeneus.World.Game.Stealth;
using Imgeneus.World.Game.Health;
using Imgeneus.World.Game.Levelling;

namespace Imgeneus.World.Game.Player
{
    public class CharacterFactory : ICharacterFactory
    {
        private readonly ILogger<ICharacterFactory> _logger;
        private readonly IDatabase _database;
        private readonly ILogger<Character> _characterLogger;
        private readonly IGameWorld _gameWorld;
        private readonly ICharacterConfiguration _characterConfiguration;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly IDatabasePreloader _databasePreloader;
        private readonly IMapsLoader _mapsLoader;
        private readonly IStatsManager _statsManager;
        private readonly IHealthManager _healthManager;
        private readonly ILevelProvider _levelProvider;
        private readonly ILevelingManager _levelingManager;
        private readonly IInventoryManager _inventoryManager;
        private readonly IChatManager _chatManager;
        private readonly ILinkingManager _linkingManager;
        private readonly IDyeingManager _dyeingManager;
        private readonly IMobFactory _mobFactory;
        private readonly INpcFactory _npcFactory;
        private readonly INoticeManager _noticeManager;
        private readonly IGuildManager _guildManager;
        private readonly IGameSession _gameSession;
        private readonly IStealthManager _stealthManager;

        public CharacterFactory(ILogger<ICharacterFactory> logger,
                                IDatabase database,
                                ILogger<Character> characterLogger,
                                IGameWorld gameWorld,
                                ICharacterConfiguration characterConfiguration,
                                IBackgroundTaskQueue backgroundTaskQueue,
                                IDatabasePreloader databasePreloader,
                                IMapsLoader mapsLoader,
                                IStatsManager statsManager,
                                IHealthManager healthManager,
                                ILevelProvider levelProvider,
                                ILevelingManager levelingManager,
                                IInventoryManager inventoryManager,
                                IChatManager chatManager,
                                ILinkingManager linkingManager,
                                IDyeingManager dyeingManager,
                                IMobFactory mobFactory,
                                INpcFactory npcFactory,
                                INoticeManager noticeManager,
                                IGuildManager guildManager,
                                IGameSession gameSession,
                                IStealthManager stealthManager)
        {
            _logger = logger;
            _database = database;
            _characterLogger = characterLogger;
            _gameWorld = gameWorld;
            _characterConfiguration = characterConfiguration;
            _backgroundTaskQueue = backgroundTaskQueue;
            _databasePreloader = databasePreloader;
            _mapsLoader = mapsLoader;
            _statsManager = statsManager;
            _healthManager = healthManager;
            _levelProvider = levelProvider;
            _levelingManager = levelingManager;
            _inventoryManager = inventoryManager;
            _chatManager = chatManager;
            _linkingManager = linkingManager;
            _dyeingManager = dyeingManager;
            _mobFactory = mobFactory;
            _npcFactory = npcFactory;
            _noticeManager = noticeManager;
            _guildManager = guildManager;
            _gameSession = gameSession;
            _stealthManager = stealthManager;
        }

        public async Task<Character> CreateCharacter(int userId, int characterId)
        {
            var dbCharacter = await _database.Characters.Include(c => c.Skills).ThenInclude(cs => cs.Skill)
                                                        .Include(c => c.Items).ThenInclude(ci => ci.Item)
                                                        .Include(c => c.ActiveBuffs).ThenInclude(cb => cb.Skill)
                                                        .Include(c => c.Friends).ThenInclude(cf => cf.Friend)
                                                        .Include(c => c.Guild).ThenInclude(g => g.Members)
                                                        .Include(c => c.Quests)
                                                        .Include(c => c.QuickItems)
                                                        .Include(c => c.User)
                                                        .ThenInclude(c => c.BankItems)
                                                        .FirstOrDefaultAsync(c => c.UserId == userId && c.Id == characterId);

            if (dbCharacter is null)
            {
                _logger.LogWarning($"Character with id {characterId} is not found.");
                return null;
            }

            Character.ClearOutdatedValues(_database, dbCharacter);

            _gameSession.CharId = dbCharacter.Id;
            _gameSession.IsAdmin = dbCharacter.User.Authority == 0;

            _statsManager.Init(dbCharacter.Id, dbCharacter.Strength, dbCharacter.Dexterity, dbCharacter.Rec, dbCharacter.Intelligence, dbCharacter.Wisdom, dbCharacter.Luck, dbCharacter.StatPoint);

            _levelProvider.Level = dbCharacter.Level;

            _levelingManager.Init();

            _healthManager.Init(dbCharacter.Id, dbCharacter.HealthPoints, dbCharacter.StaminaPoints, dbCharacter.ManaPoints, profession: dbCharacter.Class);

            _inventoryManager.Init(dbCharacter.Items);

            _stealthManager.IsAdminStealth = dbCharacter.User.Authority == 0;

            var player = Character.FromDbCharacter(dbCharacter,
                                        _characterLogger,
                                        _gameWorld,
                                        _characterConfiguration,
                                        _backgroundTaskQueue,
                                        _databasePreloader,
                                        _mapsLoader,
                                        _statsManager,
                                        _healthManager,
                                        _levelProvider,
                                        _levelingManager,
                                        _inventoryManager,
                                        _chatManager,
                                        _linkingManager,
                                        _dyeingManager,
                                        _mobFactory,
                                        _npcFactory,
                                        _noticeManager,
                                        _guildManager,
                                        _stealthManager,
                                        _gameSession);

            player.Client = _gameSession.Client; // TODO: remove it.

            return player;
        }
    }
}
