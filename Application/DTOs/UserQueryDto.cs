namespace Hospital.Application.DTOs;

public record UserQueryDto(
    int Page = 1,
    int PageSize = 20
);