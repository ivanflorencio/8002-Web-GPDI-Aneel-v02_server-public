﻿using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PeD.Auth;
using PeD.Core.ApiModels;
using PeD.Core.ApiModels.Auth;
using PeD.Core.Models;
using PeD.Core.Models.Fornecedores;
using PeD.Core.Requests;
using PeD.Data;
using PeD.Views.Email;
using TokenConfigurations = PeD.Auth.TokenConfigurations;

namespace PeD.Services
{
    public class AccessManager
    {
        private UserManager<ApplicationUser> _userManager;
        private GestorDbContext GestorDbContext;
        private SignInManager<ApplicationUser> _signInManager;
        private SigningConfigurations _signingConfigurations;
        private TokenConfigurations _tokenConfigurations;
        protected SendGridService SendGridService;
        protected IMapper Mapper;

        public AccessManager(
            UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
            SigningConfigurations signingConfigurations,
            TokenConfigurations tokenConfigurations,
            SendGridService sendGridService, IMapper mapper, GestorDbContext gestorDbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _signingConfigurations = signingConfigurations;
            _tokenConfigurations = tokenConfigurations;
            SendGridService = sendGridService;
            Mapper = mapper;
            GestorDbContext = gestorDbContext;
        }

        public async Task<bool> ValidateCredentials(Login user)
        {
            if (user != null && !string.IsNullOrWhiteSpace(user.Email))
            {
                // Verifica a existência do usuário nas tabelas do
                // ASP.NET Core Identity
                var userIdentity = _userManager
                    .FindByEmailAsync(user.Email).Result;
                if (userIdentity != null)
                {
                    // Efetua o login com base no Id do usuário e sua senha
                    var resultadoLogin =
                        await _signInManager.CheckPasswordSignInAsync(userIdentity, user.Password, true);
                    if (resultadoLogin.Succeeded)
                    {
                        return userIdentity.Status;
                    }
                }
            }

            return false;
        }

        public Token GenerateToken(Login user)
        {
            var userIdentity = _userManager
                .FindByEmailAsync(user.Email).Result;
            if (userIdentity.EmpresaId != null)
            {
                userIdentity.Empresa =
                    GestorDbContext.Empresas.FirstOrDefault(ce => ce.Id == userIdentity.EmpresaId);
            }

            var roles = _userManager.GetRolesAsync(userIdentity).Result.ToList();
            // Correção de funções do usuário
            if (roles.Count == 0 && !string.IsNullOrWhiteSpace(userIdentity.Role))
            {
                _userManager.AddToRoleAsync(userIdentity, userIdentity.Role).Wait();
                roles.Add(userIdentity.Role);
            }

            userIdentity.Roles = roles;

            var identity = new ClaimsIdentity(
                new GenericIdentity(user.Email, "Login"),
                new[]
                {
                    new Claim(JwtRegisteredClaimNames.Jti, userIdentity.Id),
                    new Claim(JwtRegisteredClaimNames.UniqueName, user.Email),
                    new Claim(ClaimTypes.GroupSid, userIdentity.EmpresaId.ToString()),
                    new Claim(ClaimTypes.Role,
                        userIdentity.Role ?? "")
                }.Concat(roles.Select(r => new Claim(ClaimTypes.Role, r)))
            );

            var dataCriacao = DateTime.Now;
            var dataExpiracao = dataCriacao +
                                TimeSpan.FromSeconds(_tokenConfigurations.Seconds);

            var handler = new JwtSecurityTokenHandler();
            var securityToken = handler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _tokenConfigurations.Issuer,
                Audience = _tokenConfigurations.Audience,
                SigningCredentials = _signingConfigurations.SigningCredentials,
                Subject = identity,
                NotBefore = dataCriacao,
                Expires = dataExpiracao
            });
            var token = handler.WriteToken(securityToken);

            return new Token()
            {
                AccessToken = token,
                User = Mapper.Map<ApplicationUserDto>(userIdentity)
            };
        }

        public async Task<bool> RecuperarSenha(Login user)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return false;
            }
            else if (await SendRecoverAccountEmail(user.Email))
            {
                return true;
            }

            throw new Exception();
        }

        public async Task<bool> NovaSenha(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.NewPassword))
            {
                throw new Exception();
            }

            var applicationUser = _userManager.Users.Where(u => u.Email == user.Email).FirstOrDefault();

            if (applicationUser == null) throw new Exception();

            var token = Encoding.ASCII.GetString(Convert.FromBase64String(user.ResetToken));
            var result = await _userManager.ResetPasswordAsync(applicationUser, token, user.NewPassword);

            if (result.Errors.Count() > 0) throw new Exception();

            return true;
        }

        public async Task<bool> SendRecoverAccountEmail(string email, bool newAccount = false,
            string subject = "Redefinição de Senha - Gerenciador PDI Norte Energia")
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            return await SendGridService.Send(email, subject,
                newAccount ? "Email/RegisterAccount" : "Email/RecoverAccount",
                new RecoverAccount()
                {
                    Email = email,
                    Token = token
                });
        }

        public async Task<bool> SendNewFornecedorAccountEmail(string email, Fornecedor fornecedor)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || fornecedor == null) return false;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            return await SendGridService.Send(email,
                "Você foi convidado para participar do Gestor PDI da Norte Energia como Fornecedor Cadastrado",
                "Email/FornecedorAccount",
                new FornecedorAccount()
                {
                    Email = email,
                    Token = token,
                    Fornecedor = fornecedor.Nome
                });
        }
    }
}