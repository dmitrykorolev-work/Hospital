using Hospital.Application.DTOs;

namespace Hospital.ConsoleClient.Interfaces;

internal interface IPagedTable
{
    public Task<int?> ShowPagedTable<TDto>(PagedResult<TDto> data, int page = 1);
}