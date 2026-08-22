namespace ConnectHub.BLL.Common.Pagination;

public class PaginationParams
{
    private const int MaxPageSize = 100;
    private int _take = 20;

    public int Skip { get; set; } = 0;

    public int Take
    {
        get => _take;
        set => _take = value > MaxPageSize ? MaxPageSize : (value < 1 ? 1 : value);
    }
}
