namespace Shared.Exceptions;

using System;

public class NotFoundException(string message) : Exception(message);

public class BusinessRuleException(string message) : Exception(message);

public class IllegalValuesException(string message) : Exception(message);

public class InvalidTournamentDataException(string message) : Exception(message);