using System.IO;
using System.Text.Json;

namespace TanksIO;

public class Configuration
{
    public string Host { get; set; }
    public int Port { get; set; }

    public void Load()
    {
        using var file = File.Open("config.json", FileMode.Open);
        using var reader = new StreamReader(file);
        var json = reader.ReadToEnd();
        
        var configuration = JsonSerializer.Deserialize<Configuration>(json);
        
        Host = configuration.Host;
        Port = configuration.Port;
    }
}