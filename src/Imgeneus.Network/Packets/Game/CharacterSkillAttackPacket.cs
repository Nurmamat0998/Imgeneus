﻿using Imgeneus.Network.PacketProcessor;

namespace Imgeneus.Network.Packets.Game
{
    public record CharacterSkillAttackPacket : IPacketDeserializer
    {
        public byte Number { get; private set; }

        public int TargetId { get; private set; }

        public void Deserialize(ImgeneusPacket packetStream)
        {
            Number = packetStream.Read<byte>();
            TargetId = packetStream.Read<int>();
        }
    }
}
