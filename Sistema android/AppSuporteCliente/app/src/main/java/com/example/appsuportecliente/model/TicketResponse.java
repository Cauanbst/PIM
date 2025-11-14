package com.example.appsuportecliente.model;
// Define o pacote onde esta classe está organizada dentro do projeto.

public class TicketResponse {
    // Esta classe representa a resposta de alguma requisição relacionada a um ticket,
    // provavelmente recebida da API/servidor.

    public boolean success;
    // Indica se a operação realizada no servidor foi bem-sucedida (true) ou não (false).

    public String tecnicoResponsavel;
    // Nome do técnico que ficará responsável pelo ticket (se houver).

    public String especialidade;
    // Especialidade do técnico ou categoria associada ao atendimento.

    public String usuario;
    // Nome do usuário que fez a requisição ou está associado ao ticket.

    public int ticketId;
    // ID do ticket retornado pelo servidor.

    public String redirectUrl;
    // URL para redirecionamento (caso a API utilize alguma lógica de navegação).

    public CharSequence criador;
    // Nome do criador do ticket, mas usando CharSequence ao invés de String.
    // CharSequence permite usar diferentes tipos de texto (ex: String, Spannable...).
}
