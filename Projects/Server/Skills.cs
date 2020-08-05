/***************************************************************************
 *                                 Skills.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Server.Network;

namespace Server
{
  public delegate TimeSpan SkillUseCallback(Mobile user);

  public enum SkillLock : byte
  {
    Up = 0,
    Down = 1,
    Locked = 2
  }

  public enum SkillName
  {
    Alchemy = 0,
    Anatomy = 1,
    AnimalLore = 2,
    ItemID = 3,
    ArmsLore = 4,
    Parry = 5,
    Begging = 6,
    Blacksmith = 7,
    Fletching = 8,
    Peacemaking = 9,
    Camping = 10,
    Carpentry = 11,
    Cartography = 12,
    Cooking = 13,
    DetectHidden = 14,
    Discordance = 15,
    EvalInt = 16,
    Healing = 17,
    Fishing = 18,
    Forensics = 19,
    Herding = 20,
    Hiding = 21,
    Provocation = 22,
    Inscribe = 23,
    Lockpicking = 24,
    Magery = 25,
    MagicResist = 26,
    Tactics = 27,
    Snooping = 28,
    Musicianship = 29,
    Poisoning = 30,
    Archery = 31,
    SpiritSpeak = 32,
    Stealing = 33,
    Tailoring = 34,
    AnimalTaming = 35,
    TasteID = 36,
    Tinkering = 37,
    Tracking = 38,
    Veterinary = 39,
    Swords = 40,
    Macing = 41,
    Fencing = 42,
    Wrestling = 43,
    Lumberjacking = 44,
    Mining = 45,
    Meditation = 46,
    Stealth = 47,
    RemoveTrap = 48,
    Necromancy = 49,
    Focus = 50,
    Chivalry = 51,
    Bushido = 52,
    Ninjitsu = 53,
    Spellweaving = 54,
    Mysticism = 55,
    Imbuing = 56,
    Throwing = 57
  }

  [PropertyObject]
  public class Skill
  {
    private ushort m_Base;
    private ushort m_Cap;

    public Skill(Skills owner, SkillInfo info, IGenericReader reader)
    {
      Owner = owner;
      Info = info;

      int version = reader.ReadByte();

      switch (version)
      {
        case 0:
          {
            m_Base = reader.ReadUShort();
            m_Cap = reader.ReadUShort();
            Lock = (SkillLock)reader.ReadByte();

            break;
          }
        case 0xFF:
          {
            m_Base = 0;
            m_Cap = 1000;
            Lock = SkillLock.Up;

            break;
          }
        default:
          {
            if ((version & 0xC0) == 0x00)
            {
              if ((version & 0x1) != 0)
                m_Base = reader.ReadUShort();

              if ((version & 0x2) != 0)
                m_Cap = reader.ReadUShort();
              else
                m_Cap = 1000;

              if ((version & 0x4) != 0)
                Lock = (SkillLock)reader.ReadByte();
            }

            break;
          }
      }

      if (Lock < SkillLock.Up || Lock > SkillLock.Locked)
      {
        Console.WriteLine("Bad skill lock -> {0}.{1}", owner.Owner, Lock);
        Lock = SkillLock.Up;
      }
    }

    public Skill(Skills owner, SkillInfo info, int baseValue, int cap, SkillLock skillLock)
    {
      Owner = owner;
      Info = info;
      m_Base = (ushort)baseValue;
      m_Cap = (ushort)cap;
      Lock = skillLock;
    }

    public Skills Owner { get; }

    public SkillName SkillName => (SkillName)Info.SkillID;

    public int SkillID => Info.SkillID;

    [CommandProperty(AccessLevel.Counselor)]
    public string Name => Info.Name;

    public SkillInfo Info { get; }

    [CommandProperty(AccessLevel.Counselor)]
    public SkillLock Lock { get; private set; }

    public int BaseFixedPoint
    {
      get => m_Base;
      set
      {
        var sv = (ushort)Math.Clamp(value, 0, 0xFFFF);

        int oldBase = m_Base;

        if (m_Base != sv)
        {
          Owner.Total = Owner.Total - m_Base + sv;

          m_Base = sv;

          Owner.OnSkillChange(this);

          var m = Owner.Owner;

          m?.OnSkillChange(SkillName, (double)oldBase / 10);
        }
      }
    }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public double Base
    {
      get => m_Base / 10.0;
      set => BaseFixedPoint = (int)(value * 10.0);
    }

    public int CapFixedPoint
    {
      get => m_Cap;
      set
      {
        var sv = (ushort)Math.Clamp(value, 0, 0xFFFF);

        if (m_Cap != sv)
        {
          m_Cap = sv;

          Owner.OnSkillChange(this);
        }
      }
    }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public double Cap
    {
      get => m_Cap / 10.0;
      set => CapFixedPoint = (int)(value * 10.0);
    }

    public static bool UseStatMods { get; set; }

    public int Fixed => (int)(Value * 10);

    [CommandProperty(AccessLevel.Counselor)]
    public double Value
    {
      get
      {
        // There has to be this distinction between the racial values and not to account for gaining skills and these skills aren't displayed nor Totaled up.
        var value = NonRacialValue;

        var raceBonus = Owner.Owner.RacialSkillBonus;

        if (raceBonus > value)
          value = raceBonus;

        return value;
      }
    }

    [CommandProperty(AccessLevel.Counselor)]
    public double NonRacialValue
    {
      get
      {
        var baseValue = Base;
        var inv = 100.0 - baseValue;

        if (inv < 0.0) inv = 0.0;

        inv /= 100.0;

        var statsOffset = (UseStatMods ? Owner.Owner.Str : Owner.Owner.RawStr) * Info.StrScale +
                          (UseStatMods ? Owner.Owner.Dex : Owner.Owner.RawDex) * Info.DexScale +
                          (UseStatMods ? Owner.Owner.Int : Owner.Owner.RawInt) * Info.IntScale;
        var statTotal = Info.StatTotal * inv;

        statsOffset *= inv;

        if (statsOffset > statTotal)
          statsOffset = statTotal;

        var value = baseValue + statsOffset;

        Owner.Owner.ValidateSkillMods();

        var mods = Owner.Owner.SkillMods;

        double bonusObey = 0.0, bonusNotObey = 0.0;

        for (var i = 0; i < mods.Count; ++i)
        {
          var mod = mods[i];

          if (mod.Skill == (SkillName)Info.SkillID)
          {
            if (mod.Relative)
            {
              if (mod.ObeyCap)
                bonusObey += mod.Value;
              else
                bonusNotObey += mod.Value;
            }
            else
            {
              bonusObey = 0.0;
              bonusNotObey = 0.0;
              value = mod.Value;
            }
          }
        }

        value += bonusNotObey;

        if (value < Cap)
        {
          value += bonusObey;

          if (value > Cap)
            value = Cap;
        }

        return value;
      }
    }

    public override string ToString() => $"[{Name}: {Base}]";

    public void SetLockNoRelay(SkillLock skillLock)
    {
      if (skillLock < SkillLock.Up || skillLock > SkillLock.Locked)
        return;

      Lock = skillLock;
    }

    public void Serialize(IGenericWriter writer)
    {
      if (m_Base == 0 && m_Cap == 1000 && Lock == SkillLock.Up)
      {
        writer.Write((byte)0xFF); // default
      }
      else
      {
        var flags = 0x0;

        if (m_Base != 0)
          flags |= 0x1;

        if (m_Cap != 1000)
          flags |= 0x2;

        if (Lock != SkillLock.Up)
          flags |= 0x4;

        writer.Write((byte)flags); // version

        if (m_Base != 0)
          writer.Write((short)m_Base);

        if (m_Cap != 1000)
          writer.Write((short)m_Cap);

        if (Lock != SkillLock.Up)
          writer.Write((byte)Lock);
      }
    }

    public void Update()
    {
      Owner.OnSkillChange(this);
    }
  }

  public class SkillInfo
  {
    public SkillInfo(int skillID, string name, double strScale, double dexScale, double intScale, string title,
      SkillUseCallback callback, double strGain, double dexGain, double intGain, double gainFactor)
    {
      Name = name;
      Title = title;
      SkillID = skillID;
      StrScale = strScale / 100.0;
      DexScale = dexScale / 100.0;
      IntScale = intScale / 100.0;
      Callback = callback;
      StrGain = strGain;
      DexGain = dexGain;
      IntGain = intGain;
      GainFactor = gainFactor;

      StatTotal = strScale + dexScale + intScale;
    }

    public SkillUseCallback Callback { get; set; }

    public int SkillID { get; }

    public string Name { get; set; }

    public string Title { get; set; }

    public double StrScale { get; set; }

    public double DexScale { get; set; }

    public double IntScale { get; set; }

    public double StatTotal { get; set; }

    public double StrGain { get; set; }

    public double DexGain { get; set; }

    public double IntGain { get; set; }

    public double GainFactor { get; set; }

    public static SkillInfo[] Table { get; set; } =
    {
      new SkillInfo(0, "Alchemy", 0.0, 5.0, 5.0, "Alchemist", null, 0.0, 0.5, 0.5, 1.0),
      new SkillInfo(1, "Anatomy", 0.0, 0.0, 0.0, "Biologist", null, 0.15, 0.15, 0.7, 1.0),
      new SkillInfo(2, "Animal Lore", 0.0, 0.0, 0.0, "Naturalist", null, 0.0, 0.0, 1.0, 1.0),
      new SkillInfo(3, "Item Identification", 0.0, 0.0, 0.0, "Merchant", null, 0.0, 0.0, 1.0, 1.0),
      new SkillInfo(4, "Arms Lore", 0.0, 0.0, 0.0, "Weapon Master", null, 0.75, 0.15, 0.1, 1.0),
      new SkillInfo(5, "Parrying", 7.5, 2.5, 0.0, "Duelist", null, 0.75, 0.25, 0.0, 1.0),
      new SkillInfo(6, "Begging", 0.0, 0.0, 0.0, "Beggar", null, 0.0, 0.0, 0.0, 1.0),
      new SkillInfo(7, "Blacksmithy", 10.0, 0.0, 0.0, "Blacksmith", null, 1.0, 0.0, 0.0, 1.0),
      new SkillInfo(8, "Bowcraft/Fletching", 6.0, 16.0, 0.0, "Bowyer", null, 0.6, 1.6, 0.0, 1.0),
      new SkillInfo(9, "Peacemaking", 0.0, 0.0, 0.0, "Pacifier", null, 0.0, 0.0, 0.0, 1.0),
      new SkillInfo(10, "Camping", 20.0, 15.0, 15.0, "Explorer", null, 2.0, 1.5, 1.5, 1.0),
      new SkillInfo(11, "Carpentry", 20.0, 5.0, 0.0, "Carpenter", null, 2.0, 0.5, 0.0, 1.0),
      new SkillInfo(12, "Cartography", 0.0, 7.5, 7.5, "Cartographer", null, 0.0, 0.75, 0.75, 1.0),
      new SkillInfo(13, "Cooking", 0.0, 20.0, 30.0, "Chef", null, 0.0, 2.0, 3.0, 1.0),
      new SkillInfo(14, "Detecting Hidden", 0.0, 0.0, 0.0, "Scout", null, 0.0, 0.4, 0.6, 1.0),
      new SkillInfo(15, "Discordance", 0.0, 2.5, 2.5, "Demoralizer", null, 0.0, 0.25, 0.25, 1.0),
      new SkillInfo(16, "Evaluating Intelligence", 0.0, 0.0, 0.0, "Scholar", null, 0.0, 0.0, 1.0, 1.0),
      new SkillInfo(17, "Healing", 6.0, 6.0, 8.0, "Healer", null, 0.6, 0.6, 0.8, 1.0),
      new SkillInfo(18, "Fishing", 0.0, 0.0, 0.0, "Fisherman", null, 0.5, 0.5, 0.0, 1.0),
      new SkillInfo(19, "Forensic Evaluation", 0.0, 0.0, 0.0, "Detective", null, 0.0, 0.2, 0.8, 1.0),
      new SkillInfo(20, "Herding", 16.25, 6.25, 2.5, "Shepherd", null, 1.625, 0.625, 0.25, 1.0),
      new SkillInfo(21, "Hiding", 0.0, 0.0, 0.0, "Shade", null, 0.0, 0.8, 0.2, 1.0),
      new SkillInfo(22, "Provocation", 0.0, 4.5, 0.5, "Rouser", null, 0.0, 0.45, 0.05, 1.0),
      new SkillInfo(23, "Inscription", 0.0, 2.0, 8.0, "Scribe", null, 0.0, 0.2, 0.8, 1.0),
      new SkillInfo(24, "Lockpicking", 0.0, 25.0, 0.0, "Infiltrator", null, 0.0, 2.0, 0.0, 1.0),
      new SkillInfo(25, "Magery", 0.0, 0.0, 15.0, "Mage", null, 0.0, 0.0, 1.5, 1.0),
      new SkillInfo(26, "Resisting Spells", 0.0, 0.0, 0.0, "Warder", null, 0.25, 0.25, 0.5, 1.0),
      new SkillInfo(27, "Tactics", 0.0, 0.0, 0.0, "Tactician", null, 0.0, 0.0, 0.0, 1.0),
      new SkillInfo(28, "Snooping", 0.0, 25.0, 0.0, "Spy", null, 0.0, 2.5, 0.0, 1.0),
      new SkillInfo(29, "Musicianship", 0.0, 0.0, 0.0, "Bard", null, 0.0, 0.8, 0.2, 1.0),
      new SkillInfo(30, "Poisoning", 0.0, 4.0, 16.0, "Assassin", null, 0.0, 0.4, 1.6, 1.0),
      new SkillInfo(31, "Archery", 2.5, 7.5, 0.0, "Archer", null, 0.25, 0.75, 0.0, 1.0),
      new SkillInfo(32, "Spirit Speak", 0.0, 0.0, 0.0, "Medium", null, 0.0, 0.0, 1.0, 1.0),
      new SkillInfo(33, "Stealing", 0.0, 10.0, 0.0, "Pickpocket", null, 0.0, 1.0, 0.0, 1.0),
      new SkillInfo(34, "Tailoring", 3.75, 16.25, 5.0, "Tailor", null, 0.38, 1.63, 0.5, 1.0),
      new SkillInfo(35, "Animal Taming", 14.0, 2.0, 4.0, "Tamer", null, 1.4, 0.2, 0.4, 1.0),
      new SkillInfo(36, "Taste Identification", 0.0, 0.0, 0.0, "Praegustator", null, 0.2, 0.0, 0.8, 1.0),
      new SkillInfo(37, "Tinkering", 5.0, 2.0, 3.0, "Tinker", null, 0.5, 0.2, 0.3, 1.0),
      new SkillInfo(38, "Tracking", 0.0, 12.5, 12.5, "Ranger", null, 0.0, 1.25, 1.25, 1.0),
      new SkillInfo(39, "Veterinary", 8.0, 4.0, 8.0, "Veterinarian", null, 0.8, 0.4, 0.8, 1.0),
      new SkillInfo(40, "Swordsmanship", 7.5, 2.5, 0.0, "Swordsman", null, 0.75, 0.25, 0.0, 1.0),
      new SkillInfo(41, "Mace Fighting", 9.0, 1.0, 0.0, "Armsman", null, 0.9, 0.1, 0.0, 1.0),
      new SkillInfo(42, "Fencing", 4.5, 5.5, 0.0, "Fencer", null, 0.45, 0.55, 0.0, 1.0),
      new SkillInfo(43, "Wrestling", 9.0, 1.0, 0.0, "Wrestler", null, 0.9, 0.1, 0.0, 1.0),
      new SkillInfo(44, "Lumberjacking", 20.0, 0.0, 0.0, "Lumberjack", null, 2.0, 0.0, 0.0, 1.0),
      new SkillInfo(45, "Mining", 20.0, 0.0, 0.0, "Miner", null, 2.0, 0.0, 0.0, 1.0),
      new SkillInfo(46, "Meditation", 0.0, 0.0, 0.0, "Stoic", null, 0.0, 0.0, 0.0, 1.0),
      new SkillInfo(47, "Stealth", 0.0, 0.0, 0.0, "Rogue", null, 0.0, 0.0, 0.0, 1.0),
      new SkillInfo(48, "Remove Trap", 0.0, 0.0, 0.0, "Trap Specialist", null, 0.0, 0.0, 0.0, 1.0),
      new SkillInfo(49, "Necromancy", 0.0, 0.0, 0.0, "Necromancer", null, 0.0, 0.0, 0.0, 1.0),
      new SkillInfo(50, "Focus", 0.0, 0.0, 0.0, "Driven", null, 0.0, 0.0, 0.0, 1.0),
      new SkillInfo(51, "Chivalry", 0.0, 0.0, 0.0, "Paladin", null, 0.0, 0.0, 0.0, 1.0),
      new SkillInfo(52, "Bushido", 0.0, 0.0, 0.0, "Samurai", null, 0.0, 0.0, 0.0, 1.0),
      new SkillInfo(53, "Ninjitsu", 0.0, 0.0, 0.0, "Ninja", null, 0.0, 0.0, 0.0, 1.0),
      new SkillInfo(54, "Spellweaving", 0.0, 0.0, 0.0, "Arcanist", null, 0.0, 0.0, 0.0, 1.0),
      new SkillInfo(55, "Mysticism", 0.0, 0.0, 0.0, "Mystic", null, 0.0, 0.0, 0.0, 1.0),
      new SkillInfo(56, "Imbuing", 0.0, 0.0, 0.0, "Artificer", null, 0.0, 0.0, 0.0, 1.0),
      new SkillInfo(57, "Throwing", 0.0, 0.0, 0.0, "Bladeweaver", null, 0.0, 0.0, 0.0, 1.0)
    };
  }

  [PropertyObject]
  public class Skills : IEnumerable<Skill>
  {
    private Skill m_Highest;
    private readonly Skill[] m_Skills;

    public Skills(Mobile owner)
    {
      Owner = owner;
      Cap = 7000;

      var info = SkillInfo.Table;

      m_Skills = new Skill[info.Length];

      // for ( int i = 0; i < info.Length; ++i )
      // m_Skills[i] = new Skill( this, info[i], 0, 1000, SkillLock.Up );
    }

    public Skills(Mobile owner, IGenericReader reader)
    {
      Owner = owner;

      var version = reader.ReadInt();

      switch (version)
      {
        case 3:
        case 2:
          {
            Cap = reader.ReadInt();

            goto case 1;
          }
        case 1:
          {
            if (version < 2)
              Cap = 7000;

            if (version < 3)
              /*m_Total =*/
              reader.ReadInt();

            var info = SkillInfo.Table;

            m_Skills = new Skill[info.Length];

            var count = reader.ReadInt();

            for (var i = 0; i < count; ++i)
              if (i < info.Length)
              {
                var sk = new Skill(this, info[i], reader);

                if (sk.BaseFixedPoint != 0 || sk.CapFixedPoint != 1000 || sk.Lock != SkillLock.Up)
                {
                  m_Skills[i] = sk;
                  Total += sk.BaseFixedPoint;
                }
              }
              else
              {
                // Will be discarded
                _ = new Skill(this, null, reader);
              }

            // for ( int i = count; i < info.Length; ++i )
            // m_Skills[i] = new Skill( this, info[i], 0, 1000, SkillLock.Up );

            break;
          }
        case 0:
          {
            reader.ReadInt();

            goto case 1;
          }
      }
    }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public int Cap { get; set; }

    public int Total { get; set; }

    public Mobile Owner { get; }

    public int Length => m_Skills.Length;

    public Skill this[SkillName name] => this[(int)name];

    public Skill this[int skillID]
    {
      get
      {
        if (skillID < 0 || skillID >= m_Skills.Length)
          return null;

        var sk = m_Skills[skillID];

        if (sk == null)
          m_Skills[skillID] = sk = new Skill(this, SkillInfo.Table[skillID], 0, 1000, SkillLock.Up);

        return sk;
      }
    }

    public Skill Highest
    {
      get
      {
        if (m_Highest == null)
        {
          Skill highest = null;
          var value = int.MinValue;

          for (var i = 0; i < m_Skills.Length; ++i)
          {
            var sk = m_Skills[i];

            if (sk != null && sk.BaseFixedPoint > value)
            {
              value = sk.BaseFixedPoint;
              highest = sk;
            }
          }

          if (highest == null && m_Skills.Length > 0)
            highest = this[0];

          m_Highest = highest;
        }

        return m_Highest;
      }
    }

    public IEnumerator<Skill> GetEnumerator()
    {
      return m_Skills.Where(s => s != null).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return m_Skills.Where(s => s != null).GetEnumerator();
    }

    public override string ToString() => "...";

    public static bool UseSkill(Mobile from, SkillName name) => UseSkill(from, (int)name);

    public static bool UseSkill(Mobile from, int skillID)
    {
      if (!from.CheckAlive())
        return false;
      if (!from.Region.OnSkillUse(from, skillID))
        return false;
      if (!from.AllowSkillUse((SkillName)skillID))
        return false;

      if (skillID >= 0 && skillID < SkillInfo.Table.Length)
      {
        var info = SkillInfo.Table[skillID];

        if (info.Callback != null)
        {
          if (Core.TickCount - from.NextSkillTime >= 0 && from.Spell == null)
          {
            from.DisruptiveAction();

            from.NextSkillTime = Core.TickCount + (int)info.Callback(from).TotalMilliseconds;

            return true;
          }

          from.SendSkillMessage();
        }
        else
        {
          from.SendLocalizedMessage(500014); // That skill cannot be used directly.
        }
      }

      return false;
    }

    public void Serialize(IGenericWriter writer)
    {
      Total = 0;

      writer.Write(3); // version

      writer.Write(Cap);
      writer.Write(m_Skills.Length);

      for (var i = 0; i < m_Skills.Length; ++i)
      {
        var sk = m_Skills[i];

        if (sk == null)
        {
          writer.Write((byte)0xFF);
        }
        else
        {
          sk.Serialize(writer);
          Total += sk.BaseFixedPoint;
        }
      }
    }

    public void OnSkillChange(Skill skill)
    {
      if (skill == m_Highest) // could be downgrading the skill, force a recalc
        m_Highest = null;
      else if (m_Highest != null && skill.BaseFixedPoint > m_Highest.BaseFixedPoint)
        m_Highest = skill;

      Owner.OnSkillInvalidated(skill);
      Owner.NetState?.Send(new SkillChange(skill));
    }

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Alchemy => this[SkillName.Alchemy];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Anatomy => this[SkillName.Anatomy];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill AnimalLore => this[SkillName.AnimalLore];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill ItemID => this[SkillName.ItemID];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill ArmsLore => this[SkillName.ArmsLore];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Parry => this[SkillName.Parry];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Begging => this[SkillName.Begging];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Blacksmith => this[SkillName.Blacksmith];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Fletching => this[SkillName.Fletching];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Peacemaking => this[SkillName.Peacemaking];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Camping => this[SkillName.Camping];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Carpentry => this[SkillName.Carpentry];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Cartography => this[SkillName.Cartography];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Cooking => this[SkillName.Cooking];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill DetectHidden => this[SkillName.DetectHidden];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Discordance => this[SkillName.Discordance];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill EvalInt => this[SkillName.EvalInt];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Healing => this[SkillName.Healing];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Fishing => this[SkillName.Fishing];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Forensics => this[SkillName.Forensics];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Herding => this[SkillName.Herding];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Hiding => this[SkillName.Hiding];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Provocation => this[SkillName.Provocation];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Inscribe => this[SkillName.Inscribe];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Lockpicking => this[SkillName.Lockpicking];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Magery => this[SkillName.Magery];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill MagicResist => this[SkillName.MagicResist];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Tactics => this[SkillName.Tactics];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Snooping => this[SkillName.Snooping];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Musicianship => this[SkillName.Musicianship];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Poisoning => this[SkillName.Poisoning];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Archery => this[SkillName.Archery];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill SpiritSpeak => this[SkillName.SpiritSpeak];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Stealing => this[SkillName.Stealing];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Tailoring => this[SkillName.Tailoring];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill AnimalTaming => this[SkillName.AnimalTaming];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill TasteID => this[SkillName.TasteID];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Tinkering => this[SkillName.Tinkering];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Tracking => this[SkillName.Tracking];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Veterinary => this[SkillName.Veterinary];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Swords => this[SkillName.Swords];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Macing => this[SkillName.Macing];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Fencing => this[SkillName.Fencing];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Wrestling => this[SkillName.Wrestling];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Lumberjacking => this[SkillName.Lumberjacking];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Mining => this[SkillName.Mining];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Meditation => this[SkillName.Meditation];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Stealth => this[SkillName.Stealth];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill RemoveTrap => this[SkillName.RemoveTrap];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Necromancy => this[SkillName.Necromancy];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Focus => this[SkillName.Focus];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Chivalry => this[SkillName.Chivalry];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Bushido => this[SkillName.Bushido];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Ninjitsu => this[SkillName.Ninjitsu];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Spellweaving => this[SkillName.Spellweaving];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Mysticism => this[SkillName.Mysticism];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Imbuing => this[SkillName.Imbuing];

    [CommandProperty(AccessLevel.Counselor)]
    public Skill Throwing => this[SkillName.Throwing];
  }
}
