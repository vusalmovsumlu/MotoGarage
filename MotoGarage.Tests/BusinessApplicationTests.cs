using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MotoGarage.Application.DTOs.Auth;
using MotoGarage.Application.DTOs.Media;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Administration;
using MotoGarage.Infrastructure.Authentication;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Tests;

public class BusinessApplicationTests
{
    static RegisterDto Registration(UserRole role = UserRole.Mechanic) => new()
    { FullName = "Test Business", Email = "business@test.example", Password = "pass123", Role = role };

    static List<RegistrationPhoto> Photos(int count = 3) => Enumerable.Range(0, count).Select(index =>
        new RegistrationPhoto(new MemoryStream(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, (byte)index }), $"photo-{index}.png")).ToList();

    [Theory]
    [InlineData(UserRole.Mechanic)]
    [InlineData(UserRole.Store)]
    public async Task ThreeOwnedCloudinaryPhotosAndOtpAreRequiredBeforeAdminReview(UserRole role)
    {
        using var context = TestSupport.Context();
        using var users = TestSupport.Users(context);
        var email = new TestEmail();
        var images = new RegistrationImages(context);
        var auth = TestSupport.Auth(context, users, email, images);
        await Assert.ThrowsAsync<DomainException>(() => auth.RegisterAsync(Registration(role)));
        Assert.Empty(context.Users);
        await auth.RegisterAsync(Registration(role), Photos());
        var user = await users.FindByEmailAsync(Registration().Email);
        Assert.Equal(BusinessApprovalStatus.Pending, user!.BusinessApprovalStatus);
        Assert.Equal(role, user.Role);
        Assert.Equal(3, images.Uploaded.Count);
        Assert.All(await context.ImageAssets.ToListAsync(), asset => Assert.Equal(user.Id, asset.UserId));
        var application = await context.BusinessApplications.Include(x => x.Photos).SingleAsync();
        Assert.Equal(new[] { 1, 2, 3 }, application.Photos.OrderBy(x => x.Position).Select(x => x.Position));
        Assert.All(application.Photos, x => Assert.StartsWith("https://res.cloudinary.com/", x.Url));
        var service = new BusinessApplicationService(context);
        Assert.Empty(await service.GetApplicationsAsync(BusinessApprovalStatus.Pending));
        await auth.VerifyEmailAsync(new() { Email = user.Email!, Code = email.LastCode! });
        Assert.Single(await service.GetApplicationsAsync(BusinessApprovalStatus.Pending));
        Assert.NotNull(await service.GetMineAsync(user.Id));
        Assert.Null(await service.GetMineAsync(user.Id + 100));
        Assert.Equal(BusinessApprovalStatus.Pending, (await users.FindByIdAsync(user.Id.ToString()))!.BusinessApprovalStatus);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task WrongPhotoCountNeverCreatesAccountOrUploads(int count)
    {
        using var context = TestSupport.Context();
        using var users = TestSupport.Users(context);
        var images = new RegistrationImages(context);
        await Assert.ThrowsAsync<DomainException>(() => TestSupport.Auth(context, users, images: images).RegisterAsync(Registration(), Photos(count)));
        Assert.Empty(context.Users);
        Assert.Empty(images.Uploaded);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("signature")]
    [InlineData("extension")]
    [InlineData("oversize")]
    public async Task InvalidPhotosAreRejectedBeforeAnyUpload(string invalid)
    {
        using var context = TestSupport.Context();
        using var users = TestSupport.Users(context);
        var images = new RegistrationImages(context);
        var photos = Photos();
        photos[2] = invalid switch
        {
            "duplicate" => new(new MemoryStream(((MemoryStream)photos[0].Stream).ToArray()), "renamed.png"),
            "signature" => new(new MemoryStream("not an image"u8.ToArray()), "fake.png"),
            "extension" => new(photos[2].Stream, "photo.exe"),
            _ => new(new MemoryStream(new byte[5 * 1024 * 1024 + 1]), "large.png")
        };
        await Assert.ThrowsAsync<DomainException>(() => TestSupport.Auth(context, users, images: images).RegisterAsync(Registration(), photos));
        Assert.Empty(context.Users);
        Assert.Empty(images.Uploaded);
    }

    [Fact]
    public async Task PartialCloudinaryFailureCompensatesAccountAndAlreadyUploadedImages()
    {
        using var context = TestSupport.Context();
        using var users = TestSupport.Users(context);
        var images = new RegistrationImages(context) { FailAt = 2 };
        await Assert.ThrowsAsync<IOException>(() => TestSupport.Auth(context, users, images: images).RegisterAsync(Registration(), Photos()));
        Assert.Single(images.Uploaded);
        Assert.Equal(images.Uploaded, images.Deleted);
        Assert.Empty(context.Users);
        Assert.Empty(context.ImageAssets);
        Assert.Empty(context.BusinessApplications);
    }

    static async Task<BusinessApplication> SeedApplication(AppDbContext context)
    {
        context.Users.Add(new User { Id = 100, FullName = "Admin", Role = UserRole.Admin, EmailConfirmed = true });
        var application = new BusinessApplication
        {
            User = new User { FullName = "Applicant", Email = "applicant@test.example", Role = UserRole.Store, EmailConfirmed = true, BusinessApprovalStatus = BusinessApprovalStatus.Pending },
            CreatedAt = DateTime.UtcNow,
            Photos = Enumerable.Range(1, 3).Select(index => new BusinessApplicationPhoto
                { Position = index, PublicId = $"motogarage/{index}", Url = $"https://res.cloudinary.com/test/{index}.png" }).ToList()
        };
        context.BusinessApplications.Add(application);
        await context.SaveChangesAsync();
        return application;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReviewCommitsDecisionAuditLiveAuthorizationAndNotificationOnce(bool approve)
    {
        using var context = TestSupport.Context();
        var application = await SeedApplication(context);
        var service = new BusinessApplicationService(context);
        await service.ReviewAsync(application.Id, 100, approve, "  Şəkillər qarajınızı göstərmir.  ");
        Assert.Equal(approve ? BusinessApprovalStatus.Approved : BusinessApprovalStatus.Rejected, application.Status);
        Assert.Equal(application.Status, application.User.BusinessApprovalStatus);
        Assert.Equal(100, application.ReviewedById);
        Assert.NotNull(application.ReviewedAt);
        Assert.Equal(approve ? null : "Şəkillər qarajınızı göstərmir.", application.DecisionComment);
        var notification = await context.Notifications.SingleAsync();
        Assert.Equal(application.UserId, notification.UserId);
        if (!approve) Assert.Equal(application.DecisionComment, notification.Message);
        await Assert.ThrowsAsync<ConflictException>(() => service.ReviewAsync(application.Id, 100, !approve, "Başqa səbəb"));
        Assert.Single(context.Notifications);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeclineRequiresNonWhitespaceComment(string? comment)
    {
        using var context = TestSupport.Context();
        var application = await SeedApplication(context);
        await Assert.ThrowsAsync<DomainException>(() => new BusinessApplicationService(context).ReviewAsync(application.Id, 100, false, comment));
        Assert.Equal(BusinessApprovalStatus.Pending, application.Status);
        Assert.Empty(context.Notifications);
    }

    [Fact]
    public async Task UnauthorizedBlockedOrIncompleteApplicationsCannotBeReviewed()
    {
        using var context = TestSupport.Context();
        var application = await SeedApplication(context);
        var service = new BusinessApplicationService(context);
        await Assert.ThrowsAsync<ForbiddenException>(() => service.ReviewAsync(application.Id, application.UserId, true, null));
        await Assert.ThrowsAsync<DomainException>(() => service.ReviewAsync(application.Id, 100, false, new string('x', 1001)));
        application.User.IsAdminBlocked = true;
        await context.SaveChangesAsync();
        await Assert.ThrowsAsync<DomainException>(() => service.ReviewAsync(application.Id, 100, true, null));
        application.User.IsAdminBlocked = false;
        application.Photos.Remove(application.Photos.First());
        await context.SaveChangesAsync();
        await Assert.ThrowsAsync<DomainException>(() => service.ReviewAsync(application.Id, 100, true, null));
        Assert.Empty(context.Notifications);
    }

    [Theory]
    [InlineData(UserRole.Mechanic, BusinessApprovalStatus.Pending)]
    [InlineData(UserRole.Store, BusinessApprovalStatus.Pending)]
    [InlineData(UserRole.Mechanic, BusinessApprovalStatus.Rejected)]
    [InlineData(UserRole.Store, BusinessApprovalStatus.Rejected)]
    public void OlderApprovedJwtCannotBypassCurrentDatabaseDecision(UserRole role, BusinessApprovalStatus status)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        { new Claim(ClaimTypes.Role, role.ToString()), new Claim("businessApprovalStatus", "Approved"), new Claim(ClaimTypes.NameIdentifier, "1") }, "Bearer"));
        BusinessApprovalAuthorization.Apply(principal, new User { Role = role, BusinessApprovalStatus = status });
        Assert.False(principal.IsInRole("Mechanic"));
        Assert.False(principal.IsInRole("Store"));
        Assert.True(principal.IsInRole("User"));
        Assert.Equal("1", principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    [Fact]
    public void ApprovalGrantsBusinessRoleWithExistingPendingToken()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Store"), new Claim("businessApprovalStatus", "Pending") }, "Bearer"));
        BusinessApprovalAuthorization.Apply(principal, new User { Role = UserRole.Store, BusinessApprovalStatus = BusinessApprovalStatus.Approved });
        Assert.True(principal.IsInRole("Store"));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = new string('k', 64) }).Build();
        var token = new JwtSecurityTokenHandler().ReadJwtToken(new TokenService(configuration).CreateAccessToken(new User
            { Id = 1, FullName = "Applicant", Role = UserRole.Store, BusinessApprovalStatus = BusinessApprovalStatus.Pending }));
        Assert.Equal("Pending", token.Claims.Single(x => x.Type == "businessApprovalStatus").Value);
    }

    sealed class RegistrationImages(AppDbContext context) : IImageService
    {
        public int FailAt { get; init; }
        public List<string> Uploaded { get; } = [];
        public List<string> Deleted { get; } = [];
        public async Task<UploadResultDto> UploadAsync(Stream stream, string fileName, int userId)
        {
            if (Uploaded.Count + 1 == FailAt) throw new IOException("Simulated Cloudinary failure");
            Assert.Equal(0, stream.Position);
            var publicId = $"motogarage/application-{Uploaded.Count}";
            var url = $"https://res.cloudinary.com/test/image/upload/{publicId}.png";
            context.ImageAssets.Add(new ImageAsset { PublicId = publicId, Url = url, UserId = userId, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();
            Uploaded.Add(publicId);
            return new() { PublicId = publicId, Url = url };
        }
        public Task DeleteAsync(string publicId) { Deleted.Add(publicId); return Task.CompletedTask; }
        public Task ValidateOwnershipAsync(int userId, string? publicId, string? url) => Task.CompletedTask;
        public Task DeleteUnusedAsync(string? publicId, int userId) => Task.CompletedTask;
    }
}
