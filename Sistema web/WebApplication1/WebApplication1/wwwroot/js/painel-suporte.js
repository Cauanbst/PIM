// ===============================================
// 📋 painel-suporte.js — Painel de suporte com encerramento sincronizado
// ===============================================

// ==========================================================
// 🔹 VARIÁVEIS GLOBAIS
// ==========================================================
const chatWindows = {};
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

// ==========================================================
// 🔹 Quando a página carrega
// ==========================================================
document.addEventListener("DOMContentLoaded", () => {
    console.log("✅ painel-suporte.js carregado com sucesso!");

    // ======================================================
    // 🔹 Função: Mostrar notificações estilizadas (toast)
    // ======================================================
    function showToast(message, tipo = "info") {
        const toast = document.createElement("div");
        toast.className = `toast toast-${tipo}`;
        toast.innerHTML = `
            <div class="toast-content">
                <span class="toast-icon">
                    ${tipo === "success" ? "✅" : tipo === "error" ? "❌" : "💬"}
                </span>
                <div class="toast-text">${message}</div>
            </div>
        `;

        let background =
            tipo === "error"
                ? "linear-gradient(135deg, #1e3c72, #2a5298)"
                : tipo === "success"
                    ? "linear-gradient(135deg, #0044cc, #007bff)"
                    : "linear-gradient(135deg, #0f2027, #203a43, #2c5364)";

        Object.assign(toast.style, {
            position: "fixed",
            top: "20px",
            right: "20px",
            padding: "16px 22px",
            background,
            color: "#fff",
            borderRadius: "16px",
            boxShadow: "0 4px 15px rgba(0, 100, 255, 0.3)",
            zIndex: 9999,
            opacity: 0,
            transform: "translateX(120%) scale(0.9)",
            transition: "opacity 0.6s ease, transform 0.6s ease",
            fontFamily: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif",
            fontSize: "14px",
            maxWidth: "340px",
            lineHeight: "1.5",
            backdropFilter: "blur(6px)",
            borderLeft: "4px solid rgba(255,255,255,0.3)"
        });

        document.body.appendChild(toast);
        setTimeout(() => {
            toast.style.opacity = 1;
            toast.style.transform = "translateX(0) scale(1)";
        }, 100);
        setTimeout(() => {
            toast.style.opacity = 0;
            toast.style.transform = "translateX(120%) scale(0.9)";
            setTimeout(() => toast.remove(), 600);
        }, 4200);
    }

    // ======================================================
    // 🔹 Alternar seções do painel
    // ======================================================
    window.mostrarSecao = function (secao) {
        document.querySelectorAll(".chamados-section").forEach(div => (div.style.display = "none"));
        document.querySelectorAll(".sidebar nav button").forEach(btn => btn.classList.remove("ativo"));

        const el = document.getElementById(`${secao}-list`);
        if (el) el.style.display = "block";

        const botaoAtivo = document.querySelector(`button[onclick="mostrarSecao('${secao}')"]`);
        if (botaoAtivo) botaoAtivo.classList.add("ativo");
    };

    mostrarSecao("abertos");

    // ======================================================
    // 🔹 Atualizar contadores
    // ======================================================
    function atualizarContagem() {
        document.getElementById("count-abertos").textContent =
            document.getElementById("abertos-list").children.length;
        document.getElementById("count-andamento").textContent =
            document.getElementById("andamento-list").children.length;
        document.getElementById("count-fechados").textContent =
            document.getElementById("fechados-list").children.length;
    }

    // ======================================================
    // 🔹 Conexão com SignalR
    // ======================================================
    connection
        .start()
        .then(() => {
            console.log("✅ Conectado ao SignalR Hub!");
            document.querySelectorAll(".chamado-item").forEach(ticketEl => {
                const ticketId = ticketEl.id.replace("ticket-", "");
                connection.invoke("EntrarNoTicket", parseInt(ticketId));
            });
        })
        .catch(err => console.error("❌ Erro ao conectar SignalR:", err));

    // ======================================================
    // 🔹 Eventos SignalR
    // ======================================================
    connection.on("NovoChamado", data => {
        console.log("📩 Novo ticket recebido:", data);
        showToast(`🆕 ${data.title} criado!`, "success");

        const container = document.getElementById("abertos-list");
        if (!container || document.getElementById(`ticket-${data.id}`)) return;

        const novo = document.createElement("div");
        novo.className = "chamado-item chamado-card";
        novo.id = `ticket-${data.id}`;
        novo.innerHTML = `
            <h3>${data.title}</h3>
            <p>${data.description}</p>
            <div class="chamado-footer">
                <span>👤 ${data.criador}</span>
                <span>🛠️ ${data.tecnico}</span>
                <span>📅 ${new Date(data.dataCriacao).toLocaleString()}</span>
            </div>
            <div class="status-badge status-aberto">🟢 Criado</div>
        `;

        const btnAtender = document.createElement("button");
        btnAtender.className = "btn-atender";
        btnAtender.dataset.ticketId = data.id;
        btnAtender.textContent = "🏁 Iniciar Atendimento";
        btnAtender.addEventListener("click", () => iniciarAtendimento(data.id));

        novo.appendChild(btnAtender);
        container.prepend(novo);
        connection.invoke("EntrarNoTicket", data.id);
        atualizarContagem();
    });

    connection.on("ClienteRecusouEncerrar", ticketId => {
        showToast("⚠️ O cliente optou por continuar a conversa.", "info");
        console.log(`⚠️ Cliente recusou encerramento do ticket ${ticketId}`);
    });

    // ======================================================
    // ✅ Cliente confirmou encerramento (sem precisar F5)
    // ======================================================
    connection.on("ChatEncerrado", data => {
        const { ticketId, status = "Finalizado" } = data;
        console.log(`💬 Ticket ${ticketId} encerrado (status: ${status})`);

        const ticketEl = document.getElementById(`ticket-${ticketId}`);
        if (!ticketEl) return;

        showToast("✅ Atendimento encerrado!", "success");

        // Atualiza status visual
        const badge = ticketEl.querySelector(".status-badge");
        if (badge) {
            badge.textContent = "🔴 Finalizado";
            badge.className = "status-badge status-fechado";
        }

        // Remove botões antigos
        ticketEl.querySelectorAll(".btn-finalizar, .btn-reabrir, .btn-atender").forEach(btn => btn.remove());

        // Adiciona botão de visualização que abre em nova aba
        const visualizarBtn = document.createElement("button");
        visualizarBtn.className = "btn btn-visualizar";
        visualizarBtn.textContent = "👁️ Visualizar Conversa";
        visualizarBtn.addEventListener("click", () => {
            window.open(`/Chat/AbrirChat?ticketId=${ticketId}&modo=leitura`, "_blank");
        });
        ticketEl.appendChild(visualizarBtn);

        // Move automaticamente para aba "Finalizados"
        const listaFechados = document.getElementById("fechados-list");
        if (listaFechados) listaFechados.prepend(ticketEl);

        atualizarContagem();

        // Fecha janela de chat se estiver aberta
        if (chatWindows[ticketId] && !chatWindows[ticketId].closed) {
            chatWindows[ticketId].close();
            delete chatWindows[ticketId];
        }
    });

    // ======================================================
    // 🔹 Funções principais
    // ======================================================
    window.iniciarAtendimento = async function (ticketId) {
        const ticketEl = document.getElementById(`ticket-${ticketId}`);
        if (!ticketEl) return showToast("⚠️ Ticket não encontrado", "error");

        try {
            const resp = await fetch(`/Tickets/IniciarTicket?id=${ticketId}`, { method: "POST" });
            const data = await resp.json();
            if (!data?.success) return showToast("Falha ao iniciar atendimento", "error");

            const statusBadge = ticketEl.querySelector(".status-badge");
            if (statusBadge) {
                statusBadge.textContent = "🟡 Em Andamento";
                statusBadge.className = "status-badge status-andamento";
            }

            const btnAtender = ticketEl.querySelector(".btn-atender");
            if (btnAtender) btnAtender.remove();

            const footerBtns = document.createElement("div");
            footerBtns.style.display = "flex";
            footerBtns.style.gap = "10px";
            footerBtns.style.marginTop = "10px";

            const btnReabrir = document.createElement("button");
            btnReabrir.className = "btn btn-reabrir";
            btnReabrir.textContent = "💬 Reabrir Conversa";
            btnReabrir.addEventListener("click", () => {
                chatWindows[ticketId] = window.open(`/Chat/AbrirChat?ticketId=${ticketId}&modo=escrita`, `_chat-${ticketId}`, "noopener,noreferrer");
            });

            const btnFinalizar = document.createElement("button");
            btnFinalizar.className = "btn-finalizar";
            btnFinalizar.dataset.ticketId = ticketId;
            btnFinalizar.textContent = "✅ Finalizar";
            btnFinalizar.addEventListener("click", () => solicitarEncerramento(ticketId));

            footerBtns.appendChild(btnReabrir);
            footerBtns.appendChild(btnFinalizar);
            ticketEl.appendChild(footerBtns);

            document.getElementById("andamento-list").prepend(ticketEl);
            atualizarContagem();
            showToast(`🚀 Ticket ${ticketId} em andamento!`, "info");

            if (!chatWindows[ticketId] || chatWindows[ticketId].closed) {
                const chatUrl = `/Chat/AbrirChat?ticketId=${ticketId}&modo=escrita`;
                chatWindows[ticketId] = window.open(chatUrl, `_chat-${ticketId}`, "noopener,noreferrer");
            }
        } catch (err) {
            console.error(err);
            showToast("💥 Erro ao iniciar atendimento", "error");
        }
    };

    // ======================================================
    // 🔹 Solicitar encerramento (técnico)
    // ======================================================
    async function solicitarEncerramento(ticketId) {
        const ticketEl = document.getElementById(`ticket-${ticketId}`);
        if (!ticketEl) return showToast("⚠️ Ticket não encontrado", "error");

        if (!confirm("Deseja solicitar o encerramento deste atendimento ao cliente?")) return;

        try {
            await connection.invoke("NotificarFechamentoChat", ticketId);
            showToast("📨 Solicitação de encerramento enviada ao cliente.", "info");
        } catch (err) {
            console.error("❌ Erro ao solicitar encerramento:", err);
            showToast("Erro ao enviar solicitação de encerramento.", "error");
        }
    }
});
