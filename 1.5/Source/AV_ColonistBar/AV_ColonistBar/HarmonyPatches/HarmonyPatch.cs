using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
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
                //Log.Message("colonist bar: Visible :" + Visible.ToString());
                //Log.Message("colonist bar: ShowGroupFrames :" + ShowGroupFrames.ToString());

                if (!__instance.Visible)
                {
                    //Log.Message("colonist bar not visible, skipping");
                    return false;   //skip original
                }
                if (Event.current.type != EventType.Layout)
                {
                    //Log.Message("colonist bar not layout");
                    List<ColonistBar.Entry> entries = __instance.Entries;
                    int num = -1;
                    bool showGroupFrames = __instance.ShowGroupFrames;
                    int value = -1;
                    for (int i = 0; i < __instance.cachedDrawLocs.Count; i++)
                    {
                        Rect rect = new Rect(__instance.cachedDrawLocs[i].x, __instance.cachedDrawLocs[i].y, __instance.Size.x, __instance.Size.y);
                        ColonistBar.Entry entry = entries[i];
                        bool flag = num != entry.group;
                        num = entry.group;
                        if (Event.current.type == EventType.Repaint)
                        {
                            if (flag)
                            {
                                value = ReorderableWidget.NewGroup(entry.reorderAction, ReorderableDirection.Horizontal, new Rect(0f, 0f, UI.screenWidth, UI.screenHeight), __instance.SpaceBetweenColonistsHorizontal, entry.extraDraggedItemOnGUI);
                            }
                            __instance.cachedReorderableGroups[i] = value;
                        }
                        bool reordering;
                        if (entry.pawn != null)
                        {
                            __instance.drawer.HandleClicks(rect, entry.pawn, __instance.cachedReorderableGroups[i], out reordering);
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
                            __instance.drawer.DrawColonist(rect, entry.pawn, entry.map, __instance.colonistsToHighlight.Contains(entry.pawn), reordering);

                            ThingWithComps thingWithComps = entry.pawn.equipment?.Primary;
                            ThingWithComps thingWithComps_2 = GetUtilityApparel(entry.pawn);

                            float offset = 0f;

                            if (thingWithComps != null && thingWithComps_2 != null)
                            {
                                offset = rect.width / 2f * (__instance.Scale / 2f) + ( 2f * __instance.Scale);
                            }

                            if ((Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.Always || (Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.WhileDrafted && entry.pawn.Drafted)) && thingWithComps != null && thingWithComps.def.IsWeapon)
                            {
                                Widgets.ThingIcon(new Rect(rect.x - offset, rect.y + rect.height * ColonistBar.WeaponIconOffsetScaleFactor, rect.width, rect.height).ScaledBy(ColonistBar.WeaponIconScaleFactor), thingWithComps, 1f, null, stackOfOne: true);
                            }
                            if ((Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.Always || (Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.WhileDrafted && entry.pawn.Drafted)) && thingWithComps_2 != null)
                            {
                                Widgets.ThingIcon(new Rect(rect.x + offset, rect.y + rect.height * ColonistBar.WeaponIconOffsetScaleFactor, rect.width, rect.height).ScaledBy(ColonistBar.WeaponIconScaleFactor), thingWithComps_2, 1f, null, stackOfOne: true);
                            }
                        }
                    }
                    num = -1;
                    if (showGroupFrames)
                    {
                        for (int j = 0; j < __instance.cachedDrawLocs.Count; j++)
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
                    __instance.colonistsToHighlight.Clear();
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



