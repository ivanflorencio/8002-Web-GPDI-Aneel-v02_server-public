﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using PeD.Core.ApiModels;
using PeD.Core.Models;
using PeD.Services;

namespace PeD.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize("Bearer")]
    public class UsersController : ControllerBase
    {
        private UserService _service;
        private IMapper mapper;

        public UsersController(UserService service, IMapper mapper)
        {
            _service = service;
            this.mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet("routes")]
        public IActionResult GetRoutes([FromServices] IActionDescriptorCollectionProvider _provider)
        {
            var routes = _provider.ActionDescriptors.Items.Select(x => new
            {
                Action = x.RouteValues["Action"],
                Controller = x.RouteValues["Controller"],
                Name = x.AttributeRouteInfo.Name,
                Template = x.AttributeRouteInfo.Template
            }).OrderBy(i => i.Template).ToList();
            return Ok(routes);
        }

        [AllowAnonymous]
        [HttpGet("fixusers")]
        public ActionResult FixRoles([FromServices] UserManager<ApplicationUser> userManager)
        {
            var users = _service.ListarTodos().ToList();
            users.Where(u => !string.IsNullOrWhiteSpace(u.Role)).ToList().ForEach(user =>
            {
                userManager.AddToRoleAsync(user, user.Role).Wait();
            });

            return Ok();
        }

        [HttpGet]
        public IEnumerable<ApplicationUserDto> Get()
        {
            //return _service.ListarTodos();
            return mapper.Map<IEnumerable<ApplicationUserDto>>(_service.ListarTodos());
        }

        [HttpGet("{id}")]
        public ActionResult<ApplicationUserDto> Get(string id, [FromServices] UserManager<ApplicationUser> userManager)
        {
            var User = _service.Obter(id);
            User.Roles = userManager.GetRolesAsync(User).Result.ToList();

            if (User != null)
            {
                return mapper.Map<ApplicationUserDto>(User);
            }
            else
                return NotFound();
        }

        [AllowAnonymous]
        [HttpGet("{id}/avatar")]
        [ResponseCache(Duration = 60)]
        public FileResult Download(string id)
        {
            byte[] image;
            var user = _service.Obter(id);

            if (user == null || user.FotoPerfil == null || user.FotoPerfil.File.Length < 1)
            {
                image = System.IO.File.ReadAllBytes("wwwroot/Assets/default_avatar.jpg");
            }
            else
            {
                image = user.FotoPerfil.File;
            }

            return File(image, System.Net.Mime.MediaTypeNames.Image.Jpeg);
        }

        [HttpPost]
        public ActionResult<Resultado> Post([FromBody] ApplicationUser User)
        {
            if (this.isAdmin())
                return _service.Incluir(User);
            return Forbid();
        }

        [HttpPut]
        public ActionResult<Resultado> Edit([FromBody] ApplicationUser User)
        {
            if (this.isAdmin())
                return _service.Atualizar(User);
            return Forbid();
        }

        [HttpGet("me")]
        public ActionResult<ApplicationUserDto> GetMe()
        {
            return mapper.Map<ApplicationUserDto>(_service.Obter(this.userId()));
        }

        [HttpPut("me")]
        public ActionResult<Resultado> EditMe([FromBody] ApplicationUser _user)
        {
            var me = _service.Obter(this.userId());
            _user.Id = this.userId();
            _user.Email = me.Email;
            _user.Role = me.Role;
            return _service.Atualizar(_user);
        }

        [HttpDelete("{id}")]
        public ActionResult<Resultado> Delete(string id)
        {
            if (this.isAdmin())
                return _service.Excluir(id);
            return Forbid();
        }

        [HttpGet("Role/{role}")]
        public ActionResult<List<ApplicationUserDto>> GetByRole(string role,
            [FromServices] UserManager<ApplicationUser> userManager)
        {
            var users = userManager.GetUsersInRoleAsync(role).Result.ToList();
            return mapper.Map<List<ApplicationUserDto>>(users);
        }
    }
}