﻿using Imgeneus.Database.Constants;
using Imgeneus.Network.Data;
using Imgeneus.Network.Packets;
using Imgeneus.Network.Packets.Game;
using Imgeneus.Network.Server;
using Imgeneus.World.Game.Zone;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Imgeneus.World.Game.Player
{
    public partial class Character
    {
        private IWorldClient _client;

        /// <summary>
        /// TCP connection with client.
        /// </summary>
        public IWorldClient Client
        {
            get => _client;

            set
            {
                if (_client is null)
                {
                    _client = value;
                    //_client.OnPacketArrived += Client_OnPacketArrived;
                }
                else
                {
                    throw new ArgumentException("TCP connection can not be set twice");
                }
            }
        }

        /// <summary>
        /// Removes TCP connection.
        /// </summary>
        public void ClearConnection()
        {
            //_client.OnPacketArrived -= Client_OnPacketArrived;
            _client = null;
        }

        /// <summary>
        /// Tries to handle all packets, that client sends.
        /// </summary>
        /// <param name="sender">TCP connection with client</param>
        /// <param name="packet">packet, that clients sends</param>
        /*private void Client_OnPacketArrived(ServerClient sender, IDeserializedPacket packet)
        {
            switch (packet)
            {                            

                case GMNoticeMapPacket gmNoticeMapPacket:
                    if (!IsAdmin)
                        return;

                    _noticeManager.SendMapNotice(gmNoticeMapPacket.Message, this.MapId, gmNoticeMapPacket.TimeInterval);
                    _packetsHelper.SendGmCommandSuccess(Client);
                    break;
            }
        }*/
    }
}
