document.addEventListener("DOMContentLoaded", () => {
    const mensagens = document.getElementById("mensagens");
    const textarea = document.getElementById("mensagemInput");
    const botao = document.getElementById("enviarBtn");

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .build();

    connection.start()
        .then(() => connection.invoke("EntrarNoTicket", ticketId))
        .catch(err => console.error(err.toString()));

    connection.on("ReceberMensagem", (autor, texto) => {
        const msgDiv = document.createElement("div");
        msgDiv.innerHTML = `<strong>${autor}:</strong> ${texto}`;
        mensagens.appendChild(msgDiv);
        mensagens.scrollTop = mensagens.scrollHeight;
    });

    botao.addEventListener("click", () => {
        const texto = textarea.value.trim();
        if (!texto) return;

        connection.invoke("EnviarMensagem", ticketId, usuario, texto)
            .catch(err => console.error(err.toString()));

        textarea.value = "";
        textarea.focus();
    });

    textarea.addEventListener("keypress", e => {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            botao.click();
        }
    });
});
