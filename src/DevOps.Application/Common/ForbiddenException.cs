using DevOps.Application.Common;

namespace DevOps.Application.Common;

public sealed class ForbiddenException(string message) : Exception(message);
