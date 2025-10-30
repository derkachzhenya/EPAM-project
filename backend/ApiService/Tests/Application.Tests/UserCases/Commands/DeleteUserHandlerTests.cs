using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Commands;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Handlers;
using Epam.ItMarathon.ApiService.Domain.Abstract;
using Epam.ItMarathon.ApiService.Domain.Shared.ValidationErrors;
using FluentAssertions;
using FluentValidation.Results;
using MediatR;
using NSubstitute;

namespace Epam.ItMarathon.ApiService.Application.Tests.UserCases.Commands
{
    /// <summary>
    /// Unit tests for the <see cref="DeleteUserHandler"/> class.
    /// </summary>
    public class DeleteUserHandlerTests
    {
        private readonly IUserReadOnlyRepository _userReadOnlyRepositoryMock;
        private readonly IRoomRepository _roomRepositoryMock;
        private readonly DeleteUserHandler _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteUserHandlerTests"/> class with mocked dependencies.
        /// </summary>
        public DeleteUserHandlerTests()
        {
            _userReadOnlyRepositoryMock = Substitute.For<IUserReadOnlyRepository>();
            _roomRepositoryMock = Substitute.For<IRoomRepository>();
            _handler = new DeleteUserHandler(_userReadOnlyRepositoryMock, _roomRepositoryMock);
        }

        /// <summary>
        /// Tests that the handler returns a NotFoundError when the admin user by provided UserCode is not found.
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnNotFoundError_WhenAdminUserNotFound()
        {
            // Arrange
            var command = new DeleteUserCommand("invalid-code", 1);
            _userReadOnlyRepositoryMock
                .GetByCodeAsync(command.UserCode, Arg.Any<CancellationToken>(), includeRoom: true, includeWishes: false)
                .Returns(Result.Failure<Domain.Entities.User.User, ValidationResult>(
                    new NotFoundError([new ValidationFailure("userCode", "User with such code not found")])));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            result.Error.Errors.Should().Contain(error => error.PropertyName.Equals("userCode"));
        }

        /// <summary>
        /// Tests that the handler returns a ForbiddenError when the user is not an admin.
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnForbiddenError_WhenUserIsNotAdmin()
        {
            // Arrange
            var nonAdminUser = DataFakers.ValidUserBuilder
                .WithAuthCode("user-code")
                .WithIsAdmin(false)
                .WithRoomId(1)
                .Build();
            var command = new DeleteUserCommand("user-code", 2);

            _userReadOnlyRepositoryMock
                .GetByCodeAsync(command.UserCode, Arg.Any<CancellationToken>(), includeRoom: true, includeWishes: false)
                .Returns(nonAdminUser);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
            result.Error.Errors.Should().Contain(error => 
                error.PropertyName.Equals("userCode") && 
                error.ErrorMessage.Contains("Only admin can delete users"));
        }

        /// <summary>
        /// Tests that the handler returns a NotFoundError when the user to delete is not found.
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnNotFoundError_WhenUserToDeleteNotFound()
        {
            // Arrange
            var adminUser = DataFakers.ValidUserBuilder
                .WithAuthCode("admin-code")
                .WithIsAdmin(true)
                .WithRoomId(1)
                .Build();
            var command = new DeleteUserCommand("admin-code", 999);

            _userReadOnlyRepositoryMock
                .GetByCodeAsync(command.UserCode, Arg.Any<CancellationToken>(), includeRoom: true, includeWishes: false)
                .Returns(adminUser);
            
            _userReadOnlyRepositoryMock
                .GetByIdAsync(command.UserId, Arg.Any<CancellationToken>(), includeRoom: false, includeWishes: false)
                .Returns(Result.Failure<Domain.Entities.User.User, ValidationResult>(
                    new NotFoundError([new ValidationFailure("id", "User with such id not found")])));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            result.Error.Errors.Should().Contain(error => error.PropertyName.Equals("id"));
        }

        /// <summary>
        /// Tests that the handler returns a ForbiddenError when users belong to different rooms.
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnForbiddenError_WhenUsersBelongToDifferentRooms()
        {
            // Arrange
            var adminUser = DataFakers.ValidUserBuilder
                .WithAuthCode("admin-code")
                .WithIsAdmin(true)
                .WithRoomId(1)
                .Build();
            
            var userToDelete = DataFakers.ValidUserBuilder
                .WithId(2)
                .WithRoomId(2) // Different room
                .Build();

            var command = new DeleteUserCommand("admin-code", 2);

            _userReadOnlyRepositoryMock
                .GetByCodeAsync(command.UserCode, Arg.Any<CancellationToken>(), includeRoom: true, includeWishes: false)
                .Returns(adminUser);
            
            _userReadOnlyRepositoryMock
                .GetByIdAsync(command.UserId, Arg.Any<CancellationToken>(), includeRoom: false, includeWishes: false)
                .Returns(userToDelete);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
            result.Error.Errors.Should().Contain(error => 
                error.PropertyName.Equals("id") && 
                error.ErrorMessage.Contains("belong to different rooms"));
        }

        /// <summary>
        /// Tests that the handler returns a BadRequestError when admin tries to delete himself.
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnBadRequestError_WhenAdminTriesToDeleteHimself()
        {
            // Arrange
            var adminUser = DataFakers.ValidUserBuilder
                .WithId(1)
                .WithAuthCode("admin-code")
                .WithIsAdmin(true)
                .WithRoomId(1)
                .Build();

            var command = new DeleteUserCommand("admin-code", 1); // Same user

            _userReadOnlyRepositoryMock
                .GetByCodeAsync(command.UserCode, Arg.Any<CancellationToken>(), includeRoom: true, includeWishes: false)
                .Returns(adminUser);
            
            _userReadOnlyRepositoryMock
                .GetByIdAsync(command.UserId, Arg.Any<CancellationToken>(), includeRoom: false, includeWishes: false)
                .Returns(adminUser);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<BadRequestError>();
            result.Error.Errors.Should().Contain(error => 
                error.PropertyName.Equals("id") && 
                error.ErrorMessage.Contains("Admin cannot delete himself"));
        }

        /// <summary>
        /// Tests that the handler returns a BadRequestError when the room is already closed.
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnBadRequestError_WhenRoomIsAlreadyClosed()
        {
            // Arrange
            var adminUser = DataFakers.ValidUserBuilder
                .WithId(1)
                .WithAuthCode("admin-code")
                .WithIsAdmin(true)
                .WithRoomId(1)
                .Build();
            
            var userToDelete = DataFakers.ValidUserBuilder
                .WithId(2)
                .WithRoomId(1)
                .Build();

            var closedRoom = DataFakers.RoomFaker
                .RuleFor(room => room.ClosedOn, faker => faker.Date.Past())
                .RuleFor(room => room.Users, _ => [adminUser, userToDelete])
                .Generate();

            var command = new DeleteUserCommand("admin-code", 2);

            _userReadOnlyRepositoryMock
                .GetByCodeAsync(command.UserCode, Arg.Any<CancellationToken>(), includeRoom: true, includeWishes: false)
                .Returns(adminUser);
            
            _userReadOnlyRepositoryMock
                .GetByIdAsync(command.UserId, Arg.Any<CancellationToken>(), includeRoom: false, includeWishes: false)
                .Returns(userToDelete);
            
            _roomRepositoryMock
                .GetByUserCodeAsync(command.UserCode, Arg.Any<CancellationToken>())
                .Returns(closedRoom);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<BadRequestError>();
            result.Error.Errors.Should().Contain(error => 
                error.PropertyName.Equals("room") && 
                error.ErrorMessage.Contains("Cannot delete user from a closed room"));
        }

        /// <summary>
        /// Tests that the handler successfully deletes a user when all validations pass.
        /// </summary>
        [Fact]
        public async Task Handle_ShouldDeleteUserSuccessfully_WhenAllValidationsPass()
        {
            // Arrange
            var adminUser = DataFakers.ValidUserBuilder
                .WithId(1)
                .WithAuthCode("admin-code")
                .WithIsAdmin(true)
                .WithRoomId(1)
                .Build();
            
            var userToDelete = DataFakers.ValidUserBuilder
                .WithId(2)
                .WithRoomId(1)
                .Build();

            var room = DataFakers.RoomFaker
                .RuleFor(room => room.ClosedOn, _ => null)
                .RuleFor(room => room.Users, _ => [adminUser, userToDelete])
                .Generate();

            var command = new DeleteUserCommand("admin-code", 2);

            _userReadOnlyRepositoryMock
                .GetByCodeAsync(command.UserCode, Arg.Any<CancellationToken>(), includeRoom: true, includeWishes: false)
                .Returns(adminUser);
            
            _userReadOnlyRepositoryMock
                .GetByIdAsync(command.UserId, Arg.Any<CancellationToken>(), includeRoom: false, includeWishes: false)
                .Returns(userToDelete);
            
            _roomRepositoryMock
                .GetByUserCodeAsync(command.UserCode, Arg.Any<CancellationToken>())
                .Returns(room);
            
            _roomRepositoryMock
                .UpdateAsync(Arg.Any<Domain.Aggregate.Room.Room>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            room.Users.Should().NotContain(u => u.Id == userToDelete.Id);
            room.Users.Should().HaveCount(1);
            await _roomRepositoryMock.Received(1).UpdateAsync(Arg.Any<Domain.Aggregate.Room.Room>(), Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// Tests that the handler returns a BadRequestError when database update fails.
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnBadRequestError_WhenDatabaseUpdateFails()
        {
            // Arrange
            var adminUser = DataFakers.ValidUserBuilder
                .WithId(1)
                .WithAuthCode("admin-code")
                .WithIsAdmin(true)
                .WithRoomId(1)
                .Build();
            
            var userToDelete = DataFakers.ValidUserBuilder
                .WithId(2)
                .WithRoomId(1)
                .Build();

            var room = DataFakers.RoomFaker
                .RuleFor(room => room.ClosedOn, _ => null)
                .RuleFor(room => room.Users, _ => [adminUser, userToDelete])
                .Generate();

            var command = new DeleteUserCommand("admin-code", 2);

            _userReadOnlyRepositoryMock
                .GetByCodeAsync(command.UserCode, Arg.Any<CancellationToken>(), includeRoom: true, includeWishes: false)
                .Returns(adminUser);
            
            _userReadOnlyRepositoryMock
                .GetByIdAsync(command.UserId, Arg.Any<CancellationToken>(), includeRoom: false, includeWishes: false)
                .Returns(userToDelete);
            
            _roomRepositoryMock
                .GetByUserCodeAsync(command.UserCode, Arg.Any<CancellationToken>())
                .Returns(room);
            
            _roomRepositoryMock
                .UpdateAsync(Arg.Any<Domain.Aggregate.Room.Room>(), Arg.Any<CancellationToken>())
                .Returns(Result.Failure("Database update failed"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<BadRequestError>();
        }
    }
}
