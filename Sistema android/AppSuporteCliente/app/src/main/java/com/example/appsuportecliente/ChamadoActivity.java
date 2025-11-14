package com.example.appsuportecliente;

import android.app.ProgressDialog;
import android.content.Intent;
import android.os.Bundle;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Toast;
import android.util.Log;

import androidx.annotation.NonNull;
import androidx.appcompat.app.AppCompatActivity;

import com.example.appsuportecliente.model.Chamado;
import com.example.appsuportecliente.model.TicketResponse;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;
import retrofit2.Retrofit;
import retrofit2.converter.gson.GsonConverterFactory;

/**
 * Activity responsável por criar um novo chamado.
 *
 * Permite ao usuário digitar título, descrição e enviar ao backend.
 */
public class ChamadoActivity extends AppCompatActivity {

    private EditText editTitulo, editDescricao;   // Campos de texto da tela
    private ProgressDialog progressDialog;        // Janela de carregamento
    private ApiService apiService;                // Interface da API
    private Button btnEnviar, btnMeusChamados;    // Botões da tela
    private static final String TAG = "ChamadoActivity"; // Tag de debug

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_chamado);

        // 📌 Recupera os componentes da interface
        editTitulo = findViewById(R.id.editTitulo);
        editDescricao = findViewById(R.id.editDescricao);
        btnEnviar = findViewById(R.id.btnEnviar);
        btnMeusChamados = findViewById(R.id.btnMeusChamados);

        // 📌 Configura ProgressDialog exibido durante o envio
        progressDialog = new ProgressDialog(this);
        progressDialog.setMessage("Enviando chamado...");
        progressDialog.setCancelable(false);

        // 📌 Configura Retrofit para chamadas HTTP
        Retrofit retrofit = new Retrofit.Builder()
                .baseUrl("http://192.168.1.9:5290/") // URL base da API
                .addConverterFactory(GsonConverterFactory.create()) // Converte JSON automaticamente
                .build();

        apiService = retrofit.create(ApiService.class);

        // 📌 Evento do botão ENVIAR chamado
        btnEnviar.setOnClickListener(v -> enviarChamado());

        // 📌 Evento do botão MEUS CHAMADOS
        btnMeusChamados.setOnClickListener(v -> {
            Intent intent = new Intent(ChamadoActivity.this, MeusChamadosActivity.class);
            startActivity(intent);
        });
    }

    /**
     * Envia um novo chamado ao servidor usando Retrofit.
     */
    private void enviarChamado() {
        // Recupera textos digitados
        String titulo = editTitulo.getText().toString().trim();
        String descricao = editDescricao.getText().toString().trim();

        // 📌 Validação básica
        if (titulo.isEmpty() || descricao.isEmpty()) {
            Toast.makeText(this, "Preencha todos os campos", Toast.LENGTH_SHORT).show();
            return;
        }

        // 📌 Recupera nome do criador armazenado login
        String criador = getSharedPreferences("UserPrefs", MODE_PRIVATE)
                .getString("username", "Usuário desconhecido");

        // Log para depuração
        Log.d(TAG, "👤 Nome do criador carregado: " + criador);

        progressDialog.show(); // Exibe loading

        // 📌 Cria objeto Chamado que será enviado no corpo da requisição
        Chamado chamado = new Chamado(titulo, descricao, criador);

        // 📌 Envia requisição POST
        apiService.criarChamado(chamado).enqueue(new Callback<TicketResponse>() {
            @Override
            public void onResponse(
                    @NonNull Call<TicketResponse> call,
                    @NonNull Response<TicketResponse> response
            ) {
                progressDialog.dismiss(); // Oculta o loading

                // 📌 Se resposta ok e corpo não for nulo
                if (response.isSuccessful() && response.body() != null) {
                    TicketResponse ticket = response.body();

                    Toast.makeText(ChamadoActivity.this,
                            "Chamado enviado com sucesso!",
                            Toast.LENGTH_SHORT).show();

                    // Limpa campos após envio
                    editTitulo.setText("");
                    editDescricao.setText("");

                    // 📌 Abre ChatActivity passando ID do ticket
                    Intent intent = new Intent(ChamadoActivity.this, ChatActivity.class);
                    intent.putExtra("ticketId", ticket.ticketId);

                    // Nome do criador (fallback caso backend retorne nulo)
                    intent.putExtra("usuario",
                            ticket.criador != null ? ticket.criador : criador);

                    // Se houver técnico responsável, envia também
                    if (ticket.tecnicoResponsavel != null) {
                        intent.putExtra("tecnico", ticket.tecnicoResponsavel);
                    }

                    startActivity(intent);

                } else {
                    Toast.makeText(ChamadoActivity.this,
                            "Erro ao enviar chamado", Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(
                    @NonNull Call<TicketResponse> call,
                    @NonNull Throwable t
            ) {
                progressDialog.dismiss(); // Oculta loading mesmo em erro

                Toast.makeText(
                        ChamadoActivity.this,
                        "Falha na conexão: " + t.getMessage(),
                        Toast.LENGTH_LONG
                ).show();
            }
        });
    }
}
