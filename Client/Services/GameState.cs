namespace ZapWord.Client.Services;

using System.Net.Http.Json;
using ZapWord.Shared.Classes;
using ZapWord.Shared.Models;

public class GameState : IGameState
{
    public bool GameRequested { get; private set; }
    public bool GameActive { get; private set; }
    public ZapWordModel? GameData { get; private set; }
    public Dictionary<int, AvailableLetter> AvailableLetters { get; init; }
    public List<TypedLetter> TypedLetters { get; init; }
    public WordState CurrentWordState { get; private set; }
    public WordHint? CurrentHint { get; private set; }
    public List<string> CorrectWords { get; init; }
    public List<string> MissedWords { get; init; }
    public int Progress => GetProgress();

    private event EventHandler? GameStateChanged;
    private readonly object GameStateChangedLock = new();
    private readonly List<string> availableWords;
    private readonly HttpClient httpClient;

    public GameState(HttpClient http_client)
    {
        httpClient = http_client;
        GameRequested = true;
        GameActive = false;
        AvailableLetters = new();
        TypedLetters = new();
        CurrentWordState = WordState.ewsTyping;
        CurrentHint = null;
        CorrectWords = new();
        MissedWords = new();
        availableWords = new();
    }

    public async Task NewGame()
    {
        GameData = await httpClient.GetFromJsonAsync<ZapWordModel>("ZapWord")!;
        availableWords.AddRange(GameData!.Words.Select(s => s.Key));
        ShuffleInternal();
        NextHintInternal();
        GameActive = true;
        GameRequested = false;
    }

    public async Task ResetGame()
    {
        GameActive = false;
        AvailableLetters.Clear();
        TypedLetters.Clear();
        CorrectWords.Clear();
        MissedWords.Clear();
        availableWords.Clear();
        await NewGame();
        CurrentWordState = WordState.ewsTyping;
        GameStateChange();
    }

    public void GiveUp()
    {
        CorrectWords.AddRange(availableWords);
        MissedWords.AddRange(availableWords);
        availableWords.Clear();
        CurrentWordState = WordState.ewsGameOver;
    }

    public void Shuffle()
    {
        ShuffleInternal();
        GameStateChange();
    }

    private void ShuffleInternal()
    {
        AvailableLetters.Clear();
        var this_shuffle = ThreadSafeRandom.Next(0, GameData!.Letters.Count());
        var letter_index = 0;
        foreach (var letter in GameData!.Letters[this_shuffle])
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
        var semantics = GameData!.Words[word]!;
        var semantic = semantics[ThreadSafeRandom.Next(0, semantics.Count())];
        CurrentHint = new(word, semantic.Category, semantic.Description);
    }

    public void UseLetter(int index)
    {
        AvailableLetters[index] = AvailableLetters[index] with { Available = false };
        TypedLetters.Add(new(index, AvailableLetters[index].Letter));
        CurrentWordState = WordState.ewsTyping;
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
        CurrentWordState = WordState.ewsTyping;
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
            if (GameData!.Words.Count() == 0)
            {
                CurrentWordState = WordState.ewsGameOver;
            }
            else
            {
                CurrentWordState = WordState.ewsTyping;
                NextHintInternal();
            }
        }
        else
        {
            CurrentWordState = WordState.ewsWrong;
        }
        GameStateChange();
    }

    private int GetProgress()
    {
        if (CurrentWordState == WordState.ewsGameOver)
        {
            return 100;
        }
        else
        {
            return (int)Math.Round((double)(CorrectWords.Count()) / (double)(availableWords.Count() + CorrectWords.Count()) * 100);
        }
    }

    private void GameStateChange()
    {
        GameStateChanged?.Invoke(this, EventArgs.Empty);
    }

    event EventHandler? IGameState.GameStateChanged
    {
        add
        {
            lock (GameStateChangedLock)
            {
                GameStateChanged += value;
            }
        }
        remove
        {
            lock (GameStateChangedLock)
            {
                GameStateChanged -= value;
            }
        }
    }
}
