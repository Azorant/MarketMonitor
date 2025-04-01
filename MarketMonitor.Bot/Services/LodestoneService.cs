using NetStone;
using NetStone.Model.Parseables.Character;
using NetStone.Model.Parseables.Search.Character;
using NetStone.Search.Character;

namespace MarketMonitor.Bot.Services;

public class LodestoneService(CacheService cacheService)
{
    public async Task<CharacterSearchEntry?> SearchCharacterAsync(string name, string dc)
    {
        var lodestoneClient = await LodestoneClient.GetClientAsync();
        var searchResponse = await lodestoneClient.SearchCharacter(new CharacterSearchQuery()
        {
            CharacterName = name,
            DataCenter = dc
        });
        if (searchResponse == null || searchResponse.Results.Count() != 1) return null;
        return searchResponse.Results.First();
    }

    public async Task<LodestoneCharacter?> FetchCharacterAsync(string id)
    {
        var lodestoneClient = await LodestoneClient.GetClientAsync();
        return await lodestoneClient.GetCharacter(id);
    }

    public async Task<string?> FetchAvatarAsync(string name)
    {
        var cached = await cacheService.GetAvatar(name);
        if (cached != null) return cached;
        var result = await SearchCharacterAsync(name, "Crystal");
        if (result == null) return null;
        var character = await result.GetCharacter();
        var avatar = character?.Avatar?.ToString();
        if (avatar != null) await cacheService.SetAvatar(name, avatar);
        return avatar;
    }
}