using System;
using System.Collections.Generic;
using System.Linq;
using Hospitality;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace VendingMachines
{
    public class JobDriver_BuyItem : JobDriver
    {
        //Constants
        public const int MinShoppingDuration = 75;
        public const int MaxShoppingDuration = 300;

        //Properties
        protected Thing Item => job.targetA.Thing;
        protected Thing Storage => job.targetB.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA.Thing, job);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            this.FailOn(() => !ItemUtility.IsBuyableNow(pawn, Item));
            //AddEndCondition(() =>
            //{
            //    if (Deliveree.health.ShouldGetTreatment)
            //        return JobCondition.Ongoing;
            //    return JobCondition.Succeeded;
            //});

            if (TargetThingA != null)
            {
                Toil reserveTargetA = Toils_Reserve.Reserve(TargetIndex.A);

                yield return reserveTargetA;
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch)
                    .FailOnDespawnedNullOrForbidden(TargetIndex.A);

                int duration = Rand.Range(MinShoppingDuration, MaxShoppingDuration);
                yield return Toils_General.Wait(duration).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);

                Toil takeThing = new Toil();
                takeThing.initAction = () => BuyThing(takeThing);
                yield return takeThing.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            }

            //yield return Toils_Jump.Jump(gotoToil); // shop some more
        }

        private void BuyThing(Toil toil)
        {
            Job curJob = toil.actor.jobs.curJob; 
            //Toils_Haul.ErrorCheckForCarry(toil.actor, Item);
            if (curJob.count == 0)
            {
                throw new Exception(string.Concat("BuyItem job had count = ", curJob.count, ". Job: ", curJob));
            }
            CompVendingMachine vendingMachine = Storage.TryGetComp<CompVendingMachine>();
            if (vendingMachine == null)
            {
                throw new Exception(string.Concat("Storage ", Storage.Label, " - cant get CompVendingMachine"));
            }

            //if (Item.MarketValue <= 0) return;
            int maxSpace = toil.actor.GetInventorySpaceFor(Item);
            var inventory = toil.actor.inventory.innerContainer;

            Thing silver = inventory.FirstOrDefault(i => i.def == ThingDefOf.Silver);

            var maxAffordable = vendingMachine.Pricing == 0 ? 1 : vendingMachine.MaxCanAfford(toil.actor, Item); // don't buy more than x of free stuff
            if (maxAffordable < 1) return;

            // Changed formula a bit, so guests are less likely to leave small stacks if they can afford it
            var maxWanted = Rand.RangeInclusive(1, maxAffordable);
            int count = Mathf.Min(Item.stackCount, maxSpace, maxWanted);

            var price = Mathf.CeilToInt(count * vendingMachine.GetSingleItemPrice(Item));

            var map = toil.actor.MapHeld;
            var inventoryItemsBefore = inventory.ToArray();
            var thing = Item.SplitOff(count);

            // Notification
            //if (Settings.enableBuyNotification)
            {
                var text = price <= 0 ? "GuestTookFreeItem" : "GuestBoughtItem";
                Messages.Message(text.Translate(new NamedArgument(toil.actor.Faction, "FACTION"), price, new NamedArgument(toil.actor, "PAWN"), new NamedArgument(thing, "ITEM")), toil.actor, MessageTypeDefOf.SilentInput);
            }

            int tookItems;
            if (thing.def.IsApparel && thing is Apparel apparel && ApparelUtility.HasPartsToWear(pawn, apparel.def) && ItemUtility.AlienFrameworkAllowsIt(toil.actor.def, apparel.def, "CanWear"))
            {
                toil.actor.apparel.Wear(apparel);
                tookItems = apparel.stackCount;
            }
            else if (thing.def.IsWeapon && thing is ThingWithComps equipment && equipment.def.IsWithinCategory(ThingCategoryDefOf.Weapons)
                     && ItemUtility.AlienFrameworkAllowsIt(toil.actor.def, thing.def, "CanEquip"))
            {
                var primary = pawn.equipment.Primary;
                if (equipment.def.equipmentType == EquipmentType.Primary && primary != null)
                    if (!pawn.equipment.TryTransferEquipmentToContainer(primary, pawn.inventory.innerContainer))
                    {
                        Log.Message(pawn.Name.ToStringShort + " failed to take " + primary + " to his inventory.");
                    }
                
                pawn.equipment.AddEquipment(equipment);
                pawn.equipment.Notify_EquipmentAdded(equipment);
                equipment.def.soundInteract?.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
                tookItems = equipment.stackCount;
            }
            else
            {
                tookItems = inventory.TryAdd(thing, count);
            }

            //var comp = toil.actor.CompGuest();
            if (tookItems > 0)// && comp != null)
            {
                vendingMachine.ReceivePayment(toil.actor.inventory.innerContainer, silver, thing, count);

                if (vendingMachine.Pricing == 3)
                {
                    toil.actor.needs.mood.thoughts.memories.TryGainMemory(InternalDefOf.GuestExpensiveVendingmachine);
                }
                else if (vendingMachine.Pricing == 1)
                {
                    toil.actor.needs.mood.thoughts.memories.TryGainMemory(InternalDefOf.GuestCheapVendingmachine);
                }
                else if (vendingMachine.Pricing == 0)
                {
                    toil.actor.needs.mood.thoughts.memories.TryGainMemory(InternalDefOf.GuestFreeVendingmachine);
                }
                
                // Check what's new in the inventory (TryAdd creates a copy of the original object!)
                var newItems = toil.actor.inventory.innerContainer.Except(inventoryItemsBefore).ToArray();
                foreach (var item in newItems)
                {
                    //comp.boughtItems.Add(item.thingIDNumber);

                    // Handle trade stuff
                    Trade(toil, item, map);
                }
            }
            else
            {
                // Failed to equip or take
                if (!GenDrop.TryDropSpawn(thing, toil.actor.Position, map, ThingPlaceMode.Near, out _))
                {
                    Log.Warning(toil.actor.Name.ToStringShort + " failed to buy and failed to drop " + thing.Label);
                }
            }
        }

        private void Trade(Toil toil, Thing item, Map map)
        {
            if (item is ThingWithComps twc && map.mapPawns.FreeColonistsSpawnedCount > 0)
            {
                twc.PreTraded(TradeAction.PlayerSells, map.mapPawns.FreeColonistsSpawned.RandomElement(), toil.actor);
            }

            // Register with lord toil
            //var lord = pawn.GetLord();
            //var lordToil = lord?.CurLordToil as LordToil_VisitPoint;

            //lordToil?.OnPlayerSoldItem(item);
        }
    }
}
