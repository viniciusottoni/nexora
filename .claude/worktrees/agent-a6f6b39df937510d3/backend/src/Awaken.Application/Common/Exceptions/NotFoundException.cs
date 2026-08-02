namespace Awaken.Application.Common.Exceptions;

public class NotFoundException(string name, object key)
    : Exception($"Entity '{name}' with key '{key}' was not found.");
