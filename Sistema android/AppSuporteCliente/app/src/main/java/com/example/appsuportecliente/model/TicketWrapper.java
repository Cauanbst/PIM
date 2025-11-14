package com.example.appsuportecliente.model;
// Pacote onde esta classe está organizada dentro da estrutura do projeto Android.

import com.google.gson.annotations.SerializedName;
// Importa a anotação @SerializedName, usada para mapear nomes de campos JSON para variáveis Java.

import java.util.List;
// Importa a classe List, já que o JSON retorna uma lista de tickets.

public class TicketWrapper {
    // Esta classe funciona como um "wrapper" (embrulho) para a resposta da API.
    // Muitas APIs retornam um JSON com vários campos, e um deles pode ser uma lista de tickets.
    // Aqui, ela serve para receber a resposta completa.

    @SerializedName("success")
    // Garante que o campo JSON "success" seja mapeado para esta variável,
    // mesmo que o nome da variável em Java fosse diferente.
    private boolean success;

    @SerializedName("tickets")
    // Mapeia o campo "tickets" do JSON para a variável listagem de Tickets.
    private List<Ticket> tickets;

    // Getter que retorna o valor do campo success.
    // Indica se a requisição ao servidor foi bem sucedida.
    public boolean isSuccess() { return success; }

    // Getter que retorna a lista de tickets enviada pela API.
    public List<Ticket> getTickets() { return tickets; }
}
