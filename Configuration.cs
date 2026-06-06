using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TanksIO;

public class Configuration
{
	public string Host { get; set; }
	public int Port { get; set; }

	public void Load()
	{
		var path = "config.json";

		if (!Path.Exists(path))
		{
			path = Directory.GetFiles("./", "config.json", SearchOption.AllDirectories)
				.FirstOrDefault() ?? throw new ApplicationException("config.json not found");
		}
		
		using var file = File.Open(path, FileMode.Open);
		using var reader = new StreamReader(file);
		var json = reader.ReadToEnd();
		
		var configuration = JsonSerializer.Deserialize<Configuration>(json);
		
		Host = configuration.Host;
		Port = configuration.Port;
	}
}
