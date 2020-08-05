using Server.Network;

namespace Server.Items
{
  [Flippable(0x1766, 0x1768)]
  public class Cloth : Item, IScissorable, IDyable, ICommodity
  {
    [Constructible]
    public Cloth(int amount = 1) : base(0x1766)
    {
      Stackable = true;
      Amount = amount;
    }

    public Cloth(Serial serial) : base(serial)
    {
    }

    public override double DefaultWeight => 0.1;
    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;

    public bool Dye(Mobile from, DyeTub sender)
    {
      if (Deleted)
        return false;

      Hue = sender.DyedHue;

      return true;
    }

    public bool Scissor(Mobile from, Scissors scissors)
    {
      if (Deleted || !from.CanSee(this)) return false;

      ScissorHelper(from, new Bandage(), 1);

      return true;
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }

    public override void OnSingleClick(Mobile from)
    {
      int number = Amount == 1 ? 1049124 : 1049123;

      from.Send(new MessageLocalized(Serial, ItemID, MessageType.Regular, 0x3B2, 3, number, "", Amount.ToString()));
    }
  }
}
