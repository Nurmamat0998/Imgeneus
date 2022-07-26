﻿using Imgeneus.Database.Constants;
using Imgeneus.Game.Market;
using Imgeneus.Network.Packets;
using Imgeneus.Network.Packets.Game;
using Imgeneus.World.Game.Inventory;
using Imgeneus.World.Game.Session;
using Imgeneus.World.Packets;
using Sylver.HandlerInvoker.Attributes;
using System.Threading.Tasks;

namespace Imgeneus.World.Handlers
{
    [Handler]
    public class MarketHandlers : BaseHandler
    {
        private readonly IMarketManager _marketManager;
        private readonly IInventoryManager _inventoryManager;

        public MarketHandlers(IGamePacketFactory packetFactory, IGameSession gameSession, IMarketManager marketManager, IInventoryManager inventoryManager) : base(packetFactory, gameSession)
        {
            _marketManager = marketManager;
            _inventoryManager = inventoryManager;
        }

        [HandlerAction(PacketType.MARKET_GET_SELL_LIST)]
        public async Task SellListHandle(WorldClient client, EmptyPacket packet)
        {
            var items = await _marketManager.GetSellItems();
            _packetFactory.SendMarketSellList(client, items);
        }

        [HandlerAction(PacketType.MARKET_GET_TENDER_LIST)]
        public void TenderListHandle(WorldClient client, EmptyPacket packet)
        {
            _packetFactory.SendMarketTenderList(client);
        }

        [HandlerAction(PacketType.MARKET_REGISTER_ITEM)]
        public async Task RegisterItemHandle(WorldClient client, MarketRegisterItemPacket packet)
        {
            var result = await _marketManager.TryRegisterItem(packet.Bag, packet.Slot, packet.Count, (MarketType)packet.MarketType, packet.MinMoney, packet.DirectMoney);
            _packetFactory.SendMarketItemRegister(client, result.Ok, result.MarketItem, result.Item, _inventoryManager.Gold);
        }

        [HandlerAction(PacketType.MARKET_UNREGISTER_ITEM)]
        public async Task UnregisterItemHandle(WorldClient client, MarketUnregisterItemPacket packet)
        {
            var result = await _marketManager.TryUnregisterItem(packet.MarketId);
            _packetFactory.SendMarketItemUnregister(client, result.Ok, result.Result);
        }
    }
}
