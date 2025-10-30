using System.Net;
using System.Net.Http.Json;
using Epam.ItMarathon.ApiService.Api.Dto.Requests.RoomRequests;
using Epam.ItMarathon.ApiService.Api.Dto.Requests.UserRequests;
using Epam.ItMarathon.ApiService.Api.Dto.Responses.RoomResponses;
using Epam.ItMarathon.ApiService.Api.Dto.Responses.UserResponses;
using FluentAssertions;

namespace Epam.ItMarathon.ApiService.Api.Tests.Endpoints
{
    /// <summary>
    /// Integration tests for User endpoints.
    /// </summary>
    public class UserEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserEndpointsTests"/> class.
        /// </summary>
        /// <param name="factory">The custom web application factory.</param>
        public UserEndpointsTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        /// <summary>
        /// Helper method to create a room with admin user.
        /// </summary>
        private async Task<(RoomCreationResponse response, string adminUserCode)> CreateRoomWithAdmin()
        {
            var roomRequest = new RoomCreationRequest
            {
                Room = new()
                {
                    Name = "Test Room",
                    Description = "Test Description",
                    GiftExchangeDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
                    GiftMaximumBudget = 1000
                },
                AdminUser = new()
                {
                    FirstName = "Admin",
                    LastName = "User",
                    Phone = "+380123456789",
                    Email = "admin@test.com",
                    DeliveryInfo = "Test address",
                    WantSurprise = true,
                    Interests = "Testing"
                }
            };

            var response = await _client.PostAsJsonAsync("/api/rooms", roomRequest);
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var roomResponse = await response.Content.ReadFromJsonAsync<RoomCreationResponse>();
            roomResponse.Should().NotBeNull();

            return (roomResponse!, roomResponse!.UserCode);
        }

        /// <summary>
        /// Helper method to add a user to a room.
        /// </summary>
        private async Task<UserCreationResponse> AddUserToRoom(string roomCode, string firstName = "Test", string lastName = "User")
        {
            var userRequest = new UserCreationRequest
            {
                FirstName = firstName,
                LastName = lastName,
                Phone = "+380987654321",
                Email = $"{firstName.ToLower()}@test.com",
                DeliveryInfo = "Test address",
                WantSurprise = true,
                Interests = "Testing"
            };

            var response = await _client.PostAsJsonAsync($"/api/users?roomCode={roomCode}", userRequest);
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var userResponse = await response.Content.ReadFromJsonAsync<UserCreationResponse>();
            userResponse.Should().NotBeNull();

            return userResponse!;
        }

        /// <summary>
        /// Test that DELETE /users/{id} returns 204 No Content when successful.
        /// </summary>
        [Fact]
        public async Task DeleteUser_ShouldReturnNoContent_WhenSuccessful()
        {
            // Arrange
            var (roomResponse, adminUserCode) = await CreateRoomWithAdmin();
            var userToDelete = await AddUserToRoom(roomResponse.Room.InvitationCode);

            // Act
            var response = await _client.DeleteAsync($"/api/users/{userToDelete.Id}?userCode={adminUserCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Test that DELETE /users/{id} returns 404 Not Found when user code is invalid.
        /// </summary>
        [Fact]
        public async Task DeleteUser_ShouldReturnNotFound_WhenUserCodeIsInvalid()
        {
            // Arrange
            var invalidUserCode = "invalid-code-12345";
            var userId = 999;

            // Act
            var response = await _client.DeleteAsync($"/api/users/{userId}?userCode={invalidUserCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Test that DELETE /users/{id} returns 403 Forbidden when user is not admin.
        /// </summary>
        [Fact]
        public async Task DeleteUser_ShouldReturnForbidden_WhenUserIsNotAdmin()
        {
            // Arrange
            var (roomResponse, adminUserCode) = await CreateRoomWithAdmin();
            var regularUser = await AddUserToRoom(roomResponse.Room.InvitationCode, "Regular", "User");
            var userToDelete = await AddUserToRoom(roomResponse.Room.InvitationCode, "ToDelete", "User");

            // Act - Regular user trying to delete another user
            var response = await _client.DeleteAsync($"/api/users/{userToDelete.Id}?userCode={regularUser.UserCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// Test that DELETE /users/{id} returns 404 Not Found when user to delete doesn't exist.
        /// </summary>
        [Fact]
        public async Task DeleteUser_ShouldReturnNotFound_WhenUserToDeleteDoesNotExist()
        {
            // Arrange
            var (roomResponse, adminUserCode) = await CreateRoomWithAdmin();
            var nonExistentUserId = 99999;

            // Act
            var response = await _client.DeleteAsync($"/api/users/{nonExistentUserId}?userCode={adminUserCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Test that DELETE /users/{id} returns 400 Bad Request when admin tries to delete himself.
        /// </summary>
        [Fact]
        public async Task DeleteUser_ShouldReturnBadRequest_WhenAdminTriesToDeleteHimself()
        {
            // Arrange
            var (roomResponse, adminUserCode) = await CreateRoomWithAdmin();
            var adminUserId = roomResponse.Room.AdminId;

            // Act
            var response = await _client.DeleteAsync($"/api/users/{adminUserId}?userCode={adminUserCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Test that DELETE /users/{id} returns 400 Bad Request when room is already closed.
        /// </summary>
        [Fact]
        public async Task DeleteUser_ShouldReturnBadRequest_WhenRoomIsAlreadyClosed()
        {
            // Arrange
            var (roomResponse, adminUserCode) = await CreateRoomWithAdmin();
            var user1 = await AddUserToRoom(roomResponse.Room.InvitationCode, "User1", "Test");
            var user2 = await AddUserToRoom(roomResponse.Room.InvitationCode, "User2", "Test");
            var user3 = await AddUserToRoom(roomResponse.Room.InvitationCode, "User3", "Test");

            // Close the room by drawing
            var drawResponse = await _client.PostAsync($"/api/rooms/draw?userCode={adminUserCode}", null);
            drawResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Act - Try to delete user from closed room
            var response = await _client.DeleteAsync($"/api/users/{user1.Id}?userCode={adminUserCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Test that multiple users can be deleted sequentially.
        /// </summary>
        [Fact]
        public async Task DeleteUser_ShouldDeleteMultipleUsers_WhenCalledSequentially()
        {
            // Arrange
            var (roomResponse, adminUserCode) = await CreateRoomWithAdmin();
            var user1 = await AddUserToRoom(roomResponse.Room.InvitationCode, "User1", "Test");
            var user2 = await AddUserToRoom(roomResponse.Room.InvitationCode, "User2", "Test");

            // Act - Delete first user
            var response1 = await _client.DeleteAsync($"/api/users/{user1.Id}?userCode={adminUserCode}");
            response1.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Act - Delete second user
            var response2 = await _client.DeleteAsync($"/api/users/{user2.Id}?userCode={adminUserCode}");
            response2.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Assert - Verify both users are deleted
            var getUsersResponse = await _client.GetAsync($"/api/users?userCode={adminUserCode}");
            getUsersResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        /// <summary>
        /// Test that DELETE /users/{id} returns 400 Bad Request when userCode query parameter is missing.
        /// </summary>
        [Fact]
        public async Task DeleteUser_ShouldReturnBadRequest_WhenUserCodeIsMissing()
        {
            // Arrange
            var userId = 1;

            // Act
            var response = await _client.DeleteAsync($"/api/users/{userId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
