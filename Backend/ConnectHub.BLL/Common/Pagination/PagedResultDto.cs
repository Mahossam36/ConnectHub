namespace ConnectHub.BLL.Common.Pagination;

public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int Total { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }

    public PagedResultDto()
    {
    }

    public PagedResultDto(IReadOnlyList<T> items, int total, int skip, int take)
    {
        Items = items;
        Total = total;
        Skip = skip;
        Take = take;
    }
}
