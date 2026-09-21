namespace Licensing.Application.Common;

public class ApplicationException : Exception
{
    public ApplicationException(string message) : base(message) { }
}

public class NotFoundException : ApplicationException
{
    public NotFoundException(string message) : base(message) { }
}

public class ConflictException : ApplicationException
{
    public ConflictException(string message) : base(message) { }
}

public class ValidationException : ApplicationException
{
    public ValidationException(string message) : base(message) { }
}

public class ForbiddenException : ApplicationException
{
    public ForbiddenException(string message) : base(message) { }
}
