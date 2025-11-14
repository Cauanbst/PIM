package com.example.appsuportecliente.model;

/**
 * Classe que representa um Chamado (Ticket) no aplicativo Android.
 * Este modelo reflete exatamente a estrutura usada no backend ASP.NET,
 * incluindo o campo "Criador".
 */
public class Chamado {

    // Título do chamado (Ex: "Erro no sistema", "Problema na rede")
    private String Title;

    // Descrição detalhada do problema relatado pelo usuário
    private String Description;

    // Nome ou identificação de quem criou o chamado (cliente)
    private String Criador;

    /**
     * Construtor principal do objeto Chamado.
     *
     * @param title       Título do chamado
     * @param description Descrição do problema
     * @param criador     Nome do criador (usuário)
     */
    public Chamado(String title, String description, String criador) {
        this.Title = title;
        this.Description = description;
        this.Criador = criador;
    }

    // ------- GETTERS e SETTERS --------

    // Retorna o título do chamado
    public String getTitle() { return Title; }

    // Define o título do chamado
    public void setTitle(String title) { this.Title = title; }

    // Retorna a descrição do chamado
    public String getDescription() { return Description; }

    // Define a descrição do chamado
    public void setDescription(String description) { this.Description = description; }

    // Retorna o nome do criador do chamado
    public String getCriador() { return Criador; }

    // Define o nome do criador
    public void setCriador(String criador) { this.Criador = criador; }
}
