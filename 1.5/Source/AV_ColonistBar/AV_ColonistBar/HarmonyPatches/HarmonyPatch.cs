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
                    Log.Warning("[AV] Show Utility Apparel - Failed to patch ColonistBar.ColonistBarOnGUI()");
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


    //old without transpiler
    /*
    [StaticConstructorOnStartup]
    public static class ColonistBar_ColonistBarOnGUI_Patch
    {


        [HarmonyPatch(typeof(ColonistBar))]
        [HarmonyPatch("ColonistBarOnGUI")]
        public static class ColonistBar_ColonistBarOnGUI
        {
            [HarmonyPrefix]
            public static bool Prefix(ColonistBar __instance)
            {
                Traverse traverse = Traverse.Create(__instance);
                //var Visible = traverse.Field("Visible").GetValue<bool>(); //always false for some reason
                var ShowGroupFrames = traverse.Field("ShowGroupFrames").GetValue<bool>();
                var cachedDrawLocs = traverse.Field("cachedDrawLocs").GetValue<List<Vector2>>();

                var cachedReorderableGroups = traverse.Field("cachedReorderableGroups").GetValue<List<int>>();
                var colonistsToHighlight = traverse.Field("colonistsToHighlight").GetValue<List<Pawn>>();

                var WeaponIconScaleFactor = traverse.Field("WeaponIconScaleFactor").GetValue<float>();
                var WeaponIconOffsetScaleFactor = traverse.Field("WeaponIconOffsetScaleFactor").GetValue<float>();
                
                //Log.Message("colonist bar: Visible :" + Visible.ToString());
                //Log.Message("colonist bar: ShowGroupFrames :" + ShowGroupFrames.ToString());

                if (!AV_Visible)
                {
                    //Log.Message("colonist bar not visible, skipping");
                    return false;   //skip original
                }
                if (Event.current.type != EventType.Layout)
                {
                    //Log.Message("colonist bar not layout");
                    List<ColonistBar.Entry> entries = __instance.Entries;
                    int num = -1;
                    bool showGroupFrames = ShowGroupFrames;
                    int value = -1;
                    for (int i = 0; i < cachedDrawLocs.Count; i++)
                    {
                        Rect rect = new Rect(cachedDrawLocs[i].x, cachedDrawLocs[i].y, __instance.Size.x, __instance.Size.y);
                        ColonistBar.Entry entry = entries[i];
                        bool flag = num != entry.group;
                        num = entry.group;
                        if (Event.current.type == EventType.Repaint)
                        {
                            if (flag)
                            {
                                value = ReorderableWidget.NewGroup(entry.reorderAction, ReorderableDirection.Horizontal, new Rect(0f, 0f, UI.screenWidth, UI.screenHeight), __instance.SpaceBetweenColonistsHorizontal, entry.extraDraggedItemOnGUI);
                            }
                            cachedReorderableGroups[i] = value;
                        }
                        bool reordering;
                        if (entry.pawn != null)
                        {
                            __instance.drawer.HandleClicks(rect, entry.pawn, cachedReorderableGroups[i], out reordering);
                        }
                        else
                        {
                            reordering = false;
                        }
                        if (Event.current.type != EventType.Repaint)
                        {
                            continue;
                        }
                        if (flag && showGroupFrames)
                        {
                            __instance.drawer.DrawGroupFrame(entry.group);
                        }
                        if (entry.pawn != null)
                        {
                            __instance.drawer.DrawColonist(rect, entry.pawn, entry.map, colonistsToHighlight.Contains(entry.pawn), reordering);

                            ThingWithComps thingWithComps = entry.pawn.equipment?.Primary;
                            ThingWithComps thingWithComps_2 = GetUtilityApparel(entry.pawn);

                            float offset = 0f;

                            if (thingWithComps != null && thingWithComps_2 != null)
                            {
                                offset = rect.width / 2f * (__instance.Scale / 2f) + ( 2f * __instance.Scale);
                            }

                            if ((Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.Always || (Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.WhileDrafted && entry.pawn.Drafted)) && thingWithComps != null && thingWithComps.def.IsWeapon)
                            {
                                Widgets.ThingIcon(new Rect(rect.x - offset, rect.y + rect.height * WeaponIconOffsetScaleFactor, rect.width, rect.height).ScaledBy(WeaponIconScaleFactor), thingWithComps, 1f, null, stackOfOne: true);
                            }
                            if ((Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.Always || (Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.WhileDrafted && entry.pawn.Drafted)) && thingWithComps_2 != null)
                            {
                                Widgets.ThingIcon(new Rect(rect.x + offset, rect.y + rect.height * WeaponIconOffsetScaleFactor, rect.width, rect.height).ScaledBy(WeaponIconScaleFactor), thingWithComps_2, 1f, null, stackOfOne: true);
                            }
                        }
                    }
                    num = -1;
                    if (showGroupFrames)
                    {
                        for (int j = 0; j < cachedDrawLocs.Count; j++)
                        {
                            ColonistBar.Entry entry2 = entries[j];
                            bool num2 = num != entry2.group;
                            num = entry2.group;
                            if (num2)
                            {
                                __instance.drawer.HandleGroupFrameClicks(entry2.group);
                            }
                        }
                    }
                }
                if (Event.current.type == EventType.Repaint)
                {
                   // Log.Message("colonist bar Repaint");
                    colonistsToHighlight.Clear();
                }
                //Log.Message("applied harmony colonist bar");
                return false;   //skip original
            }

            private static ThingWithComps GetUtilityApparel(Pawn pawn)
            {
                ThingCategoryDef ApparelUtility = DefDatabase<ThingCategoryDef>.GetNamed("ApparelUtility");

                ApparelLayerDef BeltLayer = DefDatabase<ApparelLayerDef>.GetNamed("Belt");

                BodyPartDef torsobodyPart = DefDatabase<BodyPartDef>.GetNamed("Torso");


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


            private static bool AV_Visible
            {
                get
                {
                    if (UI.screenWidth < 800 || UI.screenHeight < 500)
                    {
                        return false;
                    }
                    if (Find.TilePicker.Active)
                    {
                        return false;
                    }
                    return true;
                }
            }

        }
    }
    */


}



