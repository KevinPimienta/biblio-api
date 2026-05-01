using BibliotecaAPI.Datos;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.ServiciosUsuarios;
using BibliotecaAPI.Swagger;
using BibliotecaAPI.Utilidades;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
//Service area
// Principio de inversion de dependencias: Video 58
// Las clases deben de depender de clases de tipo abstracto o Interface. <-- Permite tener un alto nivel de flexibilidad.
// Funcion de servivios. Que tiempo de vida escojer. Si se quiere compartir estados en distinas peticiones http: Singleton. Si queremos mantener el mismo estado en el msimo
// conexto http: Scope. Y si no interesa mantener el mismo estado: Transient.

builder.Services.AddOutputCache(opciones =>
{
    opciones.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(60);
});

//builder.Services.AddStackExchangeRedisOutputCache(opciones =>
//{
//    opciones.Configuration = builder.Configuration.GetConnectionString("redis");
//});

builder.Services.AddDataProtection();

var origenesPermitidos = builder.Configuration.GetSection("origenesPermitidos").Get<string[]>()!;

builder.Services.AddCors(opciones =>
{
    opciones.AddDefaultPolicy(opcionesCORS =>
    {
        opcionesCORS.WithOrigins(origenesPermitidos).AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("cantidad-total-registros");
    });
});

builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddControllers(opciones =>
{
    //opciones.Filters.Add<FiltroTiempoEjecucion>();
}).AddNewtonsoftJson();
    //.AddJsonOptions(opciones => opciones.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles); con DTOs esta linea ya no es necesaria.

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentityCore<Usuario>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<UserManager<Usuario>>();
builder.Services.AddScoped<SignInManager<Usuario>>();
builder.Services.AddTransient<IServiciosUsuarios, ServiciosUsuarios>();
builder.Services.AddTransient<IAlmacenadorArchivos, AlmacenadorArchivoLocal>();
//builder.Services.AddTransient<IServicioHash, ServicioHash>();
//builder.Services.AddScoped<MiFiltroDeAccion>();
builder.Services.AddScoped <FiltroValidacionLibro>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication().AddJwtBearer(opciones =>
{
    opciones.MapInboundClaims = false;

    opciones.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime= true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["llavejwt"]!)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(opciones =>
{
    opciones.AddPolicy("esadmin", politica => politica.RequireClaim("esadmin"));
});

builder.Services.AddSwaggerGen(opciones =>
{

                opciones.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Biblioteca API"
                });

                opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                 {
                       Name = "Authorization",
                       Type = SecuritySchemeType.ApiKey,
                       Scheme = "Bearer",
                       BearerFormat = "JWT",
                       In = ParameterLocation.Header
                 });

    opciones.OperationFilter<FiltroAutorizacion>();

    //opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
    //{
    //    {
    //        new OpenApiSecurityScheme
    //        {
    //            Reference = new OpenApiReference
    //            {
    //                Type = ReferenceType.SecurityScheme,
    //                Id = "Bearer"
    //            }
    //        },
    //        new string []{}
    //    }
    //});

});


var app = builder.Build();

//Middleware area

//Los middleware se pueden poner en su propira clase. Se usa un clase static publica con IApllicationBuilder
//app.Use(async (contexto, next) =>
//{   //Entrada de peticion
//    var logger = contexto.RequestServices.GetRequiredService<ILogger<Program>>();
//    logger.LogInformation($"Petición: {contexto.Request.Method} {contexto.Request.Path}");
//    await next.Invoke();

//    logger.LogInformation($"Respuesta: {contexto.Response.StatusCode}"); //<-- salida de respuesta
//}); 
app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async context =>
{
    var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
    var excepcion = exceptionHandlerFeature?.Error!;

    var error = new Error()
    {
        MensajeDeDeError = excepcion.Message,
        StackTrace = excepcion.StackTrace,
        Fecha = DateTime.UtcNow
    };

    var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
    dbContext.Add(error);
    await dbContext.SaveChangesAsync();
    await Results.InternalServerError(new 
    { tipo = "error",
        mensaje = "Ha ocurrido un error inseperado",
        estatus = 500 
    }).ExecuteAsync(context);
}));
app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();

app.UseCors();

app.UseOutputCache();

app.MapControllers();

app.Run();

public partial class Program { }
