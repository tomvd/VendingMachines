using UnityEngine;
using Verse;

namespace VendingMachines;

public class Settings : ModSettings
{
    public float EmptyAfterHours;
    //public bool AcceptSurgery;
    public override void ExposeData()
    {
        Scribe_Values.Look(ref EmptyAfterHours, "emptyAfterHours", 4f);
        //Scribe_Values.Look(ref AcceptSurgery, "acceptSurgery", true);
        base.ExposeData();
    }

    public void DoWindowContents(Rect inRect)
    {
        Listing_Standard listingStandard = new Listing_Standard();
        listingStandard.Begin(inRect);
        listingStandard.SliderLabeled("EmptyAfterHours".Translate(), ref EmptyAfterHours, EmptyAfterHours.ToString("0"), 0f, 24f,
            "EmptyAfterHoursTooltip".Translate());
        //listingStandard.CheckboxLabeled("Accept Surgery", ref AcceptSurgery, "uncheck this if you do not want to get surgery events");
        listingStandard.End();
    }
}