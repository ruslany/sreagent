 public class Tool
    {
        public string Name { get; }
        public string Description { get; }
        
        public Tool(string name, string description)
        {
            Name = name;
            Description = description;
        }
        
        // In a real implementation, this would have methods to execute the tool functionality
    }
    