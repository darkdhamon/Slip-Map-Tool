namespace StarWin.Application.Services;

public interface IStarWinSearchService
{
    IReadOnlyList<StarWinSearchResult> Search(string query, int maxResults = 30);
}
