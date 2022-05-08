namespace ZapWord.Client;

public readonly record struct AvailableLetter(char Letter, bool Available);
public readonly record struct TypedLetter(int Index, char Letter);
public readonly record struct WordHint(string Word, string Category, string Description);