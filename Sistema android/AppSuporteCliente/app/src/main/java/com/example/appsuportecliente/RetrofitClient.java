package com.example.appsuportecliente;
// Pacote onde esta classe está localizada no projeto.

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;

import okhttp3.OkHttpClient;
import okhttp3.logging.HttpLoggingInterceptor;
import retrofit2.Retrofit;
import retrofit2.converter.gson.GsonConverterFactory;

// Classe responsável por criar e retornar a instância do Retrofit.
// O Retrofit é usado para fazer requisições HTTP ao servidor ASP.NET.
public class RetrofitClient {

    private static Retrofit retrofit;
    // Instância única (Singleton) do Retrofit.
    // Assim o app inteiro usa a mesma instância, economizando memória.

    private static final String BASE_URL = "http://192.168.1.9:5290/";
    // URL base da API do backend.
    // Todas as rotas do Retrofit serão adicionadas depois desse endereço.

    // Método principal que retorna a instância Singleton do Retrofit.
    public static Retrofit getInstance() {

        // Se ainda não foi criado, cria agora.
        if (retrofit == null) {

            // ================================
            // 🔹 Interceptor para LOGS HTTP
            // ================================
            HttpLoggingInterceptor logging = new HttpLoggingInterceptor();
            logging.setLevel(HttpLoggingInterceptor.Level.BODY);
            // LEVEL.BODY → exibe o corpo completo da requisição e resposta.
            // Isso ajuda demais a debugar erros da API.

            // ================================
            // 🔹 Cliente HTTP com interceptor
            // ================================
            OkHttpClient client = new OkHttpClient.Builder()
                    .addInterceptor(logging) // adiciona o log em todas as requisições
                    .build();

            // ================================
            // 🔹 Configuração do Gson
            // ================================
            Gson gson = new GsonBuilder()
                    .setLenient()  // deixa o parser mais flexível com JSON mal formatado
                    .create();

            // ================================
            // 🔹 Criação do Retrofit
            // ================================
            retrofit = new Retrofit.Builder()
                    .baseUrl(BASE_URL) // define a URL base
                    .client(client)    // adiciona o cliente com logs
                    .addConverterFactory(GsonConverterFactory.create(gson))
                    // Converte automaticamente JSON para objetos Java e vice-versa
                    .build();
        }

        return retrofit; // retorna a instância pronta
    }

    // Método auxiliar que retorna diretamente o serviço da API.
    // Evita ter que escrever getInstance().create(ApiService.class) em várias Activities.
    public static ApiService getApiService() {
        return getInstance().create(ApiService.class);
    }
}
