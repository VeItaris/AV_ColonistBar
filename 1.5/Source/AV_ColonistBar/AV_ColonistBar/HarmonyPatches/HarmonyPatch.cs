using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace AV_ColonistBar
{
    public class AV_ColonistBar : Mod
    {
        private static AV_ColonistBar _instance;

        public static AV_ColonistBar Instance => _instance;

        public AV_ColonistBar(ModContentPack content)
            : base(content)
        {
            Harmony harmony = new Harmony("AV_ColonistBar");
            harmony.PatchAll();
            _instance = this;
        }
    }

    [DefOf]
    public static class ThingCategoryDefOf
    {
        public static ThingCategoryDef ApparelUtility;

        static ThingCategoryDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ThingCategoryDefOf));
        }
    }

    [StaticConstructorOnStartup]
    public static class ColonistBar_ColonistBarOnGUI_Patch
    {


        [HarmonyPatch(typeof(ColonistBar))]
        [HarmonyPatch("ColonistBarOnGUI")]
        public static class ColonistBar_ColonistBarOnGUI
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiller(IEnumerable<CodeInstruction> instructions)
            {
                bool found1 = false;
                bool found2 = false;
                bool skip = false;
                foreach( CodeInstruction instr in instructions )
                {
                    // Replace call to drawer.DrawColonist() with a call to DrawColonist_Hook().
                    // Remove all instructions after it until (and including) the Widgets.ThingIcon() call,
                    // and add a call to After_Hook().
                    // Log.Message("T:" + instr.opcode + "::" + (instr.operand != null ? instr.operand.ToString() : instr.operand));
                    if( instr.opcode == OpCodes.Callvirt
                        && instr.operand.ToString() == "Void DrawColonist(UnityEngine.Rect, Verse.Pawn, Verse.Map, Boolean, Boolean)")
                    {
                        yield return new CodeInstruction(OpCodes.Call, typeof(ColonistBar_ColonistBarOnGUI).GetMethod(nameof(DrawColonist_Hook)));
                        skip = true;
                        found1 = true;
                    }
                    else if( instr.opcode == OpCodes.Call
                        && instr.operand.ToString() == "Void ThingIcon(UnityEngine.Rect, Verse.Thing, Single, System.Nullable`1[Verse.Rot4], Boolean)")
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0); // add 'this' as a parameter
                        yield return new CodeInstruction(OpCodes.Call, typeof(ColonistBar_ColonistBarOnGUI).GetMethod(nameof(After_Hook)));
                        skip = false;
                        found2 = true;
                    } else if( !skip )
                        yield return instr;
                }
                if(!found1 || !found2)
                    Log.Warning("Failed to patch ColonistBar.ColonistBarOnGUI()");
            }

            private static Rect currentRect;
            private static Pawn currentPawn = null;

            // This wrapper around DrawColonist() function is used to get the current rect and pawn for the iteration.
            // The first argument is the hidden 'ColonistBarColonistDrawer this' argument.
            public static void DrawColonist_Hook(ColonistBarColonistDrawer drawer, Rect rect, Pawn colonist, Map pawnMap, bool highlight, bool reordering)
            {
                currentRect = rect;
                currentPawn = colonist;
                drawer.DrawColonist(rect, colonist, pawnMap, highlight, reordering);
            }

            // This function is called at the end of the removed code. The actual mod functionality, draw the weapon/item.
            public static void After_Hook(ColonistBar colonistBar)
            {
                Pawn pawn = currentPawn;
                Rect rect = currentRect;
                currentPawn = null;

                ThingWithComps thingWithComps = pawn.equipment?.Primary;
                ThingWithComps thingWithComps_2 = GetUtilityApparel(pawn);

                float offset = 0f;

                if (thingWithComps != null && thingWithComps_2 != null)
                {
                    offset = rect.width / 2f * (colonistBar.Scale / 2f) + ( 2f * colonistBar.Scale);
                }

                if ((Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.Always || (Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.WhileDrafted && pawn.Drafted)) && thingWithComps != null && thingWithComps.def.IsWeapon)
                {
                    Widgets.ThingIcon(new Rect(rect.x - offset, rect.y + rect.height * ColonistBar.WeaponIconOffsetScaleFactor, rect.width, rect.height).ScaledBy(ColonistBar.WeaponIconScaleFactor), thingWithComps, 1f, null, stackOfOne: true);
                }
                if ((Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.Always || (Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.WhileDrafted && pawn.Drafted)) && thingWithComps_2 != null)
                {
                    Widgets.ThingIcon(new Rect(rect.x + offset, rect.y + rect.height * ColonistBar.WeaponIconOffsetScaleFactor, rect.width, rect.height).ScaledBy(ColonistBar.WeaponIconScaleFactor), thingWithComps_2, 1f, null, stackOfOne: true);
                }
            }

            private static ThingWithComps GetUtilityApparel(Pawn pawn)
            {
                ThingCategoryDef ApparelUtility = ThingCategoryDefOf.ApparelUtility;

                ApparelLayerDef BeltLayer = ApparelLayerDefOf.Belt;

                BodyPartDef torsobodyPart = BodyPartDefOf.Torso;


                if (ApparelUtility != null && pawn.apparel?.WornApparel != null)
                {
                    Apparel belt = null;

                    Apparel other = null;

                    foreach (Apparel item in pawn.apparel.WornApparel)
                    {
                        if (item.def.thingCategories != null && item.def.thingCategories.Contains(ApparelUtility))
                        {

                            BodyPartRecord record = pawn.health.hediffSet.GetBodyPartRecord(torsobodyPart);

                            if(record != null && item.def.apparel.CoversBodyPart(record))
                            {
                                other = item;
                            }
                            else
                            {
                                if (item.def?.apparel?.layers != null && item.def.apparel.layers.Contains(BeltLayer))
                                {
                                    belt = item;
                                }
                            }
                                
                            /*
                            if (item.def?.apparel?.layers != null && item.def.apparel.layers.Contains(BeltLayer))
                            {
                                belt = item;
                            }
                            else
                            {
                                other = item;
                            }
                            */
                            //return item;
                        }
                    }

                    if(belt != null)
                    {
                        return belt;
                    }
                    else if (other != null)
                    {
                        return other;
                    }


                }
                return null;
            }
        }
    }


    /*
    [StaticConstructorOnStartup]
    public static class ColonistBarColonistDrawer_DrawColonist_Patch
    {
        [HarmonyPatch(typeof(ColonistBarColonistDrawer))]
        [HarmonyPatch("DrawColonist")]
        public static class ColonistBarColonistDrawer_DrawColonist
        {
            [HarmonyPostfix]
            public static void Postfix(ColonistBarColonistDrawer __instance, Rect rect, Pawn colonist, Map pawnMap, bool highlight, bool reordering)
            {
                Traverse traverse = Traverse.Create(__instance);
                //var Visible = traverse.Field("Visible").GetValue<bool>(); //always false for some reason
                var pawnLabelsCache = traverse.Field("pawnLabelsCache").GetValue<Dictionary<string, string>>();



                float alpha = Find.ColonistBar.GetEntryRectAlpha(rect);
                GUI.color = new Color(1f, 1f, 1f, alpha * 0.8f);
                GUI.color = Color.blue;
                float num4 = 4f * Find.ColonistBar.Scale;
                Vector2 pos = new Vector2(rect.center.x, rect.yMax - num4);
                GenMapUI.DrawPawnLabel(colonist, pos, alpha, rect.width + Find.ColonistBar.SpaceBetweenColonistsHorizontal - 2f, pawnLabelsCache);
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
            }

            private static ThingWithComps GetUtilityApparel(Pawn pawn)
            {
                ThingCategoryDef ApparelUtility = DefDatabase<ThingCategoryDef>.GetNamed("ApparelUtility");

                if (ApparelUtility != null)
                {
                    foreach (Apparel item in pawn.apparel.WornApparel)
                    {
                        if (item.def.thingCategories.Contains(ApparelUtility))
                        {
                            return item;
                        }
                    }
                }
                return null;
            }



        }
    }

    */
}



