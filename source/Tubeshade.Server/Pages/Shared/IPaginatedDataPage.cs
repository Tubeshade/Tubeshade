namespace Tubeshade.Server.Pages.Shared;

public interface IPaginatedDataPage<TData>
{
    int? PageSize { get; set; }

    int? PageIndex { get; set; }

    PaginatedData<TData> PageData { get; set; }
}
