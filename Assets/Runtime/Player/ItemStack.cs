namespace Runtime.Player
{
    [System.Serializable]
    public struct ItemStack
    {
        public ItemType type;
        public int amount;

        public ItemStack(ItemType type, int amount) : this()
        {
            this.type = type;
            this.amount = amount;
        }

        public ItemStack(ItemStack other) : this(other.type, other.amount) { }
    }

}