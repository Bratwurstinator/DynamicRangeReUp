using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using Verse.AI;
using CombatExtended;
using System.Reflection;
using HarmonyLib;
using HarmonyMod;
using UnityEngine;

namespace PistolRangeTweaken
{
    [DefOf]
    public class DRDefOf : DefOf
    {
        public static StatDef DynamicRange;
    }

    public static class AnUtil
    {
        public static void AddComp(this ThingDef federal, CompProperties item)
        {
            if (federal.comps == null)
            {
                federal.comps = new List<CompProperties>();
            }

            federal.comps.Add(item);
        }

        public static void AddStat(this ThingDef federal, StatModifier item)
        {
            if (federal.statBases == null)
            {
                federal.statBases = new List<StatModifier>();
            }

            federal.statBases.Add(item);
        }

        public static StatDef DynamicRange
        {
            get
            {
                return DRDefOf.DynamicRange;
            }
        }

        public static void ChangeCurveValue(this CurvePoint point, Vector2 val)
        {
            var field = point.GetType().GetField("loc", (BindingFlags.NonPublic | BindingFlags.Instance));

            field.SetValue(point, val);
        }
    }

    public class RangerComp : CompRangedGizmoGiver
    {
        public bool changesApplied = false;

        public IntVec3 savedpos;

        public void ResetRange()
        {
            /*
            VerbPropertiesCE cereset = (VerbPropertiesCE)verpPropsCE.MemberwiseClone();

            cereset.range = this.parent.def.Verbs.Find(e => e is VerbPropertiesCE).MemberwiseClone().range;

            var verbshoots = (Verb_ShootCE)this.parent.TryGetComp<CompEquippable>().PrimaryVerb;
            verbshoots.verbProps = cereset;

            changesApplied = false;
            */
        }

        //public float range => ;

        public bool hasGT => ModLister.HasActiveModWithName("Grenade tweaks");

        public bool isNade => (this.parent.Label.ToLower().Contains("grenade") && !(this.parent.Label.ToLower().Contains("launcher")));

        public bool IsNade;

        public bool HasGT;

        public CompEquippable compEq => this.parent.TryGetComp<CompEquippable>();

        public Verb primaryVerb => compEq.PrimaryVerb;

        public override void Initialize(CompProperties props)
        {
            IsNade = isNade;
            HasGT = hasGT;
            base.Initialize(props);
            MakeNewProps();
        }

        public void MakeNewProps()
        {
            /*Log.Message("logging verbprops");

            Log.Message(primaryVerb.verbProps.ToString().Colorize(Color.blue));

            Log.Message("chaning verb props and logging if they are same");

            var oldProps = primaryVerb.verbProps;*/

            primaryVerb.verbProps = primaryVerb.verbProps.MemberwiseClone();

            //Log.Message((oldProps == primaryVerb.verbProps).ToString().Colorize(Color.blue));

            base.PostPostMake();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                MakeNewProps();
            }
        }

        public void RangeTick(Pawn P)
        {
            if (HasGT)
            {
                if (IsNade)
                { 
                    return;
                }
            }
            primaryVerb.verbProps.range = this.parent.GetStatValue(AnUtil.DynamicRange);
        }
    }

    [StaticConstructorOnStartup]
    public class FuckXML
    {
        static FuckXML()
        {
            foreach (ThingDef gun in DefDatabase<ThingDef>.AllDefs.Where(x => x.Verbs.Any(y => y is VerbPropertiesCE)))
            {
                gun.comps.Add(new CompProperties { compClass = typeof(RangerComp) });
                gun.AddStat(new StatModifier { stat =  AnUtil.DynamicRange, value = gun.Verbs.Find(x => x is VerbPropertiesCE).range});
            }
        }
    }

    public class harmonypatchallall : Mod
    {
        public harmonypatchallall(ModContentPack content) : base(content)
        {


            var harmony = new Harmony("Caulaflower.SkillRange");
            try
            {
                //log.message("succsefully harmony patched Caulaflower.SkillRange".Colorize(UnityEngine.Color.cyan));
                harmony.PatchAll();

            }
            catch (Exception e)
            {

                Log.Error(e.ToString());
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "Tick")]
    static class PostFixTickStuffThing
    {
        public static int ticks = 0;
        public static void Postfix(Pawn __instance)
        {
            if (__instance == null || (!__instance.def?.race?.Humanlike ?? true) || !(__instance.ParentHolder is Map))
            {
                return;
            }

            if (ticks >= SettingsModClass.settings.rangeRefreshRate)
            {
                if ((__instance.equipment?.Primary?.TryGetComp<RangerComp>() ?? null) != null)
                {
                    __instance.equipment.Primary.TryGetComp<RangerComp>().RangeTick(__instance);
                }

                ticks = 0;
            }

            ++ticks;
            
        }
    }

    public class PawnSkillCurveStat : StatPart
    {
        public SimpleCurve kurwa;

        public bool Equippe(StatRequest requesto)
        {
            return requesto.Thing?.TryGetComp<CompAmmoUser>()?.IsEquippedGun ?? false;
        }

        public Pawn EquippeMan(StatRequest requesto)
        {
            return requesto.Thing?.TryGetComp<CompAmmoUser>()?.Wielder ?? null;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (Equippe(req))
            {
                val *= kurwa.Evaluate(EquippeMan(req).skills.GetSkill(SkillDefOf.Shooting).Level);

                val *= Math.Min(EquippeMan(req).health.capacities.GetLevel(PawnCapacityDefOf.Manipulation), 1.1f);

                val *= Math.Min(EquippeMan(req).health.capacities.GetLevel(PawnCapacityDefOf.Sight), 1.2f);

                val = (int)val;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (Equippe(req))
            {
                return "Pawn shooting skill effect: x" + kurwa.Evaluate(EquippeMan(req).skills.GetSkill(SkillDefOf.Shooting).Level)
                    + "\n"
                    + "Pawn manipulation effect: " + Math.Min(EquippeMan(req).health.capacities.GetLevel(PawnCapacityDefOf.Manipulation), 1.1f)
                    + "\n"
                    + "Pawn sight effect: " + Math.Min(EquippeMan(req).health.capacities.GetLevel(PawnCapacityDefOf.Sight), 1.2f);
                    ;
            }
            return "";
        }
    }

    public class BipodStatPart : StatPart
    {
        public bool HasSetUpBipod(StatRequest req)
        {
            return (req.Thing?.TryGetComp<BipodComp>()?.IsSetUpRn ?? false) && CombatExtended.Controller.settings.BipodMechanics;
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (HasSetUpBipod(req))
            {
                return "Bipod IS set up +" + req.Thing.TryGetComp<BipodComp>().Props.additionalrange;
            }
            return null;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (HasSetUpBipod(req))
            {
                val += req.Thing.TryGetComp<BipodComp>().Props.additionalrange;
            }
        }
    }

    public class SettingsModClass : Mod
    {
        public SettingsModClass(ModContentPack content) : base(content)
        {
            settings = GetSettings<settinges>();
        }

        public override string SettingsCategory()
        {
            return "Dynamic range";
        }

        public static settinges settings;

        public static float[] sliders = new float[]{ 1f, 1f, 1f, 1f, 1f, 1f };

        public static SimpleCurve cachedSetCurve;

        public bool useCached;

        public static List<CurvePoint> curveList;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);

            var List1 = new Listing_Standard();

            List1.Begin(inRect);

            var newKurwa = new SimpleCurve();

            if (!curveList.NullOrEmpty())
            {
                sliders = new float[] { curveList[0].y, curveList[1].y, curveList[2].y, curveList[3].y, curveList[4].y, curveList[5].y };
                curveList = null;
            }

            foreach (var idk in DRDefOf.DynamicRange.GetStatPart<PawnSkillCurveStat>().kurwa)
            {
                List1.Label("From " + idk.x + " range mult is: " + idk.y);
                sliders[DRDefOf.DynamicRange.GetStatPart<PawnSkillCurveStat>().kurwa.FirstIndexOf(x => x.x == idk.x && x.y == idk.y)] = (float)Math.Round(List1.Slider(sliders[DRDefOf.DynamicRange.GetStatPart<PawnSkillCurveStat>().kurwa.FirstIndexOf(x => x.x == idk.x && x.y == idk.y)], 0f, 5f), 2);

                newKurwa.Add(new CurvePoint(new Vector2(idk.x, sliders[DRDefOf.DynamicRange.GetStatPart<PawnSkillCurveStat>().kurwa.FirstIndexOf(x => x.x == idk.x && x.y == idk.y)])));
            }

            if (List1.ButtonText("Reset values"))
            {
                newKurwa = new SimpleCurve
                {
                    new CurvePoint(new Vector2(0f, 0.7f)),
                    new CurvePoint(new Vector2(3f, 0.85f)),
                    new CurvePoint(new Vector2(5f, 1.05f)),
                    new CurvePoint(new Vector2(8f, 1.1f)),
                    new CurvePoint(new Vector2(15f, 1.15f)),
                    new CurvePoint(new Vector2(20f, 1.2f)),
                };

                sliders = new float[] { 0.7f, 0.85f, 1.05f, 1.1f, 1.15f, 1.2f };

                cachedSetCurve = newKurwa;
            }

            DRDefOf.DynamicRange.GetStatPart<PawnSkillCurveStat>().kurwa = newKurwa;

            cachedSetCurve = newKurwa;

            List<CurvePoint> savedcurvelist = newKurwa.ToList().ListFullCopy();

            settings.savedCurve = savedcurvelist;

            List1.Label("Ticks (60 ticks - 1 second) between range refreshes " + settings.rangeRefreshRate.ToString());

            settings.rangeRefreshRate = (int)List1.Slider(settings.rangeRefreshRate, 1f, 60f);

            List1.End();
        }
    }

    public class settinges : ModSettings
    {
        public List<CurvePoint> savedCurve = null;

        public int rangeRefreshRate = 5;

        public bool a = true;

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref savedCurve, "savedcurve", LookMode.Value);
            Scribe_Values.Look<bool>(ref a, "Aaaawtfidk");
            Scribe_Values.Look<int>(ref rangeRefreshRate, "rangerefresh");
        }
    }

    [StaticConstructorOnStartup]
    public class SettingVerter
    {
        static SettingVerter()
        {
            if (!SettingsModClass.settings.savedCurve.NullOrEmpty())
            {
                SimpleCurve curve = new SimpleCurve();

                foreach(var c in SettingsModClass.settings.savedCurve)
                {
                    curve.Add(c);
                }

                SettingsModClass.cachedSetCurve = curve;

                SettingsModClass.curveList = SettingsModClass.settings.savedCurve;

                DRDefOf.DynamicRange.GetStatPart<PawnSkillCurveStat>().kurwa = curve;
            }
        }
    }

    public class AmmoRangeExt : DefModExtension
    {
        public float mult = 1;

        public float offset = 0;
    }

    public class AmmoStatPartRange : StatPart
    {
        public AmmoRangeExt ext(StatRequest req)
        {
            return req.Thing?.TryGetComp<CompAmmoUser>().CurrentAmmo?.GetModExtension<AmmoRangeExt>();
        }

        public override string ExplanationPart(StatRequest req)
        {
            var ext = this.ext(req);
            if (ext != null)
            {
                if (ext.mult != 1)
                    return "Loaded ammo: x" + ext.mult.ToString() + " cells";
                if (ext.offset != 0)
                    return "Loaded ammo: +" + ext.mult.ToString() + " cells";
            }
            return null;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            var ext = this.ext(req);
            if (ext != null)
            {
                val += ext.offset;

                val *= ext.mult;
            }
        }
    }
}

