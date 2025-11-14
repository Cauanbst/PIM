package com.example.appsuportecliente.model;

import java.util.List;

/**
 * Classe usada para representar a resposta recebida do backend
 * quando o cliente tenta **reabrir um chamado**.
 *
 * O backend retorna:
 *  - success → se deu certo ou não reabrir o ticket
 *  - ticket → objeto completo do chamado reaberto
 *  - mensagens → histórico de mensagens do ticket enviado pela API
 *
 * Esta classe precisa refletir exatamente o JSON retornado pela API ASP.NET,
 * para que o Retrofit consiga fazer a desserialização corretamente.
 */
public class ReabrirResponse {

    // Indica se a reabertura foi bem-sucedida (true/false)
    private boolean success;

    // O ticket (chamado) reaberto, enviado pelo backend
    private Ticket ticket;

    // Lista de mensagens já existentes no ticket
    private List<Mensagem> mensagens;

    // =============================
    //           GETTERS
    // =============================

    /**
     * Retorna se a operação foi bem-sucedida.
     */
    public boolean isSuccess() {
        return success;
    }

    /**
     * Retorna o ticket retornado pelo servidor.
     */
    public Ticket getTicket() {
        return ticket;
    }

    /**
     * Retorna a lista de mensagens (histórico)
     * do ticket que foi reaberto.
     */
    public List<Mensagem> getMensagens() {
        return mensagens;
    }
}
