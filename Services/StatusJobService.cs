using System.Text;
using System.Text.Json;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Quartz;
using AnthemAPI.Common;
using AnthemAPI.Models;
using static AnthemAPI.Common.Constants;
using AnthemAPI.Common.Helpers;

namespace AnthemAPI.Services;

public class StatusJobService
{
    private readonly ISchedulerFactory _schedulerFactory;

    public StatusJobService(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public async Task<ServiceResult<bool>> Exists(string userId)
    {
        try
        {
            IScheduler scheduler = await _schedulerFactory.GetScheduler();

            bool exists = await scheduler.CheckExists(new JobKey(userId));

            return ServiceResult<bool>.Success(exists);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure(e, $"Failed to check if exists for {userId}.", "StatusJobService.Exists()");
        }
    }

    public async Task<ServiceResult<bool>> Schedule(string userId)
    {
        try
        {
            IScheduler scheduler = await _schedulerFactory.GetScheduler();

            IJobDetail job = JobBuilder.Create<EmitStatus>()
                .WithIdentity(new JobKey(userId))
                .StoreDurably(false)
                .Build();
            
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(new TriggerKey(userId, Reduced.Group))
                .ForJob(job)
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(Reduced.IntervalSeconds)
                    .RepeatForever())
                .StartNow()
                .Build();
            
            await scheduler.ScheduleJob(job, trigger);

            Console.WriteLine("Scheduled " + userId);
            
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure(e, $"Failed to schedule for {userId}.", "StatusJobService.Schedule()");
        }
    }

    public async Task<ServiceResult<bool>> Unschedule(string userId, string pollingGroup)
    {
        try
        {
            IScheduler scheduler = await _schedulerFactory.GetScheduler();

            await scheduler.UnscheduleJob(new TriggerKey(userId, pollingGroup));

            Console.WriteLine("Unscheduled " + userId + " " + pollingGroup);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure(e, $"Failed to unschedule for {userId}.", "StatusJobService.Unschedule()");
        }
    }

    public async Task<ServiceResult<bool>> SetPollingTier(string userId, PollingTier tier)
    {
        try
        {
            IScheduler scheduler = await _schedulerFactory.GetScheduler();
            
            IJobDetail? job = await scheduler.GetJobDetail(new JobKey(userId));

            if (job is null)
            {
                return ServiceResult<bool>.Failure(null, $"No job detail for {userId}.", "StatusJobService.SetPollingTier()");
            }

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(new TriggerKey(userId, tier.Group))
                .ForJob(job)
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(tier.IntervalSeconds)
                    .RepeatForever())
                .StartAt(DateTimeOffset.Now.AddSeconds(tier.IntervalSeconds))
                .Build();
            
            string previousGroup = tier.Group == Active.Group
                ? Reduced.Group
                : Active.Group;
            
            await scheduler.RescheduleJob(new TriggerKey(userId, previousGroup), trigger);

            Console.WriteLine("Set tier for " + userId + " to " + tier.Group);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure(e, $"Failed to set polling tier for {userId}.", "StatusJobService.SetPollingTier()");
        }
    }
}

public class EmitStatus : IJob
{
    private readonly AuthorizationService _authorizationService;
    private readonly IAmazonApiGatewayManagementApi _client;
    private readonly StatusJobService _jobService;
    private readonly SpotifyService _spotifyService;
    private readonly StatusConnectionService _statusConnectionService;
    private readonly StatusService _statusService;
    private readonly TokenService _tokenService;

    public EmitStatus(
        AuthorizationService authorizationService,
        IConfiguration configuration,
        StatusJobService jobService,
        SpotifyService spotifyService,
        StatusConnectionService statusConnectionService,
        StatusService statusService,
        TokenService tokenService
    )
    {
        var config = new AmazonApiGatewayManagementApiConfig
        {
            ServiceURL = configuration["StatusApiGatewayUrl"]
        };
        _client = new AmazonApiGatewayManagementApiClient(config);
        _authorizationService = authorizationService;
        _jobService = jobService;
        _spotifyService = spotifyService;
        _statusConnectionService = statusConnectionService;
        _statusService = statusService;
        _tokenService = tokenService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        string userId = context.Trigger.Key.Name;
        string pollingGroup = context.Trigger.Key.Group;

        try
        {
            // Get the user's authorization
            var auth = await _authorizationService.Load(userId);

            if (auth.Data is null || auth.IsFailure)
                throw new Exception(auth.ErrorMessage);

            Authorization authorization = auth.Data;

            // Refresh the auth token if it is expired
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > authorization.ExpiresAt)
            {
                var refresh = await _tokenService.Refresh(authorization.RefreshToken);

                if (refresh.Data is null || refresh.IsFailure)
                    throw new Exception(refresh.ErrorMessage);

                string complete = Helpers.AddRefreshTokenProperty(refresh.Data, authorization.RefreshToken);
                JsonElement element = JsonDocument.Parse(complete).RootElement;
                var save = await _authorizationService.Save(element);

                if (save.Data is null || save.IsFailure)
                    throw new Exception(save.ErrorMessage);

                authorization = save.Data;
            }

            // Get the user's status connections
            var connections = await _statusConnectionService.Load(userId);

            if (connections.Data is null || connections.IsFailure)
                throw new Exception(connections.ErrorMessage);

            HashSet<string> connectionIds = connections.Data.ConnectionIds;

            if (connectionIds.Count == 0)
                throw new Exception("ConnectionIds list is empty.");

            // Get the user's Spotify status
            var getStatus = await _spotifyService.GetStatus(authorization.AccessToken, userId);

            if (getStatus.IsFailure)
                throw new Exception(getStatus.ErrorMessage);

            Status? status = getStatus.Data;

            // If we don't have a status, don't emit anything, and ensure we set the PollingTier to Reduced
            if (status is null)
            {
                if (pollingGroup == Active.Group)
                {
                    await _jobService.SetPollingTier(userId, Reduced);
                }

                return;
            }

            // If we have a status, ensure we set the PollingTier to Active
            if (pollingGroup == Reduced.Group)
            {
                await _jobService.SetPollingTier(userId, Active);
            }

            // Save the user's status
            var saveStatus = await _statusService.Save(status);

            if (saveStatus.Data is null || saveStatus.IsFailure)
                throw new Exception(saveStatus.ErrorMessage);

            // Send the user's Spotify status to all connections
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            string json = JsonSerializer.Serialize(status, options);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            var gone = new List<string>();

            var posts = connectionIds.Select(async connectionId =>
            {
                try
                {
                    await _client.PostToConnectionAsync(new PostToConnectionRequest
                    {
                        ConnectionId = connectionId,
                        Data = new MemoryStream(bytes)
                    });

                    Console.WriteLine($"Successfully sent to ConnectionId: {connectionId}");
                }
                catch (GoneException)
                {
                    Console.WriteLine("Adding " + connectionId + " to gone.");
                    gone.Add(connectionId);
                }
            });

            await Task.WhenAll(posts);

            // Remove all connections that are gone
            if (gone.Count > 0)
            {
                var remove = await _statusConnectionService.RemoveConnectionIds(userId, gone);

                if (remove.IsFailure)
                    throw new Exception(remove.ErrorMessage);
                
                int count = remove.Data;

                // Unschedule the job if there are no more connections
                if (count == 0)
                {
                    await _jobService.Unschedule(userId, pollingGroup);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Emit status job failed.\n{e.StackTrace}");
            await _jobService.Unschedule(userId, pollingGroup);
            await _statusConnectionService.Clear(userId);
        }
    }
}
