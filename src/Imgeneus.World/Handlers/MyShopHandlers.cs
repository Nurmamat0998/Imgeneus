﻿using Imgeneus.Network.Packets;
using Imgeneus.Network.Packets.Game;
using Imgeneus.World.Game;
using Imgeneus.World.Game.Session;
using Imgeneus.World.Game.Shop;
using Imgeneus.World.Packets;
using Sylver.HandlerInvoker.Attributes;

namespace Imgeneus.World.Handlers
{
    [Handler]
    public class MyShopHandlers : BaseHandler
    {
        private readonly IShopManager _shopManager;
        private readonly IGameWorld _gameWorld;

        public MyShopHandlers(IGamePacketFactory packetFactory, IGameSession gameSession, IShopManager shopManager, IGameWorld gameWorld) : base(packetFactory, gameSession)
        {
            _shopManager = shopManager;
            _gameWorld = gameWorld;
        }

        [HandlerAction(PacketType.MY_SHOP_BEGIN)]
        public void HandleBegin(WorldClient client, MyShopBeginPacket packet)
        {
            var ok = _shopManager.TryBegin();
            if (ok)
                _packetFactory.SendMyShopBegin(client);
        }

        [HandlerAction(PacketType.MY_SHOP_ADD_ITEM)]
        public void HandleAddItem(WorldClient client, MyShopAddItemPacket packet)
        {
            var ok = _shopManager.TryAddItem(packet.Bag, packet.Slot, packet.ShopSlot, packet.Price);
            if (ok)
                _packetFactory.SendMyShopAddItem(client, packet.Bag, packet.Slot, packet.ShopSlot, packet.Price);
        }

        [HandlerAction(PacketType.MY_SHOP_REMOVE_ITEM)]
        public void HandleRemoveItem(WorldClient client, MyShopRemoveItemPacket packet)
        {
            var ok = _shopManager.TryRemoveItem(packet.ShopSlot);
            if (ok)
                _packetFactory.SendMyShopRemoveItem(client, packet.ShopSlot);
        }

        [HandlerAction(PacketType.MY_SHOP_START)]
        public void HandleStart(WorldClient client, MyShopStartPacket packet)
        {
            var ok = _shopManager.TryStart(packet.Name);
            if (ok)
                _packetFactory.SendMyShopStarted(client);
        }

        [HandlerAction(PacketType.MY_SHOP_CANCEL)]
        public void HandleCancel(WorldClient client, MyShopCancelPacket packet)
        {
            var ok = _shopManager.TryCancel();
            if (ok)
                _packetFactory.SendMyShopCanceled(client);
        }

        [HandlerAction(PacketType.MY_SHOP_END)]
        public void HandleEnd(WorldClient client, MyShopEndPacket packet)
        {
            var ok = _shopManager.TryEnd();
            if (ok)
                _packetFactory.SendMyShopEnded(client);
        }

        [HandlerAction(PacketType.MY_SHOP_VISIT)]
        public void HandleItemList(WorldClient client, MyShopItemListPacket packet)
        {
            if (!_gameWorld.Players.TryGetValue(packet.CharacterId, out var player))
                return;

            if (!player.ShopManager.IsShopOpened)
                return;

            _packetFactory.SendMyShopVisit(client, true, player.Id);
            _packetFactory.SendMyShopItems(client, player.ShopManager.Items);
        }
    }
}