public class ChatItem
{
    public string Time { get; set; }
    public string Tag { get; set; }
    public string Body { get; set; }
    public bool IsMe { get; set; } 
    public bool IsSys { get; set; }

    public override string ToString() =>
        (string.IsNullOrEmpty(Time) ? "" : $"[{Time}] ") + $"[{Tag}] {Body}";
}
