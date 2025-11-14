package com.example.appsuportecliente.model;

/**
 * Classe que representa uma mensagem dentro do chat do chamado.
 * Cada mensagem enviada ou recebida no ticket é mapeada para este modelo.
 *
 * Esse modelo deve corresponder exatamente ao retorno da API ASP.NET,
 * permitindo exibir o histórico de mensagens no aplicativo Android.
 */
public class Mensagem {

    // Identificador único da mensagem (gerado pelo backend)
    private int id;

    // Nome ou identificação de quem enviou a mensagem
    private String remetente;

    // Nome ou identificação de quem deve receber a mensagem
    private String destinatario;

    // Conteúdo textual da mensagem enviada
    private String conteudo;

    // Data e horário em que a mensagem foi enviada (string vinda da API)
    private String dataEnvio;

    // Referência ao ticket (chamado) ao qual esta mensagem pertence
    private int ticketId;

    // =============================
    //        GETTERS
    // =============================

    // Retorna o ID da mensagem
    public int getId() {
        return id;
    }

    // Retorna quem enviou a mensagem
    public String getRemetente() {
        return remetente;
    }

    // Retorna quem deve receber a mensagem
    public String getDestinatario() {
        return destinatario;
    }

    // Retorna o texto da mensagem
    public String getConteudo() {
        return conteudo;
    }

    // Retorna a data e hora de envio
    public String getDataEnvio() {
        return dataEnvio;
    }

    // Retorna a qual ticket essa mensagem pertence
    public int getTicketId() {
        return ticketId;
    }

    // =============================
    //        SETTERS
    // =============================

    // Define o ID da mensagem
    public void setId(int id) {
        this.id = id;
    }

    // Define o nome do remetente
    public void setRemetente(String remetente) {
        this.remetente = remetente;
    }

    // Define o nome do destinatário
    public void setDestinatario(String destinatario) {
        this.destinatario = destinatario;
    }

    // Define o texto enviado
    public void setConteudo(String conteudo) {
        this.conteudo = conteudo;
    }

    // Define a data/hora do envio
    public void setDataEnvio(String dataEnvio) {
        this.dataEnvio = dataEnvio;
    }

    // Define o ID do ticket vinculado
    public void setTicketId(int ticketId) {
        this.ticketId = ticketId;
    }
}
