using System.Collections.Generic;
using System.Globalization;
using Sulakore.Habbo;
using Sulakore.Protocol;

namespace PleaseNoLag {
    public class HFloorObject : HData {
        public int Id { get; set; }
        public int TypeId { get; set; }
        public HPoint Tile { get; set; }
        public HDirection Facing { get; set; }

        public int Category { get; set; }
        public object[] Stuff { get; set; }

        public int SecondsToExpiration { get; set; }
        public HUsagePolicy UsagePolicy { get; set; }

        public int OwnerId { get; set; }
        public string OwnerName { get; set; }

        public HFloorObject(HMessage packet)
        {
            Id = packet.ReadInteger();
            TypeId = packet.ReadInteger();

            var tile = new HPoint(packet.ReadInteger(), packet.ReadInteger());
            Facing = (HDirection)packet.ReadInteger();

            tile.Z = double.Parse(packet.ReadString(), CultureInfo.InvariantCulture);
            Tile = tile;

            packet.ReadString();
            packet.ReadInteger();

            Category = packet.ReadInteger();
            Stuff = ReadData(packet, Category);

            SecondsToExpiration = packet.ReadInteger();
            UsagePolicy = (HUsagePolicy)packet.ReadInteger();

            OwnerId = packet.ReadInteger();
            if (TypeId < 0)
            {
                packet.ReadString();
            }
        }

        public static HFloorObject[] Parse(HMessage packet)
        {
            int ownersCount = packet.ReadInteger();
            var owners = new Dictionary<int, string>(ownersCount);
            for (int i = 0; i < ownersCount; i++)
            {
                owners.Add(packet.ReadInteger(), packet.ReadString());
            }

            var floorObjects = new HFloorObject[packet.ReadInteger()];
            for (int i = 0; i < floorObjects.Length; i++)
            {
                var floorObject = new HFloorObject(packet);
                floorObject.OwnerName = owners[floorObject.OwnerId];

                floorObjects[i] = floorObject;
            }
            return floorObjects;
        }
    }

    public enum HUsagePolicy
    {
        Nobody = 0,
        Controller = 1,
        Everybody = 2
    }
}