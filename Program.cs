using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Services;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddSystemsManager(c =>
{
    c.Path = $"/Anthem/{builder.Configuration["ASPNETCORE_ENVIRONMENT"]!}";
    c.Optional = false;
    c.ReloadAfter = TimeSpan.FromMinutes(10);
});

builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddScoped<DynamoDBContext>();

// builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<AuthorizationService>();
builder.Services.AddScoped<ChatConnectionService>();
builder.Services.AddScoped<ChatsService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<FollowersService>();
builder.Services.AddScoped<LikesService>();
builder.Services.AddScoped<MessagesService>();
builder.Services.AddScoped<StatusConnectionService>();
builder.Services.AddScoped<StatusJobService>();
builder.Services.AddScoped<StatusService>();
builder.Services.AddScoped<UsersService>();

builder.Services.AddHttpClient<SpotifyService>();
builder.Services.AddHttpClient<TokenService>();

builder.Services.AddQuartz();
builder.Services.AddQuartzHostedService();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseAuthorization();
app.MapControllers();
app.Run();
