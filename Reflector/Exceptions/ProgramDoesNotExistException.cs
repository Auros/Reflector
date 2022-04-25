namespace Reflector.Exceptions;

internal class ProgramDoesNotExistException : Exception
{
    public ProgramDoesNotExistException(string applicationName) : base($"'{applicationName}' could not be found. Make sure you're configuration has the correct file paths.")
    {
        
    }
}