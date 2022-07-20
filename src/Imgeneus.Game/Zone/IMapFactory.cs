﻿using Imgeneus.Game.Monster;
using Imgeneus.World.Game.PartyAndRaid;
using Imgeneus.World.Game.Zone.MapConfig;
using Imgeneus.World.Game.Zone.Obelisks;
using Parsec.Shaiya.Svmap;
using System.Collections.Generic;

namespace Imgeneus.World.Game.Zone
{
    public interface IMapFactory
    {
        /// <summary>
        /// Creates map instance.
        /// </summary>
        /// <param name="id">map id</param>
        /// <param name="definition">some map settings</param>
        /// <param name="config">size, mobs, npcs etc.</param>
        /// <returns>map instance</returns>
        public IMap CreateMap(ushort id, MapDefinition definition, Svmap config, IEnumerable<ObeliskConfiguration> obelisks = null, IEnumerable<BossConfiguration> bosses = null);

        /// <summary>
        /// Creates map instance only for party.
        /// </summary>
        /// <param name="id">map id</param>
        /// <param name="definition">some map settings</param>
        /// <param name="config">size, mobs, npcs etc.</param>
        /// <param name="party">party instance</param>
        /// <returns>map instance</returns>
        public IPartyMap CreatePartyMap(ushort id, MapDefinition definition, Svmap config, IParty party, IEnumerable<BossConfiguration> bosses = null);

        /// <summary>
        /// Creates map instance only for guild.
        /// </summary>
        /// <param name="id">map id</param>
        /// <param name="definition">some map settings</param>
        /// <param name="config">size, mobs, npcs etc.</param>
        /// <param name="guildId">guild id</param>
        /// <returns>map instance</returns>
        public IGuildMap CreateGuildMap(ushort id, MapDefinition definition, Svmap config, uint guildId, IEnumerable<BossConfiguration> bosses = null);
    }
}
