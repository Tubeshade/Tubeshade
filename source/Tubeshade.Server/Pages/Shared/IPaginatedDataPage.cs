namespace Tubeshade.Server.Pages.Shared;

public interface IPaginatedDataPage<TData>
{
    public int? PageSize { get; set; }

    public int? PageIndex { get; set; }

    public PaginatedData<TData> PageData { get; set; }
}
