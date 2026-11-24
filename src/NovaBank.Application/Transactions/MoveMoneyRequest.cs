namespace NovaBank.Application.Transactions;

// Para yatırma ve çekme isteği.
public sealed record MoveMoneyRequest(decimal Amount);
