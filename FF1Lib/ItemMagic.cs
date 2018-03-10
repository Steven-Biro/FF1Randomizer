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
	{ // TODO: clean up this entire file
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
		private List<String> SpellNames = new List<String> {
			"CURE  ", "HARM  ", "FOG   ", "RUSE  ", "FIRE  ", "SLEP  ", "LOCK  ", "LIT   ", "LAMP  ", "MUTE  ", "ALIT  ",
			"INVS  ", "ICE   ", "DARK  ", "TMPR  ", "SLOW  ", "CUR2  ", "HRM2  ", "AFIR  ", "HEAL  ", "FIR2  ", "HOLD  ",
			"LIT2  ", "LOK2  ", "PURE  ", "FEAR  ", "AICE  ", "AMUT  ", "SLP2  ", "FAST  ", "CONF  ", "ICE2  ", "CUR3  ",
			"LIFE  ", "HRM3  ", "HEL2  ", "FIR3  ", "BANE  ", "WARP  ", "SLO2  ", "SOFT  ", "EXIT  ", "FOG2  ", "INV2  ",
			"LIT3  ", "RUB   ", "QAKE  ", "STUN  ", "CUR4  ", "HRM4  ", "ARUB  ", "HEL3  ", "ICE3  ", "BRAK  ", "SABR  ",
			"BLND  ", "LIF2  ", "FADE  ", "WALL  ", "XFER  ", "NUKE  ", "STOP  ", "ZAP   ", "XXXX  " // must be 7 characters long each, add spaces if needed
		};
		private readonly List<byte> SpellsToRemove = new List<byte> { 48, 24, 38, 40, 41 }; // Cure 4, Pure, Warp, Soft, Exit
		// TODO: define lists of indices for each item restricting what spells it can get


		private void Helper(List<MagicSpell> Spells,int itemSize,int i, int afterRibbon, int itemOffset, int itemStart)
		{
			var output = (Spells[i].Data.ToHex());
			output = output.Remove(output.Length - 6);
			var offset = itemOffset + itemSize * ((byte)ItemLists.AllMagicItem.ElementAt(i) - itemStart) + MagicBitOffset;
			Put(offset,
				Blob.FromHex(output));
			offset = WeaponTextOffset + afterRibbon + WeaponTextSize * ((byte)ItemLists.AllMagicItem.ElementAt(i) - WeaponStart);
			output = (Spells[i].Name.ToHex());
			output = output.Remove(output.Length - 2);
			if (ItemLists.AllMagicItem.ElementAt(i) == Item.Defense)
			{
				output += "FF"; // Remove the last leffter from the Defense, this is usually where the icon is
			}
			Put(offset, Blob.FromHex(output));
		}

		public void ShuffleItemMagic(MT19337 rng)
		{
			//var ItemText = ReadText(ItemTextPointerOffset, ItemTextPointerBase, ItemTextPointerCount);
			List<byte> SpellIndex = new List<byte>();

			var Spells = SpellNames.Select((text, i) => new MagicSpell
			{
				Data = Blob.FromInts(new int[1] { i+1 }), // spells are 1 based
				Index = (byte)i,
				Name = FF1Text.TextToBytes(text),
			}).ToList();

			Spells.RemoveAll(spell => SpellsToRemove.Contains(spell.Index));

			Spells.Shuffle(rng);
			for (int i = 0; i < ItemLists.AllMagicItem.Count; i++)
			{
				if ((byte)ItemLists.AllMagicItem.ElementAt(i) <= 67)
				{
					Helper(Spells, 8, i, 0, WeaponOffset, WeaponStart);				
				}
				else
				{
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
