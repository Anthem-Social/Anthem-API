using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Authentication;
using AnthemAPI.Authorization;
using AnthemAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Quartz;
using static AnthemAPI.Common.Constants;

var builder = WebApplication.CreateBuilder(args);

// Load secrets and configurations
builder.Configuration.AddSystemsManager(c =>
{
    c.Path = $"/Anthem/{builder.Configuration["ASPNETCORE_ENVIRONMENT"]!}";
    c.Optional = false;
    c.ReloadAfter = TimeSpan.FromMinutes(10);
});

// AWS services
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddScoped<DynamoDBContext>();

// HTTP client factory
builder.Services.AddHttpClient();

// Authentication schemes
builder.Services.AddAuthentication()
    .AddScheme<SpotifyAuthenticationOptions, SpotifyAuthenticationHandler>(Spotify, options => { });

// Authorization handlers
builder.Services.AddScoped<IAuthorizationHandler, ChatCreatorHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ChatMemberHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CommentCreatorHandler>();
builder.Services.AddScoped<IAuthorizationHandler, LikeCreatorHandler>();
builder.Services.AddScoped<IAuthorizationHandler, MessageCreatorHandler>();
builder.Services.AddScoped<IAuthorizationHandler, PostCreatorHandler>();
builder.Services.AddScoped<IAuthorizationHandler, SelfHandler>();

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ChatCreator, policy =>
    {
        policy
            .AddAuthenticationSchemes(Spotify)
            .AddRequirements(new ChatCreatorRequirement());
    });
    options.AddPolicy(ChatMember, policy =>
    {
        policy
            .AddAuthenticationSchemes(Spotify)
            .AddRequirements(new ChatMemberRequirement());
    });
    options.AddPolicy(CommentCreator, policy =>
    {
        policy
            .AddAuthenticationSchemes(Spotify)
            .AddRequirements(new CommentCreatorRequirement());
    });
    options.AddPolicy(LikeCreator, policy =>
    {
        policy
            .AddAuthenticationSchemes(Spotify)
            .AddRequirements(new LikeCreatorRequirement());
    });
    options.AddPolicy(MessageCreator, policy =>
    {
        policy
            .AddAuthenticationSchemes(Spotify)
            .AddRequirements(new MessageCreatorRequirement());
    });
    options.AddPolicy(PostCreator, policy =>
    {
        policy
            .AddAuthenticationSchemes(Spotify)
            .AddRequirements(new PostCreatorRequirement());
    });
    options.AddPolicy(Self, policy =>
    {
        policy
            .AddAuthenticationSchemes(Spotify)
            .AddRequirements(new SelfRequirement());
    });
});

// Services
builder.Services.AddScoped<AuthorizationsService>();
builder.Services.AddScoped<ChatConnectionsService>();
builder.Services.AddScoped<ChatsService>();
builder.Services.AddScoped<CommentsService>();
builder.Services.AddScoped<FeedsService>();
builder.Services.AddScoped<FollowersService>();
builder.Services.AddScoped<LikesService>();
builder.Services.AddScoped<MessagesService>();
builder.Services.AddScoped<PostsService>();
builder.Services.AddScoped<SpotifyService>();
builder.Services.AddScoped<StatusConnectionsService>();
builder.Services.AddScoped<StatusesService>();
builder.Services.AddScoped<StatusJobService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<UsersService>();

// Quartz
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

app.UseAuthorization();
app.MapControllers();
app.Run();
