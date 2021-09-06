namespace Server.Items
{
    [Serializable(0, false)]
    public partial class WallBlood : Item
    {
        [Constructible]
        public WallBlood()
            : base(Utility.RandomBool() ? 0x1D95 : 0x1D94)
        {
        }
    }
}
