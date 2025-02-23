using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using AnthemAPI.Common;
using AnthemAPI.Models;
using static AnthemAPI.Common.Constants;

namespace AnthemAPI.Services;

public class UsersService
{
    private readonly IAmazonDynamoDB _client;
    private readonly DynamoDBContext _context;
    private const string TABLE_NAME = "Users";

    public UsersService(IAmazonDynamoDB client)
    {
        _client = client;
        _context = new DynamoDBContext(client);
    }

    public async Task<ServiceResult<User?>> Load(string userId)
    {
        try
        {
            var user = await _context.LoadAsync<User>(userId);
            return ServiceResult<User?>.Success(user);
        }
        catch (Exception e)
        {
            return ServiceResult<User?>.Failure(e, $"Failed to load for {userId}.", "UsersService.Load()");
        }
    }

    public async Task<ServiceResult<User>> Save(User user)
    {
        try
        {
            await _context.SaveAsync(user);
            return ServiceResult<User>.Success(user);
        }
        catch (Exception e)
        {
            return ServiceResult<User>.Failure(e, $"Failed to save for {user.Id}.", "UsersService.Save()");
        }
    }

    public async Task<ServiceResult<User?>> AddChatIdToAll(List<string> userIds, string chatId)
    {
        try
        {
            var batch = new BatchExecuteStatementRequest
            {
                Statements = userIds.Select(userId => new BatchStatementRequest
                {
                    Statement = $"UPDATE {TABLE_NAME}" +
                                " ADD ChatIds ?" +
                                " WHERE Id = ?",
                    Parameters = new List<AttributeValue>
                    {
                        new AttributeValue { SS = [chatId] },
                        new AttributeValue { S = userId }
                    }
                }).ToList()
            };

            await _client.BatchExecuteStatementAsync(batch);

            return ServiceResult<User?>.Success(null);
        }
        catch (Exception e)
        {
            return ServiceResult<User?>.Failure(e, $"Failed to add chat id.", "UsersService.AddChatIdToAll()");
        }
    }

    public async Task<ServiceResult<User?>> RemoveChatIdFromAll(List<string> userIds, string chatId)
    {
        try
        {
            var batch = new BatchExecuteStatementRequest
            {
                Statements = userIds.Select(userId => new BatchStatementRequest
                {
                    Statement = $"UPDATE {TABLE_NAME}" +
                                " DELETE ChatIds ?" +
                                " WHERE Id = ?",
                    Parameters = new List<AttributeValue>
                    {
                        new AttributeValue { SS = [chatId] },
                        new AttributeValue { S = userId }
                    }
                }).ToList()
            };

            await _client.BatchExecuteStatementAsync(batch);

            return ServiceResult<User?>.Success(null);
        }
        catch (Exception e)
        {
            return ServiceResult<User?>.Failure(e, $"Failed to remove chat id.", "UsersService.RemoveChatIdFromAll()");
        }
    }

    public async Task<ServiceResult<User>> Update(string userId, UserUpdate userUpdate)
    {
        try
        {
            var setExpressions = new List<string>();
            var removeExpressions = new List<string>();
            var values = new Dictionary<string, AttributeValue>();

            if (userUpdate.Nickname is not null)
            {
                setExpressions.Add("Nickname = :nickname");
                values.Add(":nickname", new AttributeValue { S = userUpdate.Nickname });
            }
            else
            {
                removeExpressions.Add("Nickname");
            }

            if (userUpdate.PictureUrl is not null)
            {
                setExpressions.Add("PictureUrl = :pictureUrl");
                values.Add(":pictureUrl", new AttributeValue { S = userUpdate.PictureUrl });
            }
            else
            {
                removeExpressions.Add("PictureUrl");
            }

            if (userUpdate.Bio is not null)
            {
                setExpressions.Add("Bio = :bio");
                values.Add(":bio", new AttributeValue { S = userUpdate.Bio });
            }
            else
            {
                removeExpressions.Add("Bio");
            }

            if (userUpdate.Anthem is not null)
            {
                setExpressions.Add("Anthem = :anthem");
                values.Add(":anthem", new AttributeValue
                {
                    M = new Dictionary<string, AttributeValue>
                    {
                        ["Uri"] = new AttributeValue { S = userUpdate.Anthem!.Uri },
                        ["Name"] = new AttributeValue { S = userUpdate.Anthem.Name },
                        ["Artists"] = new AttributeValue
                        {
                            L = userUpdate.Anthem.Artists.Select(artist => new AttributeValue
                            {
                                M = new Dictionary<string, AttributeValue>
                                {
                                    ["Uri"] = new AttributeValue { S = artist.Uri },
                                    ["Name"] = new AttributeValue { S = artist.Name }
                                }
                            }).ToList()
                        },
                        ["Album"] = new AttributeValue
                        {
                            M = new Dictionary<string, AttributeValue>
                            {
                                ["Uri"] = new AttributeValue { S = userUpdate.Anthem.Album.Uri },
                                ["ImageUrl"] = new AttributeValue { S = userUpdate.Anthem.Album.ImageUrl }
                            }
                        }
                    }
                });
            }
            else
            {
                removeExpressions.Add("Anthem");
            }

            string updateExpression = "";
            updateExpression += setExpressions.Count > 0
                ? "SET " + string.Join(", ", setExpressions)
                : "";
            updateExpression += removeExpressions.Count > 0
                ? " REMOVE " + string.Join(", ", removeExpressions)
                : "";
            
            var request = new UpdateItemRequest
            {
                TableName = TABLE_NAME,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "Id", new AttributeValue { S = userId } }
                },
                UpdateExpression = updateExpression,
                ExpressionAttributeValues = values,
                ReturnValues = ReturnValue.ALL_NEW
            };

            var response = await _client.UpdateItemAsync(request);

            var user = new User
            {
                Id = response.Attributes["Id"].S,
                MusicProvider = (MusicProvider) int.Parse(response.Attributes["MusicProvider"].N),
                Nickname = response.Attributes.ContainsKey("Nickname")
                    ? response.Attributes["Nickname"].S
                    : null,
                PictureUrl = response.Attributes.ContainsKey("PictureUrl")
                    ? response.Attributes["PictureUrl"].S
                    : null,
                Bio = response.Attributes.ContainsKey("Bio")
                    ? response.Attributes["Bio"].S
                    : null,
                Anthem = response.Attributes.ContainsKey("Anthem")
                    ? new Track
                    {
                        Uri = response.Attributes["Anthem"].M["Uri"].S,
                        Name = response.Attributes["Anthem"].M["Name"].S,
                        Artists = response.Attributes["Anthem"].M["Artists"].L.Select(artist => new Artist
                        {
                            Name = artist.M["Name"].S,
                            Uri = artist.M["Uri"].S
                        }).ToList(),
                        Album = new Album
                        {
                            Artists = response.Attributes["Anthem"].M["Album"].M["Artists"].L.Select(artist => new Artist
                            {
                                Name = artist.M["Name"].S,
                                Uri = artist.M["Uri"].S
                            }).ToList(),
                            Name = response.Attributes["Anthem"].M["Album"].M["Name"].S,
                            ImageUrl = response.Attributes["Anthem"].M["Album"].M["ImageUrl"].S,
                            Uri = response.Attributes["Anthem"].M["Album"].M["Uri"].S,
                        }
                    }
                    : null,
                ChatIds = response.Attributes["ChatIds"].SS.Select(chatId => chatId).ToList().ToHashSet()
            };

            return ServiceResult<User>.Success(user);
        }
        catch (Exception e)
        {
            return ServiceResult<User>.Failure(e, $"Failed to update user {userId}.", "UsersService.Update()");
        }
    }

    public async Task<ServiceResult<User?>> UpdateMusicProvider(string userId, MusicProvider musicProvider)
    {
        try
        {
            var request = new UpdateItemRequest
            {
                TableName = TABLE_NAME,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "Id", new AttributeValue { S = userId } }
                },
                UpdateExpression = "SET MusicProvider = :musicProvider",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":musicProvider"] = new AttributeValue { N = ((int) musicProvider).ToString() }
                },
                ReturnValues = ReturnValue.ALL_NEW
            };

            var response = await _client.UpdateItemAsync(request);

            return ServiceResult<User?>.Success(null);
        }
        catch (Exception e)
        {
            return ServiceResult<User?>.Failure(e, $"Failed to update music provider for {userId}.", "UsersService.UpdateMusicProvider()");
        }
    }

    public async Task<ServiceResult<List<Card>>> GetCards(HashSet<string> userIds)
    {
        try
        {
            var batches = new List<BatchGet<User>>();

            for (int i = 0; i < userIds.Count; i += DYNAMO_DB_BATCH_GET_LIMIT)
            {
                List<string> ids  = userIds.Skip(i).Take(DYNAMO_DB_BATCH_GET_LIMIT).ToList();
                var batch = _context.CreateBatchGet<User>();
                ids.ForEach(batch.AddKey);
                batches.Add(batch);
            }

            await _context.ExecuteBatchGetAsync(batches.ToArray());

            List<Card> cards = batches
                .SelectMany(batch => batch.Results)
                .ToList()
                .Select(user => new Card
                {
                    UserId = user.Id,
                    Nickname = user.Nickname,
                    PictureUrl = user.PictureUrl
                })
                .ToList();
            
            return ServiceResult<List<Card>>.Success(cards);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Card>>.Failure(e, "Failed to get cards.", "UsersService.GetCards");
        }
    }
}
