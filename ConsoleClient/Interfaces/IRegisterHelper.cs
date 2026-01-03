using Hospital.Application.DTOs;

namespace Hospital.ConsoleClient.Interfaces;

internal interface IRegisterHelper
{
    public Task<UserRegisterDto?> RegisterPrompt();
}
