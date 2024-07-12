using FishNet.Serializing;
using Runtime.Player;

namespace Runtime.Networking
{
    public static class ItemTypeSerializer
    {
        public static void WriteItemType(this Writer writer, ItemType value) => writer.WriteInt32(ItemRegister.instance[value]);

        public static ItemType ReadItemType(this Reader reader) => ItemRegister.instance[reader.ReadInt32()];
    }
}