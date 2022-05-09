namespace ZapWord.Client.Interfaces;

using ZapWord.Shared.Models;

public interface IGameState
{
    bool GameRequested { get; }
    bool GameActive { get; }
    ZapWordModel? GameData { get; }
    Dictionary<int, AvailableLetter> AvailableLetters { get; }
    List<TypedLetter> TypedLetters { get; }
    WordState CurrentWordState { get; }
    WordHint? CurrentHint { get; }
    List<string> CorrectWords { get; }
    List<string> MissedWords { get; }
    event Action? GameStateChanged;
    Task NewGame();
    Task ResetGame();
    void GiveUp();
    void Shuffle();
    void NextHint();
    void UseLetter(int index);
    void DeleteLetter(int index);
    void FinishWord();
}