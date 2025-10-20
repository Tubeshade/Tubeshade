using System.Collections.Generic;
using Tubeshade.Server.Pages.Libraries.Tasks;

namespace Tubeshade.Server.Pages.Shared;

public interface ITaskPage : IPaginatedDataPage<TaskModel>
{
    Dictionary<string, string?> GetRouteValues(int pageIndex) => new()
    {
        { nameof(PageSize), PageSize?.ToString() },
        { nameof(PageIndex), $"{pageIndex}" },
    };
}
