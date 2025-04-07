//using Microsoft.AspNetCore.Identity;
//using Microsoft.IdentityModel.Tokens;
//using System.Collections.Generic;
//using System.Security.Claims;
//using System.Text;
//using System.Threading.Tasks;
//using System;
//using Volo.Abp.Application.Services;
//using Volo.Abp.Identity;
//using Volo.Abp;
//using IdentityUser = Volo.Abp.Identity.IdentityUser;

//public class AuthenticationAppService : ApplicationService
//{
//    private readonly UserManager<IdentityUser> _userManager;
//    private readonly SignInManager<IdentityUser> _signInManager;
//    private readonly ITokenService _tokenService;
//    private readonly IIdentityRoleRepository _roleRepository;

//    public AuthenticationAppService(
//        UserManager<IdentityUser> userManager,
//        SignInManager<IdentityUser> signInManager,
//        ITokenService tokenService,
//        IIdentityRoleRepository roleRepository)
//    {
//        _userManager = userManager;
//        _signInManager = signInManager;
//        _tokenService = tokenService;
//        _roleRepository = roleRepository;
//    }

//    public async Task<string> RegisterAsync(string userName, string password, string role)
//    {
//        var user = new IdentityUser { UserName = userName, Email = $"{userName}@example.com" };
//        var result = await _userManager.CreateAsync(user, password);

//        if (!result.Succeeded)
//        {
//            throw new UserFriendlyException("User registration failed");
//        }

//        var userRole = await _roleRepository.FindByNameAsync(role);
//        if (userRole == null)
//        {
//            throw new UserFriendlyException("Role not found");
//        }

//        await _userManager.AddToRoleAsync(user, role);
//        return await GenerateJwtTokenAsync(user);
//    }

//    public async Task<string> LoginAsync(string userName, string password)
//    {
//        var user = await _userManager.FindByNameAsync(userName);
//        if (user == null)
//        {
//            throw new UserFriendlyException("User not found");
//        }

//        var result = await _signInManager.PasswordSignInAsync(user, password, false, false);
//        if (!result.Succeeded)
//        {
//            throw new UserFriendlyException("Login failed");
//        }

//        return await GenerateJwtTokenAsync(user);
//    }

//    private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
//    {
//        var claims = await _userManager.GetClaimsAsync(user);
//        var roles = await _userManager.GetRolesAsync(user);

//        var identityClaims = new List<Claim>
//        {
//            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
//            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
//        };

//        foreach (var role in roles)
//        {
//            identityClaims.Add(new Claim(ClaimTypes.Role, role));
//        }

//        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]));
//        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

//        var token = new JwtSecurityToken(
//            issuer: Configuration["Jwt:Issuer"],
//            audience: Configuration["Jwt:Audience"],
//            claims: identityClaims,
//            expires: DateTime.Now.AddMinutes(Convert.ToInt32(Configuration["Jwt:ExpireMinutes"])),
//            signingCredentials: creds);

//        return new JwtSecurityTokenHandler().WriteToken(token);
//    }
//}
