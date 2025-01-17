﻿using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Net.Http.Headers;
using PeD.Auth;
using PeD.Core.ApiModels.Auth;
using PeD.Core.Requests;
using PeD.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace PeD.Controllers
{
    [SwaggerTag("Login")]
    [Route("api/[controller]")]
    public class LoginController : Controller
    {
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<Token>> Post(
            [FromBody] Login user,
            [FromServices] AccessManager accessManager,
            [FromServices] IDistributedCache cache)
        {
            if (await accessManager.ValidateCredentials(user))
            {
                var token = accessManager.GenerateToken(user);
                cache.Set(token.AccessToken,
                    Encoding.UTF8.GetBytes(DateTime.Now.ToString(CultureInfo.InvariantCulture)),
                    new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(4))
                );
                return token;
            }

            return Problem("Usuário ou senha incorreto", null, StatusCodes.Status401Unauthorized);
        }

        [AllowAnonymous]
        [HttpGet("/api/logout")]
        [HttpPost("/api/logout")]
        public ActionResult Logout([FromServices] IDistributedCache cache)
        {
            if (Request.Headers.ContainsKey(HeaderNames.Authorization))
            {
                var headerAuthorization = Request.Headers[HeaderNames.Authorization].Single().Split(" ").Last();
                cache.Remove(headerAuthorization);
            }

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("recuperar-senha")]
        public async Task<ActionResult> RecuperarSenha(
            [FromBody] Login user,
            [FromServices] AccessManager accessManager)
        {
            try
            {
                await accessManager.RecuperarSenha(user);
                return Ok();
            }
            catch (Exception)
            {
                return Problem("Não foi possivel enviar email de recuperação",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [AllowAnonymous]
        [HttpPost("nova-senha")]
        public async Task<ActionResult> NovaSenha(
            [FromBody] User user,
            [FromServices] AccessManager accessManager)
        {
            try
            {
                await accessManager.NovaSenha(user);                
                return Ok();
            }
            catch (Exception)
            {
                return Problem("Não foi possivel enviar email", statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}