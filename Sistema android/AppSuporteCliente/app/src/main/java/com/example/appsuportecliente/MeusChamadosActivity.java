package com.example.appsuportecliente;

import android.annotation.SuppressLint;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.appcompat.app.AppCompatActivity;

import com.example.appsuportecliente.model.ReabrirResponse;
import com.example.appsuportecliente.model.Ticket;
import com.example.appsuportecliente.model.TicketWrapper;

import java.util.List;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

/**
 * Activity responsável por listar os chamados de um usuário
 * e permitir visualizar ou reabrir um ticket.
 */
public class MeusChamadosActivity extends AppCompatActivity {

    private LinearLayout containerChamados; // Container onde os cards dos chamados serão inseridos
    private ProgressBar progressBar;        // Barra de progresso exibida durante o carregamento
    private TextView txtSemChamados;        // Texto exibido quando não há chamados
    private String usuario;                 // Nome do usuário logado
    private static final String TAG = "DEBUG_CHAMADOS"; // Tag usada nos logs

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_meus_chamados);

        // Ligação com os elementos do layout
        containerChamados = findViewById(R.id.containerChamados);
        progressBar = findViewById(R.id.progressBar);
        txtSemChamados = findViewById(R.id.txtSemChamados);

        // Recupera o nome do usuário salvo no login
        usuario = getSharedPreferences("UserPrefs", MODE_PRIVATE)
                .getString("username", null);

        Log.d(TAG, "👤 Usuário logado (SharedPreferences): " + usuario);
        Toast.makeText(this, "Usuário logado: " + usuario, Toast.LENGTH_SHORT).show();

        // Valida usuário
        if (usuario == null || usuario.isEmpty()) {
            Toast.makeText(this, "Usuário inválido!", Toast.LENGTH_SHORT).show();
            Log.e(TAG, "❌ Erro: usuário nulo ou vazio. Finalizando activity.");
            finish();
            return;
        }

        // Carrega os chamados do usuário
        carregarChamados();
    }

    /**
     * Realiza a chamada à API para buscar os chamados do usuário.
     */
    private void carregarChamados() {
        progressBar.setVisibility(View.VISIBLE);  // Mostra carregamento
        containerChamados.removeAllViews();       // Limpa lista antiga
        txtSemChamados.setVisibility(View.GONE);  // Esconde texto de vazio

        ApiService api = RetrofitClient.getInstance().create(ApiService.class);
        Call<TicketWrapper> call = api.listarChamados(usuario);

        Log.d(TAG, "🔹 Chamando API: listarChamados(" + usuario + ")");

        // Faz a requisição assíncrona
        call.enqueue(new Callback<TicketWrapper>() {
            @Override
            public void onResponse(@NonNull Call<TicketWrapper> call,
                                   @NonNull Response<TicketWrapper> response) {

                progressBar.setVisibility(View.GONE); // Oculta loading

                if (response.isSuccessful() && response.body() != null) {

                    TicketWrapper wrapper = response.body();

                    // Verifica se encontrou chamados
                    if (wrapper.isSuccess() && wrapper.getTickets() != null &&
                            !wrapper.getTickets().isEmpty()) {

                        List<Ticket> chamados = wrapper.getTickets();
                        Log.d(TAG, "✅ " + chamados.size() + " chamados recebidos do servidor.");

                        // Adiciona cada chamado no layout
                        for (Ticket ticket : chamados) {
                            Log.d(TAG, "📦 Ticket -> ID=" + ticket.getId()
                                    + ", Título=" + ticket.getTitle()
                                    + ", Técnico=" + ticket.getTecnico()
                                    + ", Status=" + ticket.getStatus());

                            adicionarChamado(ticket);
                        }

                    } else {
                        // Nenhum chamado encontrado
                        txtSemChamados.setVisibility(View.VISIBLE);
                        Log.w(TAG, "⚠️ Nenhum chamado encontrado ou wrapper inválido.");
                    }

                } else {
                    // Erro HTTP
                    Toast.makeText(MeusChamadosActivity.this,
                            "Erro ao carregar chamados", Toast.LENGTH_SHORT).show();
                    Log.e(TAG, "❌ Erro HTTP: " + response.code());
                }
            }

            @Override
            public void onFailure(@NonNull Call<TicketWrapper> call,
                                  @NonNull Throwable t) {

                progressBar.setVisibility(View.GONE);
                Toast.makeText(MeusChamadosActivity.this,
                        "Falha: " + t.getMessage(), Toast.LENGTH_LONG).show();
                Log.e(TAG, "❌ Falha na chamada da API", t);
            }
        });
    }

    /**
     * Cria dinamicamente um card para um ticket e adiciona ao layout.
     */
    @SuppressLint("SetTextI18n")
    private void adicionarChamado(Ticket ticket) {

        // Infla o layout do card de chamado
        View card = getLayoutInflater().inflate(
                R.layout.item_chamado, containerChamados, false);

        // Referências aos elementos da view
        TextView txtId = card.findViewById(R.id.txtId);
        TextView txtStatus = card.findViewById(R.id.txtStatus);
        TextView txtTitulo = card.findViewById(R.id.txtTitulo);
        TextView txtDescricao = card.findViewById(R.id.txtDescricao);
        TextView txtTecnico = card.findViewById(R.id.txtTecnico);
        TextView txtData = card.findViewById(R.id.txtData);
        Button btnReabrir = card.findViewById(R.id.btnReabrir);
        Button btnVisualizar = card.findViewById(R.id.btnVisualizar);

        // Preenche os textos
        txtId.setText("#" + ticket.getId());
        txtStatus.setText(ticket.getStatus());
        txtTitulo.setText(ticket.getTitle());
        txtDescricao.setText(ticket.getDescription());
        txtTecnico.setText("👷 Técnico: " +
                (ticket.getTecnico() != null ? ticket.getTecnico() : "Não atribuído"));
        txtData.setText("📅 " + ticket.getDataCriacao());

        // Normaliza status
        String status = ticket.getStatus() != null ?
                ticket.getStatus().trim().toLowerCase() : "";

        // Configura cor e botões baseado no status
        switch (status) {
            case "aberto":
                txtStatus.setBackgroundResource(R.drawable.bg_status_aberto);
                btnReabrir.setVisibility(View.GONE);
                btnVisualizar.setVisibility(View.GONE);
                break;

            case "em andamento":
                txtStatus.setBackgroundResource(R.drawable.bg_status_andamento);
                btnReabrir.setVisibility(View.VISIBLE);
                btnVisualizar.setVisibility(View.GONE);
                break;

            case "fechado":
            case "finalizado":
            case "encerrado":
                txtStatus.setBackgroundResource(R.drawable.bg_status_fechado);
                btnReabrir.setVisibility(View.GONE);
                btnVisualizar.setVisibility(View.VISIBLE);
                break;

            default:
                txtStatus.setBackgroundResource(R.drawable.bg_status_desconhecido);
                btnReabrir.setVisibility(View.GONE);
                btnVisualizar.setVisibility(View.GONE);
                Log.w(TAG, "⚠️ Status desconhecido: " + ticket.getStatus());
                break;
        }

        // 🔄 Botão de reabrir ticket
        btnReabrir.setOnClickListener(v -> {
            Log.d(TAG, "🟢 Reabrindo ticket ID=" + ticket.getId());

            ApiService api = RetrofitClient.getInstance().create(ApiService.class);
            Call<ReabrirResponse> call =
                    api.reabrirChatMobile(ticket.getId(), ticket.getTecnico());

            call.enqueue(new Callback<ReabrirResponse>() {
                @Override
                public void onResponse(@NonNull Call<ReabrirResponse> call,
                                       @NonNull Response<ReabrirResponse> response) {

                    if (response.isSuccessful() && response.body() != null &&
                            response.body().isSuccess()) {

                        Log.d(TAG, "✅ Ticket reaberto com sucesso. Abrindo ChatActivity...");

                        // Abre a activity de chat
                        Intent intent = new Intent(
                                MeusChamadosActivity.this, ChatActivity.class);
                        intent.putExtra("ticketId", ticket.getId());
                        intent.putExtra("titulo", ticket.getTitle());
                        intent.putExtra("usuario", usuario);
                        intent.putExtra("tecnico", ticket.getTecnico());
                        intent.putExtra("modoVisualizacao", false);
                        startActivity(intent);

                    } else {
                        Toast.makeText(MeusChamadosActivity.this,
                                "Erro ao reabrir ticket", Toast.LENGTH_SHORT).show();
                        Log.e(TAG, "❌ Falha ao reabrir ticket: " + response.code());
                    }
                }

                @Override
                public void onFailure(@NonNull Call<ReabrirResponse> call,
                                      @NonNull Throwable t) {
                    Toast.makeText(MeusChamadosActivity.this,
                            "Falha: " + t.getMessage(), Toast.LENGTH_LONG).show();
                    Log.e(TAG, "❌ Erro na chamada reabrirChatMobile", t);
                }
            });
        });

        // 👁️ Botão de visualizar ticket (modo somente leitura)
        btnVisualizar.setOnClickListener(v -> {

            Log.d(TAG, "👁️ Visualizando conversa do ticket ID=" + ticket.getId());

            Intent intent = new Intent(MeusChamadosActivity.this, ChatActivity.class);
            intent.putExtra("ticketId", ticket.getId());
            intent.putExtra("titulo", ticket.getTitle());
            intent.putExtra("usuario", usuario);
            intent.putExtra("tecnico", ticket.getTecnico());
            intent.putExtra("modoVisualizacao", true); // Somente leitura
            startActivity(intent);
        });

        // Adiciona o card ao layout
        containerChamados.addView(card);
    }
}
