package com.example.appsuportecliente;
// Pacote onde esta Activity está localizada dentro do projeto Android.

import androidx.annotation.NonNull;
import androidx.appcompat.app.AppCompatActivity;
import android.os.Bundle;
import android.content.Intent;
import android.widget.*;
import android.app.ProgressDialog;

import retrofit2.*;

// Activity responsável pela tela de login do aplicativo.
public class LoginActivity extends AppCompatActivity {

    // Campos da interface que o usuário vai digitar ou interagir
    private EditText editEmail, editSenha;
    private CheckBox checkBoxConsentimento;

    // Interface com os endpoints da API (Retrofit)
    private ApiService apiService;

    // Janela de carregamento enquanto a API processa o login
    private ProgressDialog progressDialog;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_login);
        // Define qual layout XML será usado por esta tela

        // 🔹 Inicializa os componentes com os elementos do XML
        editEmail = findViewById(R.id.editEmail);      // Campo de e-mail
        editSenha = findViewById(R.id.editSenha);      // Campo de senha
        checkBoxConsentimento = findViewById(R.id.checkBoxConsentimento); // Checkbox LGPD
        Button btnEntrar = findViewById(R.id.btnLogin); // Botão de login

        // 🔹 Configura a janela de carregamento para exibir enquanto aguarda o servidor
        progressDialog = new ProgressDialog(this);
        progressDialog.setMessage(getString(R.string.login_loading)); // Texto de "Carregando"
        progressDialog.setCancelable(false); // Não permite cancelar ao tocar no fundo

        // 🔹 Inicializa o Retrofit usando o cliente definido em RetrofitClient
        apiService = RetrofitClient.getInstance().create(ApiService.class);

        // 🔹 Quando o usuário clicar no botão, chama o método fazerLogin()
        btnEntrar.setOnClickListener(v -> fazerLogin());
    }

    // Método responsável por validar o login e chamar a API
    private void fazerLogin() {
        String email = editEmail.getText().toString().trim(); // Obtém o texto do campo de e-mail
        String senha = editSenha.getText().toString().trim(); // Obtém a senha digitada

        // 🔹 Valida se os campos estão vazios
        if (email.isEmpty() || senha.isEmpty()) {
            Toast.makeText(this, getString(R.string.login_empty_fields), Toast.LENGTH_SHORT).show();
            return; // Interrompe o método se estiver faltando informações
        }

        // 🔹 Verifica se o usuário aceitou o consentimento de dados
        if (!checkBoxConsentimento.isChecked()) {
            Toast.makeText(this, "Você precisa autorizar o uso dos seus dados.", Toast.LENGTH_SHORT).show();
            return;
        }

        // Exibe o ProgressDialog enquanto o login está sendo processado
        progressDialog.show();

        // 🔹 Envia requisição POST para o servidor usando Retrofit
        apiService.login(email, senha).enqueue(new Callback<>() {

            // Quando o servidor responde (mesmo que com erro)
            @Override
            public void onResponse(@NonNull Call<LoginResponse> call, @NonNull Response<LoginResponse> response) {
                progressDialog.dismiss(); // Fecha a tela de carregamento

                // Caso o servidor retorne com sucesso HTTP (ex: 200)
                if (response.isSuccessful() && response.body() != null) {

                    LoginResponse res = response.body(); // Obtém o JSON convertido em objeto

                    // Se o login foi bem-sucedido no backend
                    if (res.isSuccess()) {

                        // Mostra a mensagem de sucesso do servidor
                        Toast.makeText(LoginActivity.this, res.getMessage(), Toast.LENGTH_SHORT).show();

                        // 🔹 Salva informações do usuário localmente usando SharedPreferences
                        getSharedPreferences("UserPrefs", MODE_PRIVATE)
                                .edit()
                                .putString("username", res.getUsername() != null ? res.getUsername() : email)
                                .putString("email", email)
                                .apply(); // Salva os dados

                        // 🔹 Abre a próxima Activity (ChamadoActivity)
                        Intent intent = new Intent(LoginActivity.this, ChamadoActivity.class);
                        intent.putExtra("redirectUrl", res.getRedirectUrl()); // Envia URL caso exista
                        startActivity(intent);
                        finish(); // Finaliza LoginActivity para o usuário não voltar nela pelo botão "Voltar"

                    } else {
                        // Se o login falhou (senha errada, email não existe etc.)
                        Toast.makeText(LoginActivity.this, res.getMessage(), Toast.LENGTH_LONG).show();
                    }

                } else {
                    // Caso o servidor responda erro HTTP 500, 404 etc.
                    Toast.makeText(LoginActivity.this, getString(R.string.login_error_server), Toast.LENGTH_LONG).show();
                }
            }

            // Caso ocorra erro de conexão, timeout, servidor offline etc.
            @Override
            public void onFailure(@NonNull Call<LoginResponse> call, @NonNull Throwable t) {
                progressDialog.dismiss(); // Fecha o loading
                String erro = String.format(getString(R.string.login_error_failure), t.getMessage());
                Toast.makeText(LoginActivity.this, erro, Toast.LENGTH_LONG).show();
            }
        });
    }
}
