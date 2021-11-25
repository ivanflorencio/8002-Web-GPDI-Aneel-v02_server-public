﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using PeD.Authorizations;
using PeD.Core.ApiModels;
using PeD.Core.Extensions;
using PeD.Core.Models;
using PeD.Core.Requests.Users;
using PeD.Services;

namespace PeD.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize("Bearer")]
    [Authorize(Policy = Policies.IsUserPeD)]
    public class UsersController : ControllerBase
    {
        private UserService _service;
        private IMapper _mapper;
        private IConfiguration Configuration;
        private string StoragePath;
        private string AvatarPath;
        private IWebHostEnvironment env;


        public UsersController(UserService service, IMapper mapper, IConfiguration configuration,
            IWebHostEnvironment env)
        {
            _service = service;
            this._mapper = mapper;
            Configuration = configuration;
            this.env = env;
            StoragePath = Configuration.GetValue<string>("StoragePath");
            AvatarPath = Path.Combine(StoragePath, "avatar");
        }

        [HttpGet]
        public IEnumerable<ApplicationUserDto> Get()
        {
            //return _service.ListarTodos();
            return _mapper.Map<IEnumerable<ApplicationUserDto>>(_service.ListarTodos());
        }


        [HttpGet("{id}")]
        public ActionResult<ApplicationUserDto> Get(string id, [FromServices] UserManager<ApplicationUser> userManager)
        {
            var User = _service.Obter(id);

            if (User != null)
            {
                User.Roles = userManager.GetRolesAsync(User).Result.ToList();
                return _mapper.Map<ApplicationUserDto>(User);
            }

            return NotFound();
        }

        [Authorize(Roles = Roles.Administrador)]
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] NewUserRequest user)
        {
            try
            {
                await _service.Incluir(_mapper.Map<ApplicationUser>(user));
            }
            catch (Exception e)
            {
                return Problem(e.Message, statusCode: StatusCodes.Status400BadRequest);
            }

            return Ok();
        }

        [HttpPost("{userId}/Avatar")]
        [RequestSizeLimit(5242880)] // 5MB
        public async Task<IActionResult> UploadAvatar(IFormFile file, [FromRoute] string userId)
        {
            await _service.UpdateAvatar(userId, file);
            return Ok();
        }

        [HttpDelete("{userId}/Avatar")]
        public async Task<IActionResult> RemoveAvatar([FromRoute] string userId)
        {
            await _service.UpdateAvatar(userId, null);
            return Ok();
        }

        [AllowAnonymous]
        [HttpGet("/avatar/{userId}.jpg")]
        public FileResult GetAvatar(string userId)
        {
            var filename = Path.Combine(AvatarPath, $"{userId}.jpg");
            if (!System.IO.File.Exists(filename))
            {
                filename = Path.Combine(env.ContentRootPath, "./wwwroot/Assets/default_avatar.jpg");
            }

            return PhysicalFile(filename, "image/jpg");
        }


        [HttpPut]
        public ActionResult<Resultado> Edit([FromBody] EditUserRequest user)
        {
            if (this.IsAdmin() && user.Id != this.UserId())
            {
                _service.Atualizar(_mapper.Map<ApplicationUser>(user));
                return Ok();
            }

            return Forbid();
        }


        [HttpDelete("{id}")]
        public ActionResult<Resultado> Delete(string id)
        {
            if (this.IsAdmin())
                return _service.Excluir(id);
            return Forbid();
        }

        [HttpGet("Role/{role}")]
        public ActionResult<List<ApplicationUserDto>> GetByRole(string role,
            [FromServices] UserManager<ApplicationUser> userManager)
        {
            var users = userManager.GetUsersInRoleAsync(role).Result.ToList();
            return _mapper.Map<List<ApplicationUserDto>>(users);
        }
    }
}