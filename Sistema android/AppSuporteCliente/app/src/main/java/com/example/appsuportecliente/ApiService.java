package com.example.appsuportecliente;
// Pacote principal onde a interface da API fica localizada no projeto Android.

import com.example.appsuportecliente.model.Chamado;
import com.example.appsuportecliente.LoginResponse;
import com.example.appsuportecliente.model.Mensagem;
import com.example.appsuportecliente.model.ReabrirResponse;
import com.example.appsuportecliente.model.TicketResponse;
import com.example.appsuportecliente.model.TicketWrapper;

import retrofit2.Call;
import retrofit2.http.Body;
import retrofit2.http.Field;
import retrofit2.http.FormUrlEncoded;
import retrofit2.http.GET;
import retrofit2.http.POST;
import retrofit2.http.Path;
import retrofit2.http.Query;

// Interface usada pelo Retrofit para declarar todas as rotas da API.
// Cada método representa uma requisição HTTP que o app pode fazer.
public interface ApiService {

    // 🔹 LOGIN DO USUÁRIO
    // ----------------------
    // @FormUrlEncoded → envia dados no formato application/x-www-form-urlencoded (igual um formulário HTML).
    // @POST("User/LoginAjax") → rota do backend ASP.NET para autenticação.
    // @Field → cada campo será enviado no corpo da requisição.
    @FormUrlEncoded
    @POST("User/LoginAjax")
    Call<LoginResponse> login(
            @Field("email") String email,       // Envia o e-mail digitado pelo usuário
            @Field("password") String password  // Envia a senha digitada
    );

    // 🔹 CRIA UM NOVO CHAMADO
    // ------------------------
    // @POST("Tickets/Novo") → chama o endpoint que cria um ticket
    // @Body → envia um objeto completo no corpo da requisição (JSON)
    // O objeto Chamado é convertido automaticamente em JSON pelo Retrofit + Gson.
    @POST("Tickets/Novo")
    Call<TicketResponse> criarChamado(@Body Chamado chamado);

    // 🔹 LISTA TODOS OS CHAMADOS DO CLIENTE
    // -------------------------------------
    // @GET("Tickets/ListarPorCliente") → Endpoint que lista os tickets do usuário
    // @Query("usuario") → envia o nome do usuário como parâmetro na URL
    // Exemplo: /Tickets/ListarPorCliente?usuario=Cauan
    //
    // O retorno é um "TicketWrapper", pois o servidor devolve:
    // {
    //   "success": true,
    //   "tickets": [...]
    // }
    @GET("Tickets/ListarPorCliente")
    Call<TicketWrapper> listarChamados(@Query("usuario") String nomeUsuario);

    // 🔹 REABRIR O CHAT DE UM TICKET
    // --------------------------------
    // @POST("Tickets/ReabrirChatMobile/{id}") → rota com parâmetro dinâmico
    // @Path("id") → substitui {id} no endpoint pelo ticketId
    // @Query → envia o nome do técnico como parâmetro na URL
    //
    // O retorno é um ReabrirResponse que contém:
    //   - success
    //   - ticket
    //   - mensagens
    @POST("Tickets/ReabrirChatMobile/{id}")
    Call<ReabrirResponse> reabrirChatMobile(
            @Path("id") int ticketId,            // Ticket selecionado
            @Query("tecnico") String nomeTecnico // Técnico que está reabrindo
    );

    // 🔹 VISUALIZAR HISTÓRICO DE MENSAGENS DO TICKET
    // ------------------------------------------------
    // @GET("Tickets/VisualizarChatMobile/{id}") → endpoint que devolve
    // o chat completo do ticket, incluindo todas as mensagens.
    @GET("Tickets/VisualizarChatMobile/{id}")
    Call<ReabrirResponse> visualizarChatMobile(
            @Path("id") int ticketId             // ID do ticket desejado
    );
}
