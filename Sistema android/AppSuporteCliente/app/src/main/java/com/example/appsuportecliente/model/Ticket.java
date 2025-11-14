package com.example.appsuportecliente.model;
// Define o pacote onde esta classe está localizada dentro do projeto.

public class Ticket {

    // ---------------------- ATRIBUTOS DO TICKET ----------------------

    private int id;                     // ID único do ticket
    private String title;               // Título do ticket
    private String description;         // Descrição informada pelo usuário
    private String status;              // Status atual (ex: "aberto", "em andamento", "fechado")
    private String tecnico;             // Nome do técnico responsável (se houver)
    private String criador;             // Nome de quem criou o ticket
    private String dataCriacao;         // Data e hora em que o ticket foi criado
    private String inicioAtendimento;   // Data/hora em que o técnico iniciou o atendimento
    private String fimAtendimento;      // Data/hora em que o atendimento terminou
    private String tempoAtendimento;    // Tempo total gasto no atendimento

    // ---------------------- GETTERS ----------------------
    // Métodos que permitem acessar os valores dos atributos

    public int getId() { return id; }
    // Retorna o ID do ticket

    public String getTitle() { return title; }
    // Retorna o título do ticket

    public String getDescription() { return description; }
    // Retorna a descrição

    public String getStatus() { return status; }
    // Retorna o status atual do ticket

    public String getTecnico() { return tecnico; }
    // Retorna o nome do técnico responsável (pode ser null)

    public String getCriador() { return criador; }
    // Retorna o nome de quem criou o ticket

    public String getDataCriacao() { return dataCriacao; }
    // Retorna a data em que o ticket foi criado

    public String getInicioAtendimento() { return inicioAtendimento; }
    // Retorna quando o atendimento foi iniciado

    public String getFimAtendimento() { return fimAtendimento; }
    // Retorna quando o atendimento foi finalizado

    public String getTempoAtendimento() { return tempoAtendimento; }
    // Retorna o tempo calculado do atendimento

    // ---------------------- SETTERS ----------------------
    // Métodos usados para modificar os valores dos atributos

    public void setId(int id) { this.id = id; }
    // Define um novo valor para o ID

    public void setTitle(String title) { this.title = title; }
    // Define o título do ticket

    public void setDescription(String description) { this.description = description; }
    // Define a descrição completa do ticket

    public void setStatus(String status) { this.status = status; }
    // Define o status do ticket

    public void setTecnico(String tecnico) { this.tecnico = tecnico; }
    // Define o técnico responsável pelo atendimento

    public void setCriador(String criador) { this.criador = criador; }
    // Define quem criou o ticket

    public void setDataCriacao(String dataCriacao) { this.dataCriacao = dataCriacao; }
    // Define a data de criação do ticket

    public void setInicioAtendimento(String inicioAtendimento) { this.inicioAtendimento = inicioAtendimento; }
    // Define quando o técnico começou o atendimento

    public void setFimAtendimento(String fimAtendimento) { this.fimAtendimento = fimAtendimento; }
    // Define quando o atendimento foi concluído

    public void setTempoAtendimento(String tempoAtendimento) { this.tempoAtendimento = tempoAtendimento; }
    // Define o tempo total consumido no atendimento
}
