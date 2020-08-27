namespace Server.Items
{
  [Flippable(0x2068, 0x207A)]
  public class BoilingCauldronAddon : BaseAddonContainer
  {
    [Constructible]
    public BoilingCauldronAddon() : base(0x2068)
    {
      AddComponent(new LocalizedContainerComponent(0xFAC, 1076267), 0, 0, 0);
      AddComponent(new LocalizedContainerComponent(0x970, 1076267), 0, 0, 8);
    }

    public BoilingCauldronAddon(Serial serial) : base(serial)
    {
    }

    public override BaseAddonContainerDeed Deed => new BoilingCauldronDeed();
    public override int LabelNumber => 1076267; // Boiling Cauldron
    public override int DefaultGumpID => 0x9;
    public override int DefaultDropSound => 0x42;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }

  public class BoilingCauldronDeed : BaseAddonContainerDeed
  {
    [Constructible]
    public BoilingCauldronDeed() => LootType = LootType.Blessed;

    public BoilingCauldronDeed(Serial serial) : base(serial)
    {
    }

    public override BaseAddonContainer Addon => new BoilingCauldronAddon();
    public override int LabelNumber => 1076267; // Boiling Cauldron

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }
}