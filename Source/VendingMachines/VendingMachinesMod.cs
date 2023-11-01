using RimWorld;
using UnityEngine;
using Verse;

namespace VendingMachines;

[DefOf]
public static class InternalDefOf
{
	static InternalDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(InternalDefOf));
	}
	
	public static ThoughtDef GuestCheapVendingmachine;
	public static ThoughtDef GuestExpensiveVendingmachine;
	public static ThoughtDef GuestFreeVendingmachine;
	public static JobDef VendingMachines_EmptyVendingMachine;
}

[StaticConstructorOnStartup]
public class VendingMachinesMod : Mod
{
	public static Settings Settings;
	
	public VendingMachinesMod(ModContentPack content) : base(content)
	{
		Settings = GetSettings<Settings>();
		//Log.Message("VendingMachinesMod:start ");
        
        
    }
	
	/// <summary>
    /// The (optional) GUI part to set your settings.
    /// </summary>
    /// <param name="inRect">A Unity Rect with the size of the settings window.</param>
    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.DoWindowContents(inRect);
    }

    /// <summary>
    /// Override SettingsCategory to show up in the list of settings.
    /// Using .Translate() is optional, but does allow for localisation.
    /// </summary>
    /// <returns>The (translated) mod name.</returns>
    public override string SettingsCategory()
    {
        return "Hospitality: Vending machines";
    }
}