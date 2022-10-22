using DatingApp.Data;
using DatingApp.DTOs;
using DatingApp.Entities;
using DatingApp.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DatingApp.Controllers
{
	
	public class AccountController : BaseApiController
	{
		private readonly DataContext _context;
		private readonly ITokenService _tokenService;
		public AccountController(DataContext context, ITokenService tokenService)
		{
			_context = context;
			_tokenService = tokenService;
		}

		[HttpPost("register")]
		public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
		{
			if(await IsUserExists(registerDto.Username)) return BadRequest("Username is taken");

			using var hmac = new HMACSHA512();
			var user = new AppUser { UserName = registerDto.Username.ToLower(), PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)), PasswordSalt=hmac.Key};
			_context.Users.Add(user);
			await _context.SaveChangesAsync();
			return new UserDto
			{
				Username = user.UserName,
				Token = _tokenService.GenerateToken(user)
			};
		}

		private async Task<bool> IsUserExists(string username)
		{
			return await _context.Users.AnyAsync(x=> x.UserName== username.ToLower());
		}

		[HttpPost("login")]
		public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
		{
			var user= await _context.Users
				.SingleOrDefaultAsync(x=> x.UserName == loginDto.Username.ToLower());
			if (user == null)
				return Unauthorized("Invalid Username");
			var hmac = new HMACSHA512(user.PasswordSalt);
			var computedhash=hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
			for (int i = 0; i < computedhash.Length; i++)
			{
				if (computedhash[i] != user.PasswordHash[i])
					return Unauthorized("Invalid Password");
			}
			return new UserDto
			{
				Username = user.UserName,
				Token = _tokenService.GenerateToken(user)
			};


		}
	}
}
