package com.example.appsuportecliente;
// Define o pacote onde essa classe está localizada no projeto Android.

public class LoginResponse {
    // Classe usada para receber a resposta JSON do servidor após o login.
    // O Retrofit converte automaticamente o JSON nos campos abaixo.

    private final boolean success;
    // Indica se o login foi bem-sucedido no backend (true/false).

    private final String message;
    // Mensagem enviada pelo servidor (ex: "Login realizado com sucesso" ou "Credenciais inválidas").

    private final String redirectUrl;
    // URL opcional enviada pelo backend para redirecionar o usuário após o login, caso seja necessário.

    private final String username;
    // Nome do usuário retornado pela API (o backend envia o nome registrado).

    // Construtor usado pelo Retrofit para criar o objeto com base nos valores do JSON recebido.
    public LoginResponse(boolean success, String message, String redirectUrl, String username) {
        this.success = success;
        this.message = message;
        this.redirectUrl = redirectUrl;
        this.username = username;
    }

    // ---------- GETTERS ----------

    public boolean isSuccess() {
        // Retorna se o login deu certo ou não.
        return success;
    }

    public String getMessage() {
        // Retorna a mensagem enviada pelo backend.
        return message;
    }

    public String getRedirectUrl() {
        // Retorna a URL de redirecionamento, se existir.
        return redirectUrl;
    }

    public String getUsername() {
        // Retorna o nome do usuário autenticado.
        return username;
    }

}
