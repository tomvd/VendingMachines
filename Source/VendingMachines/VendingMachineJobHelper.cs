using System.Collections.Generic;
using System.Linq;
using Hospitality;
using Verse;
using CompVendingMachine = VendingMachines.CompVendingMachine;

namespace VendingMachines
{
    public static class VendingMachineJobHelper
	{

		public static List<Building> GetActiveVendingMachines(Map map)
		{
			return map.listerBuildings.allBuildingsColonist
				.Where(building => building.IsVendingMachine()).ToList();
		}

		public static bool IsVendingMachine(this Building building)
		{
			return building.comps.Any(comp => comp is CompVendingMachine machine && machine.IsActive() && machine.IsPowered());
		}
	}
}