using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Commands;
using Epam.ItMarathon.ApiService.Domain.Abstract;
using Epam.ItMarathon.ApiService.Domain.Shared.ValidationErrors;
using FluentValidation.Results;
using MediatR;

namespace Epam.ItMarathon.ApiService.Application.UseCases.User.Handlers
{
    /// <summary>
    /// Handler for deleting a user from a room.
    /// </summary>
    /// <param name="userRepository">Implementation of <see cref="IUserReadOnlyRepository"/> for operating with database.</param>
    /// <param name="roomRepository">Implementation of <see cref="IRoomRepository"/> for operating with database.</param>
    public class DeleteUserHandler(IUserReadOnlyRepository userRepository, IRoomRepository roomRepository)
        : IRequestHandler<DeleteUserCommand, Result<Unit, ValidationResult>>
    {
        /// <inheritdoc/>
        public async Task<Result<Unit, ValidationResult>> Handle(DeleteUserCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Get admin user by userCode
            var adminUserResult = await userRepository.GetByCodeAsync(request.UserCode, cancellationToken,
                includeRoom: true, includeWishes: false);
            
            if (adminUserResult.IsFailure)
            {
                return Result.Failure<Unit, ValidationResult>(new NotFoundError([
                    new ValidationFailure("userCode", "User with such code not found")
                ]));
            }

            var adminUser = adminUserResult.Value;

            // 2. Check if user is admin
            if (!adminUser.IsAdmin)
            {
                return Result.Failure<Unit, ValidationResult>(new ForbiddenError([
                    new ValidationFailure("userCode", "Only admin can delete users from the room")
                ]));
            }

            // 3. Get user to delete by id
            var userToDeleteResult = await userRepository.GetByIdAsync(request.UserId, cancellationToken,
                includeRoom: false, includeWishes: false);
            
            if (userToDeleteResult.IsFailure)
            {
                return Result.Failure<Unit, ValidationResult>(new NotFoundError([
                    new ValidationFailure("id", "User with such id not found")
                ]));
            }

            var userToDelete = userToDeleteResult.Value;

            // 4. Check if both users belong to the same room
            if (adminUser.RoomId != userToDelete.RoomId)
            {
                return Result.Failure<Unit, ValidationResult>(new ForbiddenError([
                    new ValidationFailure("id", "User with userCode and user with id belong to different rooms")
                ]));
            }

            // 5. Check if admin is not trying to delete himself
            if (adminUser.Id == userToDelete.Id)
            {
                return Result.Failure<Unit, ValidationResult>(new BadRequestError([
                    new ValidationFailure("id", "Admin cannot delete himself")
                ]));
            }

            // 6. Get room to check if it's closed
            var roomResult = await roomRepository.GetByUserCodeAsync(request.UserCode, cancellationToken);
            
            if (roomResult.IsFailure)
            {
                return roomResult.ConvertFailure<Unit>();
            }

            var room = roomResult.Value;

            // 7. Check if room is not closed
            if (room.ClosedOn != null)
            {
                return Result.Failure<Unit, ValidationResult>(new BadRequestError([
                    new ValidationFailure("room", "Cannot delete user from a closed room")
                ]));
            }

            // 8. Remove user from room
            var userToRemove = room.Users.FirstOrDefault(u => u.Id == request.UserId);
            if (userToRemove != null)
            {
                room.Users.Remove(userToRemove);
            }

            // 9. Update room in database
            var updateResult = await roomRepository.UpdateAsync(room, cancellationToken);
            
            if (updateResult.IsFailure)
            {
                return Result.Failure<Unit, ValidationResult>(new BadRequestError([
                    new ValidationFailure(string.Empty, updateResult.Error)
                ]));
            }

            return Unit.Value;
        }
    }
}
