
namespace EdgeKit.Models;

public class QuickAction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Title { get; set; } = "";

    public string Type { get; set; } = "note";

    public string Content { get; set; } = "";

    public string ProfileId { get; set; } = "default";

    public string Icon { get; set; } = "●";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsPinned { get; set; } = true;
}
