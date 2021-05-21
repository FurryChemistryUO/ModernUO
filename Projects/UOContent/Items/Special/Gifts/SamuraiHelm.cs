namespace Server.Items
{
    [Flippable(0x236C, 0x236D)]
    public class SamuraiHelm : BaseArmor
    {
        [Constructible]
        public SamuraiHelm() : base(0x236C)
        {
            Weight = 5.0;
            LootType = LootType.Blessed;

            Attributes.DefendChance = 15;
            ArmorAttributes.SelfRepair = 10;
            ArmorAttributes.LowerStatReq = 100;
            ArmorAttributes.MageArmor = 1;
        }

        public SamuraiHelm(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1062923; // Ancient Samurai Helm

        public override int BasePhysicalResistance => 15;
        public override int BaseFireResistance => 10;
        public override int BaseColdResistance => 10;
        public override int BasePoisonResistance => 15;
        public override int BaseEnergyResistance => 10;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
