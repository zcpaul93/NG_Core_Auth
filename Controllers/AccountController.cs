using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using NG_Core_Auth.Models;
using System.Threading.Tasks;
using System;
using NG_Core_Auth.Helpers;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace NG_Core_Auth.Controllers
{
    [Route("api/[controller]")]
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly AppSettings _appSettings;

        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IOptions<AppSettings> appSettings)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _appSettings = appSettings.Value;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel formData)
        {
             List<string> errorList = new  List<string>();

             var user = new IdentityUser
             {
                 Email = formData.Email,
                 UserName = formData.UserName,
                 SecurityStamp = Guid.NewGuid().ToString()
             };

             var result = await _userManager.CreateAsync(user, formData.Password);

             if(result.Succeeded)
             {
                 await _userManager.AddToRoleAsync(user, "Customer");
                 return Ok(new 
                 {
                     userName = user.UserName, 
                     email = user.Email, 
                     status = 1, 
                     message = "Registration Succesful!"
                 });
             }
             else
             {
                 foreach(var error in result.Errors)
                 {
                     ModelState.AddModelError("",error.Description);
                     errorList.Add(error.Description);
                 }
             }

             return BadRequest(new JsonResult(errorList));
        }

        [HttpPost("[action]")]
         public async Task<IActionResult> Login([FromBody] LoginViewModel formData)
         {
             var user = await _userManager.FindByNameAsync(formData.UserName);
             var roles = await _userManager.GetRolesAsync(user);
             var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_appSettings.Secret));
             double tokenExpiryTime = Convert.ToDouble(_appSettings.ExpireTime);

             if(user != null && await _userManager.CheckPasswordAsync(user, formData.Password))
             {
                 //Confirmation of email
                 var tokenHandler = new JwtSecurityTokenHandler();
                 var tokenDescriptor = new SecurityTokenDescriptor
                 {
                     Subject = new ClaimsIdentity(new Claim[]
                     {
                        new Claim(JwtRegisteredClaimNames.Sub, formData.UserName),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Role, roles.FirstOrDefault()),
                        new Claim("LoggedOn", DateTime.Now.ToString())
                     }),

                     SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
                     Issuer = _appSettings.Site,
                     Audience = _appSettings.Audience,
                     Expires = DateTime.UtcNow.AddMinutes(tokenExpiryTime)
                 }; 

                 //Generate Token
                 var token = tokenHandler.CreateToken(tokenDescriptor);

                 return Ok(new 
                 {
                     token = tokenHandler.WriteToken(token),
                     expiration = token.ValidTo,
                     username = user.UserName,
                     userRole = roles.FirstOrDefault()
                 });
             }

            //return error
            ModelState.AddModelError("","Username/Password was not Found");
            return Unauthorized();

         }
    }
}