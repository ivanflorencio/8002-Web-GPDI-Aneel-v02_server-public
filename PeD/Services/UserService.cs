﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PeD.Core.ApiModels;
using PeD.Core.Models;
using PeD.Core.Models.Catalogos;
using PeD.Data;
using TaesaCore.Extensions;

namespace PeD.Services
{
    //@todo Refatorar os metodos que retornam Resultado
    public class UserService
    {
        private GestorDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private AccessManager accessManager;
        private IConfiguration Configuration;

        public UserService(
            GestorDbContext context,
            UserManager<ApplicationUser> userManager,
            AccessManager accessManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            this.accessManager = accessManager;
            Configuration = configuration;
        }

        protected List<string> GetRoles(ApplicationUser user) => _userManager.GetRolesAsync(user).Result.ToList();

        public ApplicationUser Obter(string userId)
        {
            if (!String.IsNullOrWhiteSpace(userId))
            {
                var user = _context.Users
                    .Where(p => p.Id == userId).Include("Empresa").FirstOrDefault();
                if (user != null)
                {
                    user.Roles = GetRoles(user);
                }

                return user;
            }

            return null;
        }

        public IEnumerable<ApplicationUser> ListarTodos()
        {
            var Users = _context.Users
                .Include("Empresa")
                .OrderBy(p => p.Id)
                .ToList();
            return Users;
        }

        public Resultado Incluir(ApplicationUser dadosUser)
        {
            Resultado resultado = DadosValidos(dadosUser);
            resultado.Acao = "Inclusão de User";

            if (resultado.Inconsistencias.Count == 0 &&
                _context.Users.Where(
                    p => p.Email == dadosUser.Email).Count() > 0)
            {
                resultado.Inconsistencias.Add(
                    "E-mail já cadastrado");
            }

            if (resultado.Sucesso)
            {
                string Password = DateTime.Now.ToString().ToMD5() + $"@Mi{DateTime.Now.Minute}";
                dadosUser.Id = Guid.NewGuid().ToString();
                dadosUser.EmailConfirmed = true;
                dadosUser.DataCadastro = DateTime.Now;
                resultado = CreateUser(dadosUser, Password, dadosUser.Role);
                string UserId = resultado.Id;
                if (resultado.Sucesso)
                {
                    try
                    {
                        accessManager.SendRecoverAccountEmail(dadosUser.Email, true,
                            "Seja bem-vindo ao Gerenciador P&D Taesa").Wait(10000);
                    }
                    catch (Exception e)
                    {
                        resultado.Inconsistencias.Add(e.Message);
                    }

                    resultado.Id = UserId;
                }
            }

            return resultado;
        }

        public Resultado Atualizar(ApplicationUser dadosUser)
        {
            var resultado = new Resultado();
            resultado.Acao = "Atualização de User";

            var user = _context.Users
                .Where(u => u.Id == dadosUser.Id)
                .FirstOrDefault();

            if (user == null)
            {
                resultado.Inconsistencias.Add("User não encontrado");
            }

            if (resultado.Inconsistencias.Count == 0 && user != null)
            {
                var roles = _userManager.GetRolesAsync(user).Result.ToList();
                roles.ForEach(r => _userManager.RemoveFromRoleAsync(user, r).Wait());
                _userManager.AddToRoleAsync(user, dadosUser.Role).Wait();

                user.Status = dadosUser.Status;
                user.NomeCompleto = dadosUser.NomeCompleto == null ? user.NomeCompleto : dadosUser.NomeCompleto;
                user.EmpresaId = dadosUser.EmpresaId == 0
                    ? null
                    : dadosUser.EmpresaId;
                user.RazaoSocial = dadosUser.RazaoSocial == null ? user.RazaoSocial : dadosUser.RazaoSocial;


                user.Role = dadosUser.Role == null ? user.Role : dadosUser.Role;
                user.Cpf = dadosUser.Cpf == null ? user.Cpf : dadosUser.Cpf;
                user.Cargo = dadosUser.Cargo == null ? user.Cargo : dadosUser.Cargo;
                user.DataAtualizacao = DateTime.Now;
                _context.SaveChanges();
            }

            return resultado;
        }

        public Resultado Excluir(string userId)
        {
            Resultado resultado = new Resultado();
            resultado.Acao = "Exclusão de User";

            ApplicationUser User = Obter(userId);
            if (User == null)
            {
                resultado.Inconsistencias.Add(
                    "User não encontrado");
            }
            else
            {
                //_context.FotoPerfil.RemoveRange(_context.FotoPerfil.Where(t => t.UserId == userId));
                _context.Users.Remove(User);
                _context.SaveChanges();
            }

            return resultado;
        }

        private Resultado DadosValidos(ApplicationUser User)
        {
            var resultado = new Resultado();
            if (User == null)
            {
                resultado.Inconsistencias.Add(
                    "Preencha os Dados do User");
            }
            else
            {
                if (String.IsNullOrWhiteSpace(User.Email))
                {
                    resultado.Inconsistencias.Add(
                        "Preencha o E-mail do Usuário");
                }

                if (String.IsNullOrWhiteSpace(User.NomeCompleto))
                {
                    resultado.Inconsistencias.Add(
                        "Preencha o Nome Completo do Usuário");
                }

                if (String.IsNullOrWhiteSpace(User.Role))
                {
                    resultado.Inconsistencias.Add(
                        "Preencha a Role do usuário");
                }
                else
                {
                    if (!Roles.AllRoles.Contains(User.Role))
                    {
                        resultado.Inconsistencias.Add(
                            "Role do usuário não identificada.");
                    }
                }

                if (User.EmpresaId > 0)
                {
                    var Empresa = _context.Empresas.Where(
                        e => e.Id == User.EmpresaId).FirstOrDefault();
                    if (Empresa == null)
                    {
                        resultado.Inconsistencias.Add("Empresa não encontrada");
                    }
                }
            }

            return resultado;
        }

        public Resultado CreateUser(
            ApplicationUser user,
            string password,
            string initialRole = null)
        {
            var resultado = new Resultado();
            resultado.Acao = "Inclusão de User";
            user.UserName = user.Email;

            if (_userManager.FindByEmailAsync(user.Email).Result == null)
            {
                var result = _userManager
                    .CreateAsync(user, password).Result;

                if (result.Succeeded &&
                    !String.IsNullOrWhiteSpace(initialRole))
                {
                    resultado.Id = user.Id;
                    _userManager.AddToRoleAsync(user, initialRole).Wait();
                }

                if (result.Errors.Count() > 0)
                {
                    foreach (var error in result.Errors)
                    {
                        resultado.Inconsistencias.Add(error.Description);
                    }
                }
            }

            return resultado;
        }

        public async Task Desativar(string emailOrId)
        {
            var user = emailOrId.Contains('@')
                ? await _userManager.FindByEmailAsync(emailOrId)
                : await _userManager.FindByIdAsync(emailOrId);
            if (user != null && user.Status)
            {
                await Desativar(user);
            }
        }

        public async Task Desativar(ApplicationUser user)
        {
            user.Status = false;
            await _userManager.UpdateAsync(user);
        }

        public async Task Ativar(string emailOrId)
        {
            var user = emailOrId.Contains('@')
                ? await _userManager.FindByEmailAsync(emailOrId)
                : await _userManager.FindByIdAsync(emailOrId);
            if (user != null && user.Status == true)
            {
                await Ativar(user);
            }
        }

        public async Task Ativar(ApplicationUser user)
        {
            user.Status = true;
            await _userManager.UpdateAsync(user);
        }

        public async Task UpdateAvatar(string id, IFormFile file)
        {
            var filename = id + ".jpg";
            var storagePath = Configuration.GetValue<string>("StoragePath");
            var fullname = Path.Combine(storagePath, "avatar", filename);

            if (File.Exists(fullname))
            {
                File.Move(fullname, fullname + ".old");
            }

            try
            {
                using (var stream = File.Create(fullname))
                {
                    await file.CopyToAsync(stream);
                    stream.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (File.Exists(fullname + ".old"))
                {
                    File.Move(fullname + ".old", fullname);
                }
            }
            finally
            {
                if (File.Exists(fullname + ".old"))
                {
                    File.Delete(fullname + ".old");
                }
            }

            var user = Obter(id);
            user.FotoPerfil = $"/avatar/{filename}";
            Atualizar(user);
        }
    }
}