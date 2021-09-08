using System;
using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using RomUtilities;
using System.IO;
using FFR.Common;
using Newtonsoft.Json;
using FF1Lib;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using System.Linq;

namespace FF1R.Commands
{
    [Command("procgen", Description = "Create a procedurally generated map")]
    class Procgen
    {
	[Argument(0, Description = "The seed")]
	public int Seed { get; }

	int OnExecute(IConsole console)
	{
	    var rng = new MT19337((uint)this.Seed);
	    var replacementMap = FF1Lib.Procgen.NewOverworld.GenerateNewOverworld(rng);
	    replacementMap.Checksum = replacementMap.ComputeChecksum();
	    replacementMap.Seed = this.Seed;
	    replacementMap.FFRVersion = FF1Lib.FFRVersion.Version;

	    using (StreamWriter file = File.CreateText($"FFR_map_{replacementMap.Checksum}.json")) {
		JsonSerializer serializer = new JsonSerializer();
		serializer.Formatting = Formatting.Indented;
		serializer.Serialize(file, replacementMap);
	    }
	    return 0;
	}
    }

    [Command("render", Description = "Render map")]
    class MapRender
    {
	[Argument(0, Description = "The map")]
	public string Mapfile { get; }

	int OnExecute(IConsole console)
	{

	    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
	    var resourcePath = assembly.GetManifestResourceNames().First(str => str.EndsWith("maptiles.png"));

	    using (Stream stream = assembly.GetManifestResourceStream(resourcePath)) {
		IImageFormat format;
		var tiles = Image.Load(stream, out format);

		using (StreamReader file = new StreamReader(this.Mapfile)) {
		    JsonSerializer serializer = new JsonSerializer();

		    var map = serializer.Deserialize<OwMapExchangeData>(new JsonTextReader(file));
		    var rows = new List<List<byte>>();
		    foreach (var c in map.DecompressedMapRows) {
			rows.Add(new List<byte>(Convert.FromBase64String(c)));
		    }
		    var output = new Image<Rgba32>(16 * 256, 16 * 256);

		    for (int y = 0; y < 256; y++) {
			for (int x = 0; x < 256; x++) {
			    var t = rows[y][x];
			    var tile_row = t/16;
			    var tile_col = t%16;
			    var src = tiles.Clone(d => d.Crop(new Rectangle(tile_col*16, tile_row*16, 16, 16)));
			    output.Mutate(d => d.DrawImage(src, new Point(x*16, y*16), 1));
			}
		    }
		    output.Save("map.png");
		}

		return 0;
	    }
	}
    }
}
