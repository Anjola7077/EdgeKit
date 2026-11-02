
namespace EdgeKit.Models;

public class EdgeProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
