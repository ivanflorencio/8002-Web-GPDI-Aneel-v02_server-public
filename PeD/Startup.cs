﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AspNetCoreRateLimit;
using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using GlobalExceptionHandler.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MimeDetective;
using MimeDetective.Definitions;
using MimeDetective.Definitions.Licensing;
using MimeDetective.Storage;
using Newtonsoft.Json;
using PeD.Auth;
using PeD.Authorizations;
using PeD.BackgroundServices;
using PeD.Core;
using PeD.Core.Exceptions;
using PeD.Core.Exceptions.Demandas;
using PeD.Core.Models;
using PeD.Data;
using PeD.Middlewares;
using PeD.Services;
using PeD.Services.Analises;
using PeD.Services.Captacoes;
using PeD.Services.Cronograma;
using PeD.Services.Demandas;
using PeD.Services.Projetos;
using PeD.Services.Projetos.Xml;
using PeD.Services.Sistema;
using Swashbuckle.AspNetCore.SwaggerUI;
using TaesaCore.Data;
using TaesaCore.Interfaces;
using TaesaCore.Services;
using CaptacaoService = PeD.Services.Captacoes.CaptacaoService;
using Log = Serilog.Log;
using Path = System.IO.Path;

namespace PeD
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        protected async Task OnError(HttpContext context, int statusCode, string messageError)
        {
            context.Response.StatusCode = statusCode;
            context.Response.Headers["Content-Type"] = "application/json; charset=utf-8";
            await context.Response.WriteAsync(
                JsonConvert.SerializeObject(new { Error = messageError }));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddMemoryCache();
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));
            services.AddInMemoryRateLimiting();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            services.AddSingleton(provider =>
            {
                var allDefintions = new ExhaustiveBuilder()
                {
                    UsageType = UsageType.PersonalNonCommercial
                }.Build();
                var allowedFiles = Configuration.GetSection("AllowedExtensionFiles").Get<string[]>()
                    .ToImmutableHashSet(StringComparer.InvariantCultureIgnoreCase);
                var scopedDefinitions = allDefintions
                        .ScopeExtensions(allowedFiles)
                        .TrimMeta() //If you don't care about the meta information (definition author, creation date, etc)
                        .TrimDescription() //If you don't care about the description
                                           // .TrimMimeType() //If you don't care about the mime type
                        .ToImmutableArray()
                    ;
                return new ContentInspectorBuilder() { Definitions = scopedDefinitions }.Build();
            });
            ConfigureSpa(services);
            try
            {
                ConfigureDatabaseConnection(services);
                ConfigureAuth(services);
                ConfigureEmail(services);
                services.AddSingleton<InstallService>();
                services.AddTransient<IStartupFilter, IdentityInitializer>();
            }
            catch (Exception e)
            {
                Log.Warning("Erro na configuração: {Error}", e.Message);
            }


            services.AddControllers()
                .AddNewtonsoftJson()
                .AddFluentValidation(fv =>
                {
                    fv.RegisterValidatorsFromAssemblyContaining(typeof(Startup));
                    fv.RegisterValidatorsFromAssemblyContaining(typeof(ApplicationUser));
                });
            services.AddHostedService<PropostasServices>();

            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddAutoMapper(typeof(Startup));
            services.AddScoped<PdfService>();


            services.AddCors(options =>
            {
                var origins = Configuration.GetSection("CorsOrigins")?.Get<string[]>() ?? Array.Empty<string>();
                options.AddPolicy("CorsPolicy", builder => builder
                    .WithOrigins(origins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithExposedHeaders("Content-Disposition"));
            });

            services.AddMvcCore().AddRazorViewEngine();
            services.AddDistributedMemoryCache();

            #region Swagger

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v2",
                    new OpenApiInfo
                    {
                        Title = "Norte Energia - Gestor PDI",
                        Version = "2.8",
                        Description = "API REST criada com o ASP.NET Core 3.1 para comunição com o Gestor PDI"
                    });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      \r\n\r\nExample: 'Bearer 12345abcdef'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header
                        },
                        new List<string>()
                    }
                });
                c.SchemaGeneratorOptions.SchemaIdSelector = type => type.FullName;
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
                c.SchemaGeneratorOptions.DiscriminatorNameSelector = type => type.FullName;
                c.SwaggerGeneratorOptions.TagsSelector = description =>
                {
                    var path = Regex.Replace(description.RelativePath, @"^api\/|\/\{.+?\}", "");
                    // Log.Information("{P1} => {P2}", description.RelativePath, path);
                    path = path.Replace("/", " => ");
                    return new[] { path };
                };
                //c.EnableAnnotations();
            });

            #endregion

            services.AddScoped<IViewRenderService, ViewRenderService>();

            #region Serviços

            services.AddScoped<SendGridService>();
            services.AddScoped<MailService>();
            services.AddScoped<ArquivoService>();
            services.AddScoped<UserService>();
            services.AddScoped<DemandaService>();
            services.AddScoped<DemandaLogService>();
            services.AddScoped<SistemaService>();
            services.AddScoped<CaptacaoService>();
            services.AddScoped<PropostaService>();
            services.AddScoped<CronogramaService>();
            services.AddScoped<CronogramaProjetoService>();
            services.AddScoped<EmpresaService>();
            services.AddScoped<ProjetoService>();
            services.AddTransient<RelatorioFinalService>();
            services.AddTransient<RelatorioAuditoriaService>();
            services.AddTransient<ProjetoPeDService>();
            services.AddTransient<AnaliseTecnicaService>();
            services.AddTransient<AnalisePedService>();

            #endregion

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.User.AllowedUserNameCharacters =
                        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+/";
                    options.ClaimsIdentity.UserIdClaimType = JwtRegisteredClaimNames.Jti;
                })
                .AddEntityFrameworkStores<GestorDbContext>()
                .AddDefaultTokenProviders()
                .AddErrorDescriber<PortugueseIdentityErrorDescriber>();
            services.Configure<IdentityOptions>(opt =>
            {
                opt.Lockout.MaxFailedAccessAttempts = Configuration.GetValue<int>("MaxFailedAccessAttempts");
                opt.Lockout.DefaultLockoutTimeSpan =
                    TimeSpan.FromMinutes(Configuration.GetValue<int>("LockoutTimeSpan"));
            });

            services.AddScoped<IViewRenderService, ViewRenderService>();
            services.AddScoped<DbContext, GestorDbContext>();
            // Configurando a dependência para a classe de validação
            // de credenciais e geração de tokens
            services.AddScoped<AccessManager>();

            #region Genéricos

            services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
            services.AddScoped(typeof(IService<>), typeof(BaseService<>));

            #endregion


            services.AddPropostaAuthorizations();
            services.AddRoleAuthorizations();
            services.AddTransient<XlsxService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            #region Define Cultura Padrão

            var cultureInfo = new CultureInfo("pt-BR")
            {
                NumberFormat =
                {
                    CurrencySymbol = "R$"
                }
            };
            ValidatorOptions.Global.LanguageManager.Culture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;

            #endregion

            #region Pasta Storage

            var storagePath = Configuration.GetValue<string>("StoragePath");
            if (!string.IsNullOrWhiteSpace(storagePath) && Directory.Exists(storagePath))
            {
                if (!Directory.Exists(Path.Combine(storagePath, "avatar")))
                {
                    Directory.CreateDirectory(Path.Combine(storagePath, "avatar"));
                }

                if (!Directory.Exists(Path.Combine(storagePath, "temp")))
                {
                    Directory.CreateDirectory(Path.Combine(storagePath, "temp"));
                }
            }

            #endregion

            app.UseIpRateLimiting();

            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
                await next();
            });


            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHttpsRedirection();
                app.UseHsts();
            }


            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            #region Swagger

            var swaggerEnable = Configuration.GetValue<bool>("SwaggerEnable");
            if (swaggerEnable)
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v2/swagger.json", "API V2");
                    c.DocExpansion(DocExpansion.List);
                    c.ShowExtensions();
                    c.EnableFilter();
                    c.EnableDeepLinking();
                    c.EnableValidator();
                });
            }

            #endregion


            app.UseRouting();
            app.UseCors("CorsPolicy");

            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (FileNotAllowedException)
                {
                    await OnError(context, StatusCodes.Status406NotAcceptable, "Extensão de arquivo não permitida!");
                }
                catch (Exception e)
                {
                    Log.Error(e, "Erro interno no servidor!");
                    await OnError(context, StatusCodes.Status500InternalServerError, "Erro interno no servidor!");
                }
            });

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<TokenMiddleware>();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
                await next();
            });

            app.UseWhen(context => context.Request.Method == "GET", appBranch => { appBranch.UseSpa(spa => { }); });
        }

        private void ConfigureDatabaseConnection(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("BaseGestor");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                services.AddDbContext<GestorDbContext>(options =>
                    {
                        options.UseSqlServer(connectionString, sqlServerOptions => sqlServerOptions.CommandTimeout(120));
                    }
                );
            }

            var connectionStringContabPed = Configuration.GetConnectionString("ContabPed");
            if (!string.IsNullOrWhiteSpace(connectionStringContabPed))
            {
                services.AddDbContext<ContabPedDbContext>(options =>
                    {
                        options.UseSqlServer(connectionStringContabPed, sqlServerOptions => sqlServerOptions.CommandTimeout(120));
                    }
                );
            }
        }

        private void ConfigureAuth(IServiceCollection services)
        {
            var tokenConfigurations = new TokenConfigurations();
            new ConfigureFromConfigurationOptions<TokenConfigurations>(Configuration.GetSection("TokenConfigurations"))
                .Configure(tokenConfigurations);
            var signingConfigurations = new SigningConfigurations(tokenConfigurations.BaseHash);
            services.AddSingleton(signingConfigurations);
            services.AddSingleton(tokenConfigurations);
            services.AddJwtSecurity(signingConfigurations, tokenConfigurations);
        }

        private void ConfigureEmail(IServiceCollection services)
        {
            var sendgrid = Configuration.GetSection("SendGrid");
            var mailSettings = Configuration.GetSection("EmailSettings");
            var emailConfig = new EmailConfig
            {
                ApiKey = sendgrid.GetValue<string>("ApiKey"),
                SenderEmail = sendgrid.GetValue<string>("SenderEmail"),
                SenderName = sendgrid.GetValue<string>("SenderName"),
                Bcc = sendgrid.GetSection("Bcc").Get<string[]>()
            };
            var emailSettings = new EmailSettings
            {
                DisplayName = mailSettings.GetValue<string>("DisplayName"),
                Host = mailSettings.GetValue<string>("Host"),
                Port = mailSettings.GetValue<int>("Port"),
                Mail = mailSettings.GetValue<string>("Mail"),
            };
            services.AddSingleton(emailSettings);
            services.AddSingleton(emailConfig);
        }

        private void ConfigureSpa(IServiceCollection services)
        {
            var spaPath = Configuration.GetValue<string>("SpaPath");
            services.AddSpaStaticFiles(opt =>
            {
                if (!string.IsNullOrWhiteSpace(spaPath) || Directory.Exists(spaPath))
                {
                    opt.RootPath = spaPath;
                }
                else
                {
                    opt.RootPath = "StaticFiles/DefaultSpa";
                }
            });
        }
    }
}