namespace ZapWord.Client.Services;

using System.Net.Http.Json;
using ZapWord.Shared.Classes;
using ZapWord.Shared.Models;

public class GameState : IGameState
{
    public bool GameRequested { get { return gameRequested; } }
    public bool GameActive { get { return gameActive; } }
    public ZapWordModel? GameData { get { return gameData; } }
    public Dictionary<int, AvailableLetter> AvailableLetters { get; }
    public List<TypedLetter> TypedLetters { get; }
    public WordState CurrentWordState { get { return currentWordState; } }
    public WordHint? CurrentHint { get { return currentHint; } }
    public List<string> CorrectWords { get; }
    public List<string> MissedWords { get; }
    public event Action? GameStateChanged;

    private bool gameRequested;
    private bool gameActive;
    private ZapWordModel? gameData;
    private WordState currentWordState;
    private WordHint? currentHint;
    private List<string> availableWords;

    private readonly HttpClient httpClient;

    public GameState(HttpClient http_client)
    {
        httpClient = http_client;
        gameRequested = true;
        gameActive = false;
        AvailableLetters = new();
        TypedLetters = new();
        currentWordState = WordState.ewsTyping;
        currentHint = null;
        CorrectWords = new();
        MissedWords = new();
        availableWords = new();
    }

    private void GameStateChange()
    {
        GameStateChanged?.Invoke();
    }

    public async Task NewGame()
    {
        gameData = await httpClient.GetFromJsonAsync<ZapWordModel>("ZapWord")!;
        availableWords.AddRange(gameData!.Words.Select(s => s.Key));
        ShuffleInternal();
        NextHintInternal();
        gameActive = true;
        gameRequested = false;
    }

    public async Task ResetGame()
    {
        gameActive = false;
        AvailableLetters.Clear();
        TypedLetters.Clear();
        CorrectWords.Clear();
        MissedWords.Clear();
        availableWords.Clear();
        await NewGame();
        currentWordState = WordState.ewsTyping;
        GameStateChange();
    }

    public void GiveUp()
    {
        CorrectWords.AddRange(availableWords);
        MissedWords.AddRange(availableWords);
        availableWords.Clear();
        currentWordState = WordState.ewsGameOver;
    }

    public void Shuffle()
    {
        ShuffleInternal();
        GameStateChange();
    }

    private void ShuffleInternal()
    {
        AvailableLetters.Clear();
        var this_shuffle = ThreadSafeRandom.Next(0, gameData!.Letters.Count());
        var letter_index = 0;
        foreach (var letter in gameData!.Letters[this_shuffle])
        {
            AvailableLetters.Add(letter_index++, new(letter, true));
        }
    }

    public void NextHint()
    {
        NextHintInternal();
        GameStateChange();
    }

    private void NextHintInternal()
    {
        var skip = ThreadSafeRandom.Next(0, availableWords.Count());
        var word = availableWords.Skip(skip).FirstOrDefault()!;
        var semantics = gameData!.Words[word]!;
        var semantic = semantics[ThreadSafeRandom.Next(0, semantics.Count())];
        currentHint = new(word, semantic.Category, semantic.Description);
    }

    public void UseLetter(int index)
    {
        AvailableLetters[index] = AvailableLetters[index] with { Available = false };
        TypedLetters.Add(new(index, AvailableLetters[index].Letter));
        currentWordState = WordState.ewsTyping;
        GameStateChange();
    }

    public void DeleteLetter(int index)
    {
        var delete = false;
        var typed_letters = TypedLetters.ToArray();
        foreach (var typed_letter in typed_letters)
        {
            if (delete || (typed_letter.Index == index))
            {
                delete = true;
                AvailableLetters[typed_letter.Index] = AvailableLetters[typed_letter.Index] with { Available = true };
                TypedLetters.RemoveAll(p => p.Index == typed_letter.Index);
            }
        }
        currentWordState = WordState.ewsTyping;
        GameStateChange();
    }

    private void DeleteAllLetters()
    {
        foreach (var typed_letter in TypedLetters)
        {
            AvailableLetters[typed_letter.Index] = AvailableLetters[typed_letter.Index] with { Available = true };
        }
        TypedLetters.Clear();
    }

    public void FinishWord()
    {
        var complete_word = new String(TypedLetters.Select(s => s.Letter).ToArray());
        if (availableWords.Contains(complete_word))
        {
            availableWords.Remove(complete_word);
            CorrectWords.Insert(0, complete_word);
            DeleteAllLetters();
            if (gameData!.Words.Count() == 0)
            {
                currentWordState = WordState.ewsGameOver;
            }
            else
            {
                currentWordState = WordState.ewsTyping;
                NextHintInternal();
            }
        }
        else
        {
            currentWordState = WordState.ewsWrong;
        }
        GameStateChange();
    }
}
