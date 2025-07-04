﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace VendingMachines
{
    public class WorkGiver_EmptyVendingMachine : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => pawn?.Map != null ? VendingMachineJobHelper.GetActiveVendingMachines(pawn.Map) : Array.Empty<Thing>();

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var vendingMachine = t.TryGetComp<CompVendingMachine>();
            if (vendingMachine == null) return false;
            if (vendingMachine.ShouldBeEmptied() && pawn.CanReserve(t, 1, 1))
            {
                var silver = vendingMachine.GetDirectlyHeldThings()?.FirstOrDefault();
                if (silver != null)
                {
                    if (StoreUtility.TryFindBestBetterStorageFor(silver, pawn, pawn.Map,
                            StoreUtility.CurrentStoragePriorityOf(silver), pawn.Faction, out _, out _))
                    {
                        var haulJob = HaulAIUtility.HaulToStorageJob(pawn, silver, false);
                        if (haulJob != null) return true;
                    }
                }
            }
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var vendingMachine = t.TryGetComp<CompVendingMachine>();
            var silver = vendingMachine?.GetDirectlyHeldThings()?.FirstOrDefault();
            return JobMaker.MakeJob(InternalDefOf.VendingMachines_EmptyVendingMachine, t, silver);
        }
    }
}
