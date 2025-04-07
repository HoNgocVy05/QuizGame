using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseWebSockets();

app.MapGet("/ws/{topic}", async (HttpContext context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        string topic = context.Request.RouteValues["topic"]?.ToString() ?? "default";
        await HandleWebSocketConnection(webSocket, topic);
    }
    else {
        context.Response.StatusCode = 400;
    }
});

app.Run("http://localhost:5030");

async Task HandleWebSocketConnection(WebSocket webSocket, string topic) {
    var questions = GetQuestions(topic);
    int score = 0;
    int index = 0;

    while (index < questions.Count) {
        var question = questions [index];
        string json = JsonSerializer.Serialize(question);
        await webSocket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);

        var buffer = new byte[1024];
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        if (result.MessageType == WebSocketMessageType.Close) {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client đóng kết nối", CancellationToken.None);
            return;
        }

        string answerStr = Encoding.UTF8.GetString(buffer, 0, result.Count);
        if (int.TryParse(answerStr, out int answerIndex) && answerIndex == question.CorrectIndex)
            score++;

        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        string nextCommand = Encoding.UTF8.GetString(buffer, 0, result.Count);
        if (nextCommand.Trim() == "next")
            index++;
    }

    string finalSore = $"score:{score}";
    await webSocket.SendAsync(Encoding.UTF8.GetBytes(finalSore), WebSocketMessageType.Text, true, CancellationToken.None);
    await Task.Delay(2000);
    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Trò chơi kết thúc" , CancellationToken.None);
}

List<Question> GetQuestions(string topic)
{
    var mathQuestions = new List<Question>
    {
        new Question("5 + 3 =", new[] { "6", "7", "8", "9" }, 2),
        new Question("12 ÷ 4 =", new[] { "2", "3", "4", "5" }, 1),
        new Question("6 - 2 =", new[] { "2", "4", "6", "8" }, 1),
        new Question("9 × 3 =", new[] { "18", "24", "27", "30" }, 2),
        new Question("12 ÷ 6 =", new[] { "0", "1", "2", "3" }, 2)
    };

    var programmingQuestions = new List<Question>
    {
        new Question("How do you center text in CSS?", new[] { "text-align: left;", "text-align: center;", "align-text: center;", "center-text: true;" }, 1),
        new Question("How do you insert a comment in CSS?", new[] { "<!-- comment -->", "// comment", "/* comment */", "# comment" }, 2),
        new Question("Which tag is used to insert an image in HTML?", new[] { "<img>", "<image>", "<picture>", "<src>" }, 0),
        new Question("Which CSS property controls the size of text?", new[] { "font-weight", "font-size", "text-style", "text-size" }, 1),
        new Question("What is the correct HTML element for the largest heading?", new[] { "<head>", "<h6>", "<heading>", "<h1>" }, 3)
    };

    var englishQuestions = new List<Question>
    {
        new Question("She is very _______ at playing the piano.", new[] { "good", "badly", "slow", "quickly" }, 0),
        new Question("The cat is sitting _______ the table.", new[] { "under", "on", "in", "above" }, 1),
        new Question("He is _______ than his brother.", new[] { "short", "tall", "more tall", "taller" }, 3),
        new Question("We _______ go to the park tomorrow.", new[] { "will", "is", "are", "can" }, 0),
        new Question("He _______ to the gym every morning.", new[] { "go", "going", "goes", "gone" }, 2)
    };

    return topic switch
    {
        "math" => mathQuestions,
        "programming" => programmingQuestions,
        "english" => englishQuestions,
        _ => new List<Question> { new Question("Chủ đề không tồn tại!", new[] { "OK" }, 0) }
    };
}

class Question {
    public string QuestionText {get; set;}
    public string[] Options {get; set;}
    public int CorrectIndex {get; set;}

    public Question(string question, string[] options, int correctIndex) {
        QuestionText = question;
        Options = options;
        CorrectIndex = correctIndex;
    }
}
