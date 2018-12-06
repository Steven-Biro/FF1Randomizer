namespace FF1R.Commands
{
	using System;
	using McMaster.Extensions.CommandLineUtils;

	using FF1Lib;
	using FFR.Common;

	[Command("generate", Description = "Randomize a Final Fantasy ROM")]
	class Generate
	{
		[Argument(0, Description = "Final Fantasy ROM to randomize")]
		[FileExists]
		public string RomPath { get; }

		[Option(Description = "File path for the generated ROM",
			ShortName = "o")]
		[LegalFilePath]
		public string OutFile { get; }

		[Option(Description = "8 Character Hexadecimal string to use as a seed",
			ShortName = "s")]
		public string Seed { get; }

		[Option("-f|--flags <FLAGS>", Description = "Base64 encoded FFR flag string")]
		public string FlagString { get; }

		// Will take precedence over the above Seed and Flags if present. 
		[Option(Description = "SEED_FLAGS string, as generated by crimbot",
			ShortName = "i")]
		public string Import { get; }

		// Will take precedence over any flags defined by earlier options.
		[Option(Description = "Preset to load randomizer settings from",
			ShortName = "p")]
		public string Preset { get; }

		[Option(Description = "Enable verbose output",
			ShortName = "v")]
		public bool Verbose { get; }

		int OnExecute(IConsole console)
		{
			RandomizerSettings settings;
			if (!String.IsNullOrEmpty(Preset))
			{
				try
				{
					settings = new RandomizerSettings(
						Seed,
						FFR.Common.Presets.Load(Preset).Flags
					);
				}
				catch(System.IO.FileNotFoundException)
				{
					console.Error.WriteLine($"Preset not found: '{Preset}'");
					return 1;
				}
			}
			else if (!String.IsNullOrEmpty(Import))
			{
				try
				{
					settings = RandomizerSettings.FromImportString(Import);
				}
				catch
				{
					console.Error.WriteLine("Ensure that you are using an import string in a format as supplied by crim_bot or the finalfantasyrandomizer.com web version.");
					return 1;
				}
			}
			else
			{
				try
				{
					settings = new RandomizerSettings(Seed, FlagString);
				}
				catch(Exception e)
				{
					console.Error.WriteLine(e.ToString());
					console.Error.WriteLine("Ensure that you are using an 8 character hexadecimal string as a seed and a valid base64 encoded set of flags.");
					return 1;
				}
			}

			var outFile = String.IsNullOrEmpty(OutFile)
				? GenerateDefaultFilename(RomPath, settings)
				: OutFile;
      
			var rom = new FF1Rom(RomPath);
			rom.Randomize(settings.Seed, settings.Flags);
			rom.Save(outFile);

			if (Verbose) {
				console.WriteLine($"Seed: {settings.Seed.ToHex()}");
				console.WriteLine($"Flags: {Flags.EncodeFlagsText(settings.Flags)}");
				console.WriteLine($"ROM created at: {outFile}");
			}

			return 0;
		}

		string GenerateDefaultFilename(string rom, RandomizerSettings settings)
		{
			var baseName = rom.Substring(
				0,
				rom.LastIndexOf(".", StringComparison.InvariantCulture)
			);
			return $"{baseName}_{settings.SeedString}_{settings.FlagString}.nes";
		}
	}
}
