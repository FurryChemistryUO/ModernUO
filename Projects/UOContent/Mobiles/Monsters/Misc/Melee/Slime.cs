namespace Server.Mobiles
{
  public class Slime : BaseCreature
  {
    [Constructible]
    public Slime() : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
    {
      Body = 51;
      BaseSoundID = 456;

      Hue = Utility.RandomSlimeHue();

      SetStr(22, 34);
      SetDex(16, 21);
      SetInt(16, 20);

      SetHits(15, 19);

      SetDamage(1, 5);

      SetDamageType(ResistanceType.Physical, 100);

      SetResistance(ResistanceType.Physical, 5, 10);
      SetResistance(ResistanceType.Poison, 10, 20);

      SetSkill(SkillName.Poisoning, 30.1, 50.0);
      SetSkill(SkillName.MagicResist, 15.1, 20.0);
      SetSkill(SkillName.Tactics, 19.3, 34.0);
      SetSkill(SkillName.Wrestling, 19.3, 34.0);

      Fame = 300;
      Karma = -300;

      VirtualArmor = 8;

      Tamable = true;
      ControlSlots = 1;
      MinTameSkill = 23.1;
    }

    public Slime(Serial serial) : base(serial)
    {
    }

    public override string CorpseName => "a slimey corpse";
    public override string DefaultName => "a slime";

    public override Poison PoisonImmune => Poison.Lesser;
    public override Poison HitPoison => Poison.Lesser;

    public override FoodType FavoriteFood => FoodType.Meat | FoodType.Fish | FoodType.FruitsAndVegies |
                                             FoodType.GrainsAndHay | FoodType.Eggs;

    public override void GenerateLoot()
    {
      AddLoot(LootPack.Poor);
      AddLoot(LootPack.Gems);
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);
      writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);
      int version = reader.ReadInt();
    }
  }
}