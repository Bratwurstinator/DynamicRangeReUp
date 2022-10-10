using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using Verse.AI;
using CombatExtended;

namespace PistolRangeTweaken
{
    public class RangerComp : CompRangedGizmoGiver
    {
        public VerbPropertiesCE verpPropsCE
        {
            get
            {
               return (VerbPropertiesCE)this.parent.TryGetComp<CompEquippable>().PrimaryVerb.verbProps;
            }
        }
        
        public override void Notify_Equipped(Pawn pawn)
        {
            VerbPropertiesCE verbPropsCE2 = (VerbPropertiesCE)verpPropsCE.MemberwiseClone();
            //Log.Message(verbPropsCE2.range.ToString() + "tt33");
            verbPropsCE2.range = verpPropsCE.range * Math.Max(1f, ((pawn.skills.skills.Find(tt33 => tt33.def == SkillDefOf.Shooting).Level / 10f) * pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight)));
            //Log.Message(verbPropsCE2.range.ToString());
            var verbshoots = (Verb_ShootCE)this.parent.TryGetComp<CompEquippable>().PrimaryVerb;
            verbshoots.verbProps = verbPropsCE2;
            //Log.Message(verbshoots.VerbPropsCE.range.ToString());
            base.Notify_Equipped(pawn);
        }
      
    }
}
