using System.Collections.Generic;
using System.Text;

public class ConversationEntry
{
    public string Role { get; set; }
    public string Content { get; set; }

    public ConversationEntry(string role, string content)
    {
        Role = role;
        Content = content;
    }
}

public class Conversation
{
    private List<ConversationEntry> entries;

    public Conversation()
    {
        entries = new List<ConversationEntry>();
    }

    public void AddEntry(string role, string content)
    {
        entries.Add(new ConversationEntry(role, content));
    }

    public string GetFormattedConversation()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var entry in entries)
        {
            sb.AppendLine($"{entry.Role}: {entry.Content}");
        }
        return sb.ToString();
    }
}

public class RAGPromptBuilder
{
    private Conversation conversation;
    private List<string> context;
    private string systemMessage;
    private string assistantRole;
    private string question;
    
    public RAGPromptBuilder()
    {
        conversation = new Conversation();
        context = new List<string>();
        systemMessage = "";
        assistantRole = "";
        question = "";
    }

    public void AddSystem(string message)
    {
        systemMessage = message;
    }
    
    public void SetAssistantRole(string role)
    {
        assistantRole = role;
    }

    public void AddContext(string document)
    {
        context.Add(document);
    }
    
    public void ClearContext()
    {
        context.Clear();
    }    

    public void AddConversationEntry(string role, string content)
    {
        conversation.AddEntry(role, content);
    }

    public void UpdateQuestion(string role, string content)
    {
        question = $"{role}: {content}";
    }

    public string BuildPrompt()
    {
        StringBuilder prompt = new StringBuilder();
        
        if (!string.IsNullOrEmpty(systemMessage))
        {
            prompt.AppendLine($"### System ###\n{systemMessage}\n");
        }
        
        if (!string.IsNullOrEmpty(assistantRole))
        {
            prompt.AppendLine($"### Assistant Role ###\n{assistantRole}\n");
        }
        
        if (context.Count > 0)
        {
            prompt.AppendLine("### Context ###");
            foreach (var doc in context)
            {
                prompt.AppendLine(doc);
            }
            prompt.AppendLine();
        }

        string conversationHistory = conversation.GetFormattedConversation();
        if (!string.IsNullOrEmpty(conversationHistory))
        {
            prompt.AppendLine("### Conversation History ###");
            prompt.Append(conversationHistory);
            prompt.AppendLine();
        }

        prompt.AppendLine("### Question ###");
        prompt.AppendLine(question);

        return prompt.ToString();
    }
}