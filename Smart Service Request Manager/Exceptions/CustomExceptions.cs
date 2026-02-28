namespace Smart_Service_Request_Manager.Exceptions;

public class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string message) : base(message) { }
}

public class ServiceValidationException : Exception
{
    public ServiceValidationException(string message) : base(message) { }
}

public class ServiceOperationException : Exception
{
    public ServiceOperationException(string message) : base(message) { }
}
