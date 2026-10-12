namespace TeamContributionManagementSystem.Application.Exceptions;

public class RecordDeletedException : Exception
{
    public RecordDeletedException(string message = "This record no longer exists. Please refresh the page.")
        : base(message)
    {
    }
}

public class RecordModifiedException : Exception
{
    public RecordModifiedException(string message = "This record was updated by another user. Please refresh and try again.")
        : base(message)
    {
    }
}

public class AlreadyProcessedException : Exception
{
    public AlreadyProcessedException(string message = "This action has already been completed.")
        : base(message)
    {
    }
}
