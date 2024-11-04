using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Enuns;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Dominio.ModelViews;
using MinimalApi.Dominio.Servicos;
using MinimalApi.DTOs;
using MinimalApi.Infraestrutura.Db;
using Swashbuckle.AspNetCore.SwaggerGen;

#region Builder
var builder = WebApplication.CreateBuilder(args);

var key = builder.Configuration.GetSection("Jwt").ToString();
if(string.IsNullOrEmpty(key)) key = "123456";

builder.Services.AddAuthentication(option => {
  option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option => {
  option.TokenValidationParameters = new TokenValidationParameters{
    ValidateLifetime = true,    
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
    ValidateIssuer = false,
    ValidateAudience = false,
  };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>{
  options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "Insira o token Jwt aqui"
  });

  options.AddSecurityRequirement(new OpenApiSecurityRequirement
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


builder.Services.AddDbContext<DBcontexto>(options =>{
    options.UseMySql(builder.Configuration.GetConnectionString("MySql"), ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("MySql")));
});

var app = builder.Build();
#endregion

#region Home
app.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");
#endregion

#region Administradores

string GerarTokenJwt(Administrador administrador){
  if(string.IsNullOrEmpty(key)) return string.Empty;

  var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
  var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

  var claims = new List<Claim>()
  {
    new Claim("Email", administrador.Email),
    new Claim("Perfil", administrador.Perfil),
    new Claim(ClaimTypes.Role, administrador.Perfil),
    
  };

  var token = new JwtSecurityToken(
      claims: claims,
      expires: DateTime.Now.AddDays(1),
      signingCredentials: credentials
  );
    return new JwtSecurityTokenHandler().WriteToken(token);
  };

app.MapPost("/Administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) => 
{
  var adm = administradorServico.Login(loginDTO);
  if(adm != null){
    string token = GerarTokenJwt(adm);
      return Results.Ok(new AdministradorLogado
      {
        Email = adm.Email,
        Perfil = adm.Perfil,
        Token = token
      });
  }
  else
      return Results.Unauthorized();
}).AllowAnonymous().WithTags("Administradores");

app.MapPost("/Administradores", ([FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) => 
{
  var validacao = new ErrosDeValidacao{
    Mensagens = new List<string>()};
  
  if(string.IsNullOrEmpty(administradorDTO.Email)) validacao.Mensagens.Add("Não é possível incluir email vazio!");
  if(string.IsNullOrEmpty(administradorDTO.Senha)) validacao.Mensagens.Add("Não é possível incluir Senha vazia!");
  if(administradorDTO.Perfil == null) validacao.Mensagens.Add("Não é possível incluir Perfil vazia!");
  
  if(validacao.Mensagens.Count > 0) return Results.BadRequest(validacao);

  var administrador = (new Administrador{
    Email = administradorDTO.Email,
    Senha = administradorDTO.Senha,
    Perfil = administradorDTO.Perfil.ToString() ?? Perfil.editor.ToString()

});
  administradorServico.Incluir(administrador);

  return Results.Created($"/administrador/{administrador.Id}", new AdministradorModelView{
      Id = administrador.Id,
      Email = administrador.Email,
      Perfil = administrador.Perfil
  });

})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "adm"})
.WithTags("Administradores");

app.MapGet("/administradores", ([FromQuery] int? pagina, IAdministradorServico administradorServico) => 
{
  var adms = new List<AdministradorModelView>();
  var administradores = administradorServico.Todos(pagina);
  foreach (var adm in administradores)
  {
    adms.Add(new AdministradorModelView{
      Id = adm.Id,
      Email = adm.Email,
      Perfil = adm.Perfil
    });
  }

  return Results.Ok(adms);
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "adm"})
.WithTags("Administradores");

app.MapGet("/administradores/{id}", ([FromRoute] int id, IAdministradorServico administradorServico) => 
{
  var administrador = administradorServico.BuscaPorId(id);
  if(administrador == null) return Results.NotFound();
  return Results.Ok(new AdministradorModelView{
      Id = administrador.Id,
      Email = administrador.Email,
      Perfil = administrador.Perfil
  });
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "adm"})
.WithTags("Administradores");

#endregion

#region Veiculos
ErrosDeValidacao validaDTO(VeiculoDTO veiculoDTO)
{
    var validacao = new ErrosDeValidacao{
      Mensagens = new List<string>()
    };

  if(string.IsNullOrEmpty(veiculoDTO.Nome)) 
    validacao.Mensagens.Add("O nome não pode ser vazio!");

  if(string.IsNullOrEmpty(veiculoDTO.Marca)) 
    validacao.Mensagens.Add("A Marca não pode estar em branco!");

  if(veiculoDTO.Ano < 1950 || veiculoDTO.Ano > (DateTime.Now.Year + 1))   
    validacao.Mensagens.Add("Não é possivel incluir data inferior a 1950 ou superior ao próximo ano!");

    return validacao;
}


app.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) => 
{    
  var validacao = validaDTO(veiculoDTO);
  if(validacao.Mensagens.Count > 0) return Results.BadRequest(validacao);

  var veiculo = (new Veiculo{
    Nome = veiculoDTO.Nome,
    Marca = veiculoDTO.Marca,
    Ano = veiculoDTO.Ano
  });
  veiculoServico.Incluir(veiculo);

  return Results.Created($"/veiculo/{veiculo.Id}", veiculo);

})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "adm, editor"})
.WithTags("Veiculos");

app.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculoServico veiculoServico) => 
{
  var veiculos = veiculoServico.Todos(pagina);
   
  return Results.Ok(veiculos);

}).RequireAuthorization().WithTags("Veiculos");

app.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) => 
{
  var veiculo = veiculoServico.BuscaPorId(id);
  if(veiculo == null) return Results.NotFound();
  return Results.Ok(veiculo);

})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "adm, editor"})
.WithTags("Veiculos");

app.MapPut("/veiculos/{id}", ([FromRoute] int id, VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) => 
{
  var veiculo = veiculoServico.BuscaPorId(id);
  if(veiculo == null) return Results.NotFound();

  var validacao = validaDTO(veiculoDTO);
  if(validacao.Mensagens.Count > 0) return Results.BadRequest(validacao);
  
  veiculo.Nome = veiculoDTO.Nome;
  veiculo.Marca = veiculoDTO.Marca;
  veiculo.Ano = veiculoDTO.Ano;

  veiculoServico.Atualizar(veiculo);

  return Results.Ok(veiculo);

})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "adm"})
.WithTags("Veiculos");

app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) => 
{
  var veiculo = veiculoServico.BuscaPorId(id);
  if(veiculo == null) return Results.NotFound();  

  veiculoServico.Apagar(veiculo);

  return Results.NoContent();

})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "adm"})
.WithTags("Veiculos");

#endregion

#region App
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
#endregion