public class PriorityAttribute : Attribute { 
    public int Level { get; } 
    public PriorityAttribute(int level) => Level = level; 
}