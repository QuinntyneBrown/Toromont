namespace IronvaleFleetHub.Api.DTOs;

public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}
