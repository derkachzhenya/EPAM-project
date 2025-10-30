using CSharpFunctionalExtensions;
using FluentValidation.Results;
using MediatR;

namespace Epam.ItMarathon.ApiService.Application.UseCases.User.Commands
{
    /// <summary>
    /// Command for deleting a user from a room.
    /// </summary>
    /// <param name="UserCode">Authorization code of the admin user.</param>
    /// <param name="UserId">Unique identifier of the user to delete.</param>
    public record DeleteUserCommand(string UserCode, ulong UserId)
        : IRequest<Result<Unit, ValidationResult>>;
}
