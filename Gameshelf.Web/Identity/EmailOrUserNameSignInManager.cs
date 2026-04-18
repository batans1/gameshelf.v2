using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GameShelf.Web.Identity;

/// <summary>
/// Default Identity UI passes the "Email" field into <see cref="SignInManager{TUser}.PasswordSignInAsync(string, string, bool, bool)"/>,
/// which resolves the user by <b>username</b>. After a user changes their username, email login would fail unless we also resolve by email.
/// </summary>
public sealed class EmailOrUserNameSignInManager : SignInManager<IdentityUser>
{
    public EmailOrUserNameSignInManager(
        UserManager<IdentityUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<IdentityUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<IdentityUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<IdentityUser> userConfirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, userConfirmation)
    {
    }

    public override async Task<SignInResult> PasswordSignInAsync(
        string userNameOrEmail,
        string password,
        bool isPersistent,
        bool lockoutOnFailure)
    {
        var user = await UserManager.FindByNameAsync(userNameOrEmail);
        if (user == null && userNameOrEmail.Contains('@', StringComparison.Ordinal))
            user = await UserManager.FindByEmailAsync(userNameOrEmail);

        if (user == null)
            return SignInResult.Failed;

        return await PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
    }
}
