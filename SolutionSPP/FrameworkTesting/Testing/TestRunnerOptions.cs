public class TestRunnerOptions
{
    private int _maxDegreeOfParallelism = 4;
    public int MaxDegreeOfParallelism
    {
        get => _maxDegreeOfParallelism;
        set => _maxDegreeOfParallelism = value < 1 ? 1 : value;
    }
 
    public bool ParallelizeMethods { get; set; } = false;
}