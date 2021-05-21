using Server.Targeting;

namespace Server.Engines.Plants
{
    public class PlantPourTarget : Target
    {
        private readonly PlantItem m_Plant;

        public PlantPourTarget(PlantItem plant) : base(3, true, TargetFlags.None) => m_Plant = plant;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!m_Plant.Deleted && from.InRange(m_Plant.GetWorldLocation(), 3) && targeted is Item item)
            {
                m_Plant.Pour(from, item);
            }
        }

        protected override void OnTargetFinish(Mobile from)
        {
            if (!m_Plant.Deleted && m_Plant.PlantStatus < PlantStatus.DecorativePlant &&
                from.InRange(m_Plant.GetWorldLocation(), 3) && m_Plant.IsUsableBy(from))
            {
                if (from.HasGump<MainPlantGump>())
                {
                    from.CloseGump<MainPlantGump>();
                }

                from.SendGump(new MainPlantGump(m_Plant));
            }
        }
    }
}
