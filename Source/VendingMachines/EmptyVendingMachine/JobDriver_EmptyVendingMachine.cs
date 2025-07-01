﻿using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace VendingMachines
{
    public class JobDriver_EmptyVendingMachine : JobDriver
    {
        private const TargetIndex IndexVM = TargetIndex.A;
        private const TargetIndex IndexSilver = TargetIndex.B;
        private CompVendingMachine VendingMachine => job.GetTarget(IndexVM).Thing.TryGetComp<CompVendingMachine>();
        private Thing VMParent => job.GetTarget(IndexVM).Thing;
        private Thing Silver => job.GetTarget(IndexSilver).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(VendingMachine.parent, job, 1, 1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(IndexVM);
            this.FailOnForbidden(IndexVM);
            yield return Toils_Goto.GotoThing(IndexVM, PathEndMode.Touch);
            yield return Toils_General.Do(GetSilver);
            //yield return Toils_Haul.StartCarryThing(IndexSilver, true).FailOnDestroyedOrNull(IndexSilver);
            yield return Toils_General.DoAtomic(Haul).FailOnDestroyedOrNull(IndexSilver);
        }

        private void Haul()
        {
            if (HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, Silver, false))
            {
                var haulJob = HaulAIUtility.HaulToStorageJob(pawn, Silver, false);
                if (haulJob != null)
                {
                    pawn.jobs.StartJob(haulJob, JobCondition.Succeeded);
                    VendingMachine.Emptied();
                    return;
                }
            }
            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
        }

        private void GetSilver()
        {
            VendingMachine.GetDirectlyHeldThings().TryDrop(Silver, VMParent.Position, VMParent.Map, ThingPlaceMode.Near, Silver.stackCount, out _, (thing, i) => pawn.CurJob.SetTarget(IndexSilver, thing));
        }
	}
}
