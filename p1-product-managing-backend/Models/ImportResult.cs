public class ImportResult
{
    public int InsertedRow { get; set; }
    public List<string> Errors { get; set; } = new();
}