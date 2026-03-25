[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class TimeoutAttribute : Attribute
{
    public int Milliseconds { get; }
 
    public TimeoutAttribute(int milliseconds)
    {
            Milliseconds = milliseconds;
    }
}