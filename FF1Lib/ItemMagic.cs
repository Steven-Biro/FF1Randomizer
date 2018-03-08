using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RomUtilities;
using System.Diagnostics;

namespace FF1Lib
{
	public partial class FF1Rom : NesRom
	{
		public const int WeaponTextPointerOffset = 0x2B738;
		public const int ArmourTextPointerOffset = 0x2B787;
		public const int WeaponTextCount = 40;
		public const int ArmourTextCount = 40;
		public const int WeaponTextPointerBase = ItemTextPointerBase;
		public const int ArmourTextPointerBase = ItemTextPointerBase;
		public const int WeaponTextOffset = 0x2B9BD;
		public const int ArmourTextOffset = 0x2BCFD;
		public const int WeaponTextSize = 8;
		private const int MagicBitOffset = 0x3;
		private const int WeaponStart = 28; //(byte)ItemLists.AllWeapons.ElementAt(0)
		private const int ArmourStart = 68; //(byte)ItemLists.AllArmors.ElementAt(0)



		private void Helper(List<MagicSpell> Spells,int itemSize,int i, int afterRibbon, int itemOffset, int itemStart)
		{
			var output = (Spells[i].Data.ToHex());
			output = output.Remove(output.Length - 6);
			var offset = itemOffset + itemSize * ((byte)ItemLists.AllMagicItem.ElementAt(i) - itemStart) + MagicBitOffset;
			Put(offset,
				Blob.FromHex(output));
			offset = WeaponTextOffset + afterRibbon + WeaponTextSize * ((byte)ItemLists.AllMagicItem.ElementAt(i) - WeaponStart);
			output = (Spells[i].Name.ToHex());
			output = output.Remove(output.Length - 2) + "FFFF";
			if (ItemLists.AllMagicItem.ElementAt(i) == Item.Defense)
			{
				output += "FF"; // Remove the last leffter from the Defense, this is usually where the icon is
			}
			Put(offset, Blob.FromHex(output));
		}

		public void ShuffleItemMagic(MT19337 rng)
		{
			//var ItemText = ReadText(ItemTextPointerOffset, ItemTextPointerBase, ItemTextPointerCount);
			List<Blob> SpellText = Get(MagicNamesOffset, MagicNameSize * MagicCount).Chunk(MagicNameSize);
			List<byte> SpellIndex = new List<byte>();

			var Spells = SpellText.Select((text, i) => new MagicSpell
			{
				Data = Blob.FromInts(new int[1] { i+1 }), // spells are 1 based
				Index = (byte)i,
				Name = text,
			}).ToList();

			Spells.Shuffle(rng);
			for (int i = 0; i < ItemLists.AllMagicItem.Count; i++)
			{
				if ((byte)ItemLists.AllMagicItem.ElementAt(i) <= 67)
				{
					//continue;
					Helper(Spells, 8, i, 0, WeaponOffset, WeaponStart);				
				}
				else
				{
				//	continue;
					if ((byte)ItemLists.AllMagicItem.ElementAt(i) > 99)
					{
						Helper(Spells, 4, i, 1, ArmorOffset, ArmourStart);
					}
					else
					{
						Helper(Spells, 4, i, 0, ArmorOffset, ArmourStart);
					}
				}

			}


		}


	}
}
