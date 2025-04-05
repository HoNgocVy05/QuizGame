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

    var scienceQuestions = new List<Question>
    {
        new Question("Mặt Trời là gì?", new[] { "Một hành tinh", "Một ngôi sao", "Một vệ tinh", "Một thiên hà" }, 1),
        new Question("Con người cần khí gì để thở?", new[] { "Carbon dioxide", "Nitơ", "Oxy", "Hydro" }, 2),
        new Question("Nước sôi ở bao nhiêu độ C?", new[] { " 100°C", " 80°C", " 90°C", " 70°C" }, 0),
        new Question("Trái Đất quay quanh gì?", new[] { "Mặt Trăng", "Mặt Trời", "Sao Hỏa", "Sao Kim" }, 1),
        new Question("Cơ quan nào giúp con người hít thở?", new[] { "Tim", "Gan", "Dạ dày", "Phổi" }, 3)
    };

    var englishQuestions = new List<Question>
    {
        new Question("apple", new[] { "táo", "cam", "xoài", "nho" }, 0),
        new Question("ten", new[] { "5", "10", "9", "1" }, 1),
        new Question("kitchen", new[] { "phòng tắm", "phòng ngủ", "phòng vệ sinh", "phòng bếp" }, 3),
        new Question("blue", new[] { "màu xanh", "màu đỏ", "màu tím", "màu vàng" }, 0),
        new Question("car", new[] { "xe buýt", "xe tải", "xe ô tô", "xe máy" }, 2)
    };

    return topic switch
    {
        "math" => mathQuestions,
        "science" => scienceQuestions,
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
