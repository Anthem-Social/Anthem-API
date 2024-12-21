using System.Text;
using System.Text.Json;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Quartz;
using AnthemAPI.Common;
using AnthemAPI.Models;
using static AnthemAPI.Common.Constants;

namespace AnthemAPI.Services;

public class JobService
{
    private readonly ISchedulerFactory _schedulerFactory;

    public JobService(ISchedulerFactory schedulerFactory)
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
            return ServiceResult<bool>.Failure($"Failed to check if job with key {userId} exists.\n{e}", "JobService.Exists()");
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
            return ServiceResult<bool>.Failure($"Failed to schedule job for user: {userId}.\n{e}", "JobService.Schedule()");
        }
    }

    public async Task<ServiceResult<bool>> Unschedule(string userId, string pollingGroup)
    {
        try
        {
            IScheduler scheduler = await _schedulerFactory.GetScheduler();

            await scheduler.UnscheduleJob(new TriggerKey(userId, pollingGroup));

            Console.WriteLine("Unscheduled " + userId);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure($"Failed to unschedule job for {userId}.\n{e}", "JobService.Unschedule()");
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
                return ServiceResult<bool>.Failure($"No job detail for {userId}.", "JobService.Reschedule()");
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

            Console.WriteLine("Set tier to " + tier.Group + " for " + userId);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure($"Failed to change job interval for {userId}.\n{e}", "JobService.Reschedule()");
        }
    }
}

public class EmitStatus : IJob
{
    private readonly AuthorizationService _authorizationService;
    private readonly IAmazonApiGatewayManagementApi _client;
    private readonly JobService _jobService;
    private readonly SpotifyService _spotifyService;
    private readonly StatusConnectionService _statusConnectionService;

    public EmitStatus(
        AuthorizationService authorizationService,
        JobService jobService,
        SpotifyService spotifyService,
        StatusConnectionService statusConnectionService
    )
    {
        var config = new AmazonApiGatewayManagementApiConfig
        {
            ServiceURL = "https://wda44qensj.execute-api.us-east-1.amazonaws.com/development"
        };
        _client = new AmazonApiGatewayManagementApiClient(config);
        _authorizationService = authorizationService;
        _jobService = jobService;
        _spotifyService = spotifyService;
        _statusConnectionService = statusConnectionService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        string userId = context.Trigger.Key.Name;
        string pollingGroup = context.Trigger.Key.Group;

        try
        {
            // Get the user's authorization
            var authResult = await _authorizationService.Load(userId);

            if (authResult.Data is null || authResult.IsFailure)
            {
                throw new Exception(authResult.ErrorMessage);
            }

            Authorization authorization = authResult.Data;

            // Refresh the auth token if it is expired
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > authorization.ExpiresAt)
            {
                var refreshResult = await _authorizationService.Refresh(authorization.RefreshToken);

                if (refreshResult.Data is null || refreshResult.IsFailure)
                {
                    throw new Exception(refreshResult.ErrorMessage);
                }

                JsonElement refreshJson = JsonDocument.Parse(refreshResult.Data!).RootElement;
                var saveResult = await _authorizationService.Save(refreshJson);

                if (saveResult.Data is null || saveResult.IsFailure)
                {
                    throw new Exception(saveResult.ErrorMessage);
                }

                authorization = saveResult.Data;
            }

            // Get the user's status connections
            var connectionsResult = await _statusConnectionService.Load(userId);

            if (connectionsResult.Data is null || connectionsResult.IsFailure)
            {
                throw new Exception(connectionsResult.ErrorMessage);
            }

            HashSet<string> connectionIds = connectionsResult.Data.ConnectionIds;

            // Get the user's Spotify status
            var statusResult = await _spotifyService.GetStatus(authorization.AccessToken, userId);

            if (statusResult.IsFailure)
            {
                throw new Exception(statusResult.ErrorMessage);
            }

            Status? status = statusResult.Data;

            // If it is empty, don't send it
            if (status is null)
            {
                // Set the PollingTier to Reduced
                if (pollingGroup == Active.Group)
                {
                    await _jobService.SetPollingTier(userId, Reduced);
                }

                return;
            }

            // If we have a status, ensure we are in the Active PollingTier
            if (pollingGroup == Reduced.Group)
            {
                await _jobService.SetPollingTier(userId, Active);
            }

            Console.WriteLine("Track: " + status.Track);

            // Send the user's Spotify status to all connections
            string statusJson = JsonSerializer.Serialize(status);
            byte[] bytes = Encoding.UTF8.GetBytes(statusJson);
            var goneConnectionIds = new List<string>();

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
                    goneConnectionIds.Add(connectionId);
                }
            });

            await Task.WhenAll(posts);

            // Remove all Connections that are gone
            if (goneConnectionIds.Count > 0)
            {
                var removeResult = await _statusConnectionService.RemoveConnectionIds(userId, goneConnectionIds);

                if (removeResult.IsFailure)
                {
                    throw new Exception(removeResult.ErrorMessage);
                }
                
                int count = removeResult.Data;

                // Unschedule the job if there are no more connections
                if (count == 0)
                {
                    await _jobService.Unschedule(userId, pollingGroup);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Job failed.\n{e.Message}");
            await _jobService.Unschedule(userId, pollingGroup);
        }
    }
}
