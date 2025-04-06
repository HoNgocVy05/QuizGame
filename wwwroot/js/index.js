// Lấy topic từ URL để kết nối đến WebSocket server
const urlParams = new URLSearchParams(window.location.search);
const topic = urlParams.get("topic");

// Khởi tạo kết nối WebSocket
const socket = new WebSocket(`ws://localhost:5030/ws/${topic}`);
let waitingForNext = false;

// Sự kiện khi kết nối thành công
socket.onopen = () => {
    console.log("WebSocket đã kết nối!");
};

// Sự kiện khi nhận dữ liệu từ server
socket.onmessage = (event) => {
    if (waitingForNext) return;
    
    console.log("Nhận dữ liệu:", event.data);

    // Nếu dữ liệu bắt đầu bằng "score", tức là trò chơi đã kết thúc
    if (event.data.startsWith("score")) {
        document.body.innerHTML = `
            <h1>Điểm của bạn: ${event.data.split(":")[1]}</h1>
            <button onclick="window.location.href='index.html'">Quay lại trang chủ</button>
        `;
        return;
    }

    // Nếu không phải "score", là câu hỏi => hiển thị câu hỏi
    const data = JSON.parse(event.data);
    showQuestion(data);
};

// Hiển thị câu hỏi và các đáp án
function showQuestion(data) {
    document.getElementById("question").innerText = data.QuestionText;

    const optionsDiv = document.getElementById("options");
    optionsDiv.innerHTML = "";

    // Duyệt qua từng đáp án và tạo nút bấm
    data.Options.forEach((answer, index) => {
        const button = document.createElement("button");
        button.innerText = answer;
        button.classList.add("answer");
        button.onclick = () => handleAnswer(button, index, data.CorrectIndex);
        optionsDiv.appendChild(button);
    });

    // Đảm bảo không nhấn quá nhanh liên tục
    setTimeout(() => {
        waitingForNext = false;
    }, 2000);
}

// Xử lý khi người dùng chọn một đáp án
function handleAnswer(button, selectedIndex, correctIndex) {
    if (waitingForNext) return;

    waitingForNext = true;

    // Vô hiệu hóa các nút sau khi chọn
    document.querySelectorAll(".answer").forEach(btn => btn.disabled = true);
    if (selectedIndex === correctIndex){
        button.classList.add("correct"); 
    } else {
        button.classList.add("wrong");
        document.querySelectorAll(".answer")[correctIndex].classList.add("correct");
    }

    // Gửi đáp án đã chọn lên server sau 1.5 giây
    setTimeout(() => {
        socket.send(selectedIndex);

        // Sau khi gửi đáp án xong, gửi yêu cầu câu tiếp theo
        setTimeout(() => {
            if (document.getElementById("question")) {
                socket.send("next");
            }
            waitingForNext = false;
        }, 500);
    }, 1500);
}
