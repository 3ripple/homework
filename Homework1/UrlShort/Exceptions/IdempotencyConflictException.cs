namespace UrlShort.Exceptions;

public class IdempotencyConflictException() : Exception("Idempotency key is already used with another url");