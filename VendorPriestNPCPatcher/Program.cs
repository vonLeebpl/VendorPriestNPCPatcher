using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using VendorPriestNPCPatcher.Utilities;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;


namespace VendorPriestNPCPatcher
{
    public class Program
    {
        const string PVendorPatchName = "Priest Vendors SE.esp";
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch, new PatcherPreferences()
                {
                    ExclusionMods = new List<ModKey>()
                    {
                         new ModKey(PVendorPatchName, ModType.Plugin),
                         new ModKey("Nemesis PCEA.esp", ModType.Plugin)
                    }
                })
                .SetTypicalOpen(GameRelease.SkyrimSE, "VendorPriestNPCPatcher.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var VPriest = state.LoadOrder.GetModByFileName("Priest Vendors SE.esp");

            if (VPriest == null)
            {
                System.Console.WriteLine("Priest Vendors SE.esp not found.");
                return;
            }

            var VPriestFormIDs = VPriest.Npcs.Select(x => x.FormKey).ToList();
            var winningOverrides = state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>().Where(x => VPriestFormIDs.Contains(x.FormKey)).ToList();

            int changed = 0;

            foreach (var npc in VPriest.Npcs)
            {
                if (new List<uint>() { 7, 14 }.Contains(npc.FormKey.ID)) continue; // ignore player record

                var winningOverride = winningOverrides.Where(x => x.FormKey == npc.FormKey).First();

                var patchNpc = state.PatchMod.Npcs.GetOrAddAsOverride(winningOverride);
                foreach (var fac in npc.Factions)
                    if (!patchNpc.Factions.Select(x => new KeyValuePair<FormKey, int>(x.Faction.FormKey, x.Rank)).Contains(new KeyValuePair<FormKey, int>(fac.Faction.FormKey, fac.Rank)))
                    {
                        patchNpc.Factions.Add(fac.DeepCopy());
                        changed++;
                    }
            }

            System.Console.WriteLine("VendorPriestPatcher patched " + changed + " records.");

            if (state.PatchMod.ModKey.Name == PVendorPatchName)
            {
                state.PatchMod.ModHeader.Flags = state.PatchMod.ModHeader.Flags | SkyrimModHeader.HeaderFlag.LightMaster;
            }
        }
    }
}
