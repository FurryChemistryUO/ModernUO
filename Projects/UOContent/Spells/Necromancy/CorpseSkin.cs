using System;
using System.Collections.Generic;
using Server.Targeting;

namespace Server.Spells.Necromancy
{
    public class CorpseSkinSpell : NecromancerSpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo m_Info = new(
            "Corpse Skin",
            "In Agle Corp Ylem",
            203,
            9051,
            Reagent.BatWing,
            Reagent.GraveDust
        );

        private static readonly Dictionary<Mobile, ExpireTimer> m_Table = new();

        public CorpseSkinSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.5);

        public override double RequiredSkill => 20.0;
        public override int RequiredMana => 11;

        public void Target(Mobile m)
        {
            if (m == null)
            {
                return;
            }

            if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                /* Transmogrifies the flesh of the target creature or player to resemble rotted corpse flesh,
                 * making them more vulnerable to Fire and Poison damage,
                 * but increasing their resistance to Physical and Cold damage.
                 *
                 * The effect lasts for ((Spirit Speak skill level - target's Resist Magic skill level) / 25 ) + 40 seconds.
                 *
                 * NOTE: Algorithm above is fixed point, should be:
                 * ((ss-mr)/2.5) + 40
                 *
                 * NOTE: Resistance is not checked if targeting yourself
                 */

                if (m_Table.TryGetValue(m, out var timer))
                {
                    timer.DoExpire();
                }
                else
                {
                    m.SendLocalizedMessage(1061689); // Your skin turns dry and corpselike.
                }

                m.Spell?.OnCasterHurt();

                m.FixedParticles(0x373A, 1, 15, 9913, 67, 7, EffectLayer.Head);
                m.PlaySound(0x1BB);

                var ss = GetDamageSkill(Caster);
                var mr = Caster == m ? 0.0 : GetResistSkill(m);
                m.CheckSkill(SkillName.MagicResist, 0.0, 120.0); // Skill check for gain

                var duration = TimeSpan.FromSeconds((ss - mr) / 2.5 + 40.0);

                ResistanceMod[] mods =
                {
                    new(ResistanceType.Fire, -15),
                    new(ResistanceType.Poison, -15),
                    new(ResistanceType.Cold, +10),
                    new(ResistanceType.Physical, +10)
                };

                timer = new ExpireTimer(m, mods, duration);
                timer.Start();

                BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.CorpseSkin, 1075663, duration, m));

                m_Table[m] = timer;

                for (var i = 0; i < mods.Length; ++i)
                {
                    m.AddResistanceMod(mods[i]);
                }

                HarmfulSpell(m);
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
        }

        public static bool RemoveCurse(Mobile m)
        {
            if (!m_Table.TryGetValue(m, out var t))
            {
                return false;
            }

            m.SendLocalizedMessage(1061688); // Your skin returns to normal.
            t?.DoExpire();
            return true;
        }

        private class ExpireTimer : Timer
        {
            private readonly Mobile m_Mobile;
            private readonly ResistanceMod[] m_Mods;

            public ExpireTimer(Mobile m, ResistanceMod[] mods, TimeSpan delay) : base(delay)
            {
                m_Mobile = m;
                m_Mods = mods;
            }

            public void DoExpire()
            {
                for (var i = 0; i < m_Mods.Length; ++i)
                {
                    m_Mobile.RemoveResistanceMod(m_Mods[i]);
                }

                Stop();
                BuffInfo.RemoveBuff(m_Mobile, BuffIcon.CorpseSkin);
                m_Table.Remove(m_Mobile);
            }

            protected override void OnTick()
            {
                m_Mobile.SendLocalizedMessage(1061688); // Your skin returns to normal.
                DoExpire();
            }
        }
    }
}
