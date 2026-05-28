namespace ZoroDBRestore.Models;

public record OperationResult(bool Success, string Message, string? FullOutput = null);
