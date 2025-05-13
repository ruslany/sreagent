namespace sreagent;

public class ConversationState
{
    private List<Message> _messages = new List<Message>();
    private string _currentPhase = "initial";
    private string? _currentCategory = null;
    private string? _diagnosisResult = null;

    public void AddUserMessage(string content)
    {
        _messages.Add(new Message { Role = "user", Content = content });
    }

    public void AddAgentMessage(string content)
    {
        _messages.Add(new Message { Role = "agent", Content = content });
    }

    public void SetCurrentPhase(string phase)
    {
        _currentPhase = phase;
    }

    public void SetCurrentCategory(string category)
    {
        _currentCategory = category;
    }

    public void SetDiagnosisResult(string result)
    {
        _diagnosisResult = result;
    }

    public string GetDiagnosisResult()
    {
        return _diagnosisResult ?? "";
    }

    public string GetFormattedState()
    {
        // Format the conversation state for inclusion in prompts
        var formattedState = new System.Text.StringBuilder();

        formattedState.AppendLine($"Current phase: {_currentPhase}");
        if (_currentCategory != null)
            formattedState.AppendLine($"Current category: {_currentCategory}");

        formattedState.AppendLine("Recent messages:");

        // Include last 5 messages for context
        int startIndex = Math.Max(0, _messages.Count - 5);
        for (int i = startIndex; i < _messages.Count; i++)
        {
            var message = _messages[i];
            formattedState.AppendLine($"{message.Role}: {message.Content}");
        }

        return formattedState.ToString();
    }
}
