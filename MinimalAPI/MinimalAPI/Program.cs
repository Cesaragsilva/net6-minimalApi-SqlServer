using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MinimalAPI.Data;
using MinimalAPI.Models;
using MinimalAPI.ViewModel;
using MiniValidation;
using NetDevPack.Identity;
using NetDevPack.Identity.Jwt;
using NetDevPack.Identity.Model;

var builder = WebApplication.CreateBuilder(args);

#region ConfigureServices

builder.Services.AddIdentityEntityFrameworkContextConfiguration(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Database"),
    b => b.MigrationsAssembly("MinimalAPI")));

builder.Services.AddDbContext<ContextDb>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddIdentityConfiguration();
builder.Services.AddJwtConfiguration(builder.Configuration, "AppSettings");

//Validação com Claims (Necessario criar a claim na tabela AspNetUserClaim)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ExcluirFornecedor",
        policy => policy.RequireClaim("ExcluirFornecedor"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minimal API Sample",
        Description = "Desenvolvido por César Augusto - Base: https://www.youtube.com/watch?v=aXayqUfSNvw",
        Contact = new OpenApiContact { Name = "Cesar Augusto", Email = "educador.cesar@gmail.com" },
        License = new OpenApiLicense { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT desta maneira: Bearer {seu token}",
        Name = "Authorization",
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
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
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

#endregion

#region ConfigurePipelines

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthConfiguration();
app.UseHttpsRedirection();

MapActionsUsuarios(app);
MapActionsFornecedores(app);

app.Run();
#endregion

#region Actions

void MapActionsUsuarios(WebApplication app)
{

    app.MapPost("/registro", [AllowAnonymous] async (
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IOptions<AppJwtSettings> appJwtSettings,
        RegisterUser registerUser) =>
    {
        if (registerUser == null)
            return Results.BadRequest("Usuário não informado");

        if (!MiniValidator.TryValidate(registerUser, out var errors))
            return Results.ValidationProblem(errors);

        var user = new IdentityUser
        {
            UserName = registerUser.Email,
            Email = registerUser.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, registerUser.Password);

        if (!result.Succeeded)
            return Results.BadRequest(result.Errors);

        var jwt = new JwtBuilder()
                    .WithUserManager(userManager)
                    .WithJwtSettings(appJwtSettings.Value)
                    .WithEmail(user.Email)
                    .WithJwtClaims()
                    .WithUserClaims()
                    .WithUserRoles()
                    .BuildUserResponse();

        return Results.Ok(jwt);

    }).ProducesValidationProblem()
      .Produces(StatusCodes.Status200OK)
      .Produces(StatusCodes.Status400BadRequest)
      .WithName("RegistroUsuario")
      .WithTags("Usuario");

    app.MapPost("/login", [AllowAnonymous] async (
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IOptions<AppJwtSettings> appJwtSettings,
        LoginUser loginUser) =>
    {
        if (loginUser == null)
            return Results.BadRequest("Usuário não informado");

        if (!MiniValidator.TryValidate(loginUser, out var errors))
            return Results.ValidationProblem(errors);

        var result = await signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

        if (result.IsLockedOut)
            return Results.BadRequest("Usuário bloqueado");

        if (!result.Succeeded)
            return Results.BadRequest("Usuário ou senha inválidos");

        var jwt = new JwtBuilder()
                    .WithUserManager(userManager)
                    .WithJwtSettings(appJwtSettings.Value)
                    .WithEmail(loginUser.Email)
                    .WithJwtClaims()
                    .WithUserClaims()
                    .WithUserRoles()
                    .BuildUserResponse();

        return Results.Ok(jwt);

    }).ProducesValidationProblem()
      .Produces(StatusCodes.Status200OK)
      .Produces(StatusCodes.Status400BadRequest)
      .WithName("LoginUsuario")
      .WithTags("Usuario");
}

void MapActionsFornecedores(WebApplication app)
{
    app.MapGet("/fornecedor", [AllowAnonymous] async (
        ContextDb context) =>
        await context.Fornecedores.ToListAsync()
    )
    .WithName("GetFornecedor").WithTags("Fornecedor");

    app.MapGet("/fornecedor/{id}", [AllowAnonymous] async (
        Guid id,
        ContextDb context) =>
        await context.Fornecedores.FindAsync(id) is Fornecedor fornecedor ? Results.Ok(fornecedor) : Results.NotFound()
    )
    .Produces<Fornecedor>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status500InternalServerError)
    .WithName("GetFornecedorPorId").WithTags("Fornecedor");

    app.MapPost("/fornecedor", [Authorize] async (
        ContextDb context,
        FornecedorViewModel fornecedorDto) =>
    {
        if (!MiniValidator.TryValidate(fornecedorDto, out var erros))
            return Results.ValidationProblem(erros);

        var entidade = fornecedorDto.ToFornecedorEntity();
        context.Fornecedores.Add(entidade);
        var resultado = await context.SaveChangesAsync();

        return resultado > 0
                ? Results.CreatedAtRoute("GetFornecedorPorId", new { id = entidade.Id })
                : Results.BadRequest("Erro ao salvar fornecedor!");
    })
    .ProducesValidationProblem()
    .Produces<Fornecedor>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status500InternalServerError)
    .WithName("PostFornecedor").WithTags("Fornecedor");


    app.MapPut("/fornecedor/{id}", [Authorize] async (
        Guid id,
        ContextDb context,
        FornecedorViewModel fornecedorDto) =>
    {
        var forncedor = await context.Fornecedores.AsNoTracking<Fornecedor>()
                                                  .FirstOrDefaultAsync(c => c.Id == id);

        if (forncedor == null) return Results.NotFound();

        if (!MiniValidator.TryValidate(fornecedorDto, out var erros))
            return Results.ValidationProblem(erros);

        forncedor.Atualizar(fornecedorDto.Nome, fornecedorDto.Documento);
        context.Fornecedores.Update(forncedor);
        var resultado = await context.SaveChangesAsync();

        return resultado > 0
                ? Results.NoContent()
                : Results.BadRequest("Erro ao salvar atualização de fornecedor!");
    })
    .ProducesValidationProblem()
    .Produces<Fornecedor>(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status500InternalServerError)
    .WithName("PutFornecedor").WithTags("Fornecedor");

    app.MapDelete("/fornecedor/{id}", [Authorize] async (
        Guid id,
        ContextDb context) =>
    {
        var forncedor = await context.Fornecedores.AsNoTracking<Fornecedor>()
                                                  .FirstOrDefaultAsync(c => c.Id == id);

        if (forncedor == null) return Results.NotFound();

        context.Fornecedores.Remove(forncedor);
        var resultado = await context.SaveChangesAsync();

        return resultado > 0
                ? Results.NoContent()
                : Results.BadRequest("Erro ao salvar atualização de fornecedor!");
    })
    .ProducesValidationProblem()
    .RequireAuthorization("ExcluirFornecedor")
    .Produces<Fornecedor>(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status500InternalServerError)
    .WithName("DeleteFornecedor").WithTags("Fornecedor");
}

#endregion