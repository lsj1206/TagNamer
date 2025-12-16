namespace TagNamer.Models;

public class TagItem
{
    public string DisplayName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ToolTip { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
}
