﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using RomUtilities;
using System.Text;
using FF1Lib;

namespace Sandbox
{
	public static class ItemDistribution
	{
		private static List<MapLocation> ItemShops =
			new List<MapLocation>
			{
				MapLocation.ConeriaTown,
				MapLocation.Pravoka,
				MapLocation.ElflandTown,
				MapLocation.CresentLake,
				MapLocation.Onrac,
				MapLocation.Gaia,
				MapLocation.Caravan
			};
		public static void RunStats(MT19337 rng, ITreasureShuffleFlags flags, IncentiveData incentivesData)
		{
			var forcedItems = ItemLocations.AllOtherItemLocations.ToList();
			if (!flags.NPCItems)
			{
				forcedItems = ItemLocations.AllNonTreasureItemLocations.ToList();
			}

			var placedItems = new List<IRewardSource>();
			var treasurePool = ItemLocations.AllTreasures.Where(x => !x.IsUnused).Select(x => x.Item).ToList();
			var requirementChecks = ItemLists.AllQuestItems.ToDictionary(x => x, x => 0);
			requirementChecks[Item.Ribbon] = 0;
			var requirementsToCheck = new List<Item> {
				Item.Crown, Item.Crystal, Item.Herb, Item.Tnt,
				Item.Adamant, Item.Slab, Item.Ruby, Item.Bottle, Item.Canal
			};
			const int maxIterations = 10000;
			var forcedIceCount = 0;
			var itemPlacementStats = ItemLists.AllQuestItems.ToDictionary(x => x, x => new List<int>());
			itemPlacementStats[Item.Ribbon] = new List<int>();
			var itemPlacementZones = ItemLists.AllQuestItems.ToDictionary(x => x, x => new List<string>());
			itemPlacementZones[Item.Ribbon] = new List<string>();
			var mapLocationRequirements = ItemLocations.MapLocationRequirements.ToDictionary(x => x.Key, x => x.Value.ToList());
			long iterations = 0;
			while (iterations < maxIterations)
			{
				iterations++;
				// When Enemy Status Shuffle is turned on Coneria is reduced to 11% chance with other shops spliting the remaining 89%
				var shopTownSelected = ItemShops.PickRandom(rng);
				var itemShopItem = new ItemShopSlot(ItemLocations.CaravanItemShop1.Address,
													$"{Enum.GetName(typeof(MapLocation), shopTownSelected)}Shop",
													shopTownSelected,
													Item.Bottle);
				placedItems = ItemPlacement.PlaceSaneItems(rng,
														   flags,
														   incentivesData,
														   treasurePool,
														   itemShopItem,
														   mapLocationRequirements);

				var outputIndexes = placedItems.ToLookup(x => x.Item, x => x);
				foreach (Item item in itemPlacementStats.Keys)
				{
					itemPlacementStats[item].AddRange(outputIndexes[item].Select(x => x.Address).ToList());
				}
				var outputZones =
					placedItems
						.ToLookup(x => x.Item,
								  x => Enum.GetName(typeof(MapLocation), x.MapLocation));
				foreach (Item item in itemPlacementZones.Keys)
				{
					if (!outputZones[item].Any()) continue;
					itemPlacementZones[item].AddRange(outputZones[item]);
				}
				var matoyaShip = placedItems.Any(x => x.Address == ItemLocations.Matoya.Address && x.Item == Item.Ship);
				var crystalIceCave = placedItems.Any(x => x.Item == Item.Crystal &&
									x.Address >= ItemLocations.IceCave1.Address &&
													 x.Address <= ItemLocations.IceCaveMajor.Address);
				var keyIceCave = placedItems.Any(x => x.Item == Item.Key &&
								x.Address >= ItemLocations.IceCave1.Address &&
												 x.Address <= ItemLocations.IceCaveMajor.Address);
				var keyLockedCrystal = placedItems.Any(x => x.Item == Item.Crystal &&
													   x.AccessRequirement.HasFlag(AccessRequirement.Key));
				var keyLockedShip = placedItems.Any(x => x.Item == Item.Ship &&
													x.AccessRequirement.HasFlag(AccessRequirement.Key));
				if ((keyLockedShip && keyIceCave) ||
					(matoyaShip && crystalIceCave) ||
					(matoyaShip && keyLockedCrystal && keyIceCave))
					forcedIceCount++;

				foreach (Item item in requirementsToCheck)
				{
					if (!ItemPlacement.CheckSanity(placedItems.Where(x => x.Item != item).ToList(), flags, mapLocationRequirements))
						requirementChecks[item]++;
				}
			}

			if (iterations > 10)
			{
				Debug.WriteLine(PrintStats(maxIterations, itemPlacementStats, itemPlacementZones, requirementChecks));
				Debug.WriteLine($"Forced Early Ice Cave for Ship: {forcedIceCount} out of {maxIterations}");
			}
		}

		private static string PrintStats(int maxIterations, Dictionary<Item, List<int>> incentiveLocations, Dictionary<Item, List<string>> incentiveZones, Dictionary<Item, int> requirements)
		{
			var sb = new StringBuilder();
			sb.Append("Location         ,");
			foreach (Item item in incentiveLocations.Keys)
			{
				var name = Enum.GetName(typeof(Item), item);
				name = $"{string.Join("", Enumerable.Repeat(" ", Math.Max(1, 9 - name.Length)))}{name},";
				sb.Append(name.Substring(0, Math.Min(12, name.Length)));
			}
			sb.Append("\n");
			foreach (var rewardSource in ItemLocations.AllQuestItemLocations.Where(x => x.Address < 0x80000)) // && x.Address > 0x31FF))
			{
				sb.Append($"{rewardSource.Name}" +
						  $"{string.Join("", Enumerable.Repeat(" ", Math.Max(1, 17 - rewardSource.Name.Length)))},");
				foreach (var itemPlacements in incentiveLocations.Values)
				{
					var percentage = $"   {100.0 * itemPlacements.Count(x => x == rewardSource.Address) / maxIterations:g2}";
					percentage = $"{string.Join("", Enumerable.Repeat(" ", Math.Max(1, 9 - percentage.Length)))}{percentage},";
					sb.Append(percentage);
				}
				sb.Append("\n");
			}
			foreach (var zoneName in Enum.GetNames(typeof(MapLocation)))
			{
				sb.Append($"{zoneName}" +
						  $"{string.Join("", Enumerable.Repeat(" ", Math.Max(1, 17 - zoneName.Length)))},");
				foreach (var itemPlacements in incentiveZones.Values)
				{
					var percentage = $"   {100.0 * itemPlacements.Count(x => x == zoneName) / maxIterations:g2}";
					percentage = $"{string.Join("", Enumerable.Repeat(" ", Math.Max(1, 9 - percentage.Length)))}{percentage},";
					sb.Append(percentage);
				}
				sb.Append("\n");
			}
			sb.Append($"Required         ,");
			foreach (var requiredCount in requirements.Values)
			{
				var percentage = $"   {100.0 * requiredCount / maxIterations:g2}";
				percentage = $"{string.Join("", Enumerable.Repeat(" ", Math.Max(1, 9 - percentage.Length)))}{percentage},";
				sb.Append(percentage);
			}
			sb.Append("\n");
			return sb.ToString();
		}
	}
}
