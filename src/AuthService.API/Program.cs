using AuthService.API;

var builder = WebApplication.CreateBuilder(args);

// Use Startup class to configure services
var startup = new Startup(builder.Configuration, builder.Environment);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
startup.Configure(app, app.Environment);

app.Run();