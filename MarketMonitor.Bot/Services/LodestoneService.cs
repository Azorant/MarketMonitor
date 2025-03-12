using NetStone;
using NetStone.Model.Parseables.Character;
using NetStone.Model.Parseables.Search.Character;
using NetStone.Search.Character;

namespace MarketMonitor.Bot.Services;

public class LodestoneService
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
}