package com.example.appsuportecliente;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.annotation.NonNull;
import androidx.appcompat.app.AppCompatActivity;
import androidx.appcompat.app.AlertDialog;

import android.graphics.Paint;
import android.annotation.SuppressLint;
import android.content.Intent;
import android.database.Cursor;
import android.net.Uri;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.text.Html;
import android.text.method.LinkMovementMethod;
import android.util.Log;
import android.view.Gravity;
import android.view.View;
import android.webkit.MimeTypeMap;
import android.widget.EditText;
import android.widget.ImageButton;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;
import android.widget.Toast;

import com.google.gson.Gson;
import com.google.gson.internal.LinkedTreeMap;
import com.microsoft.signalr.HubConnection;
import com.microsoft.signalr.HubConnectionBuilder;
import com.microsoft.signalr.HubConnectionState;

import org.json.JSONArray;
import org.json.JSONObject;

import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.IOException;
import java.net.HttpURLConnection;
import java.net.URL;
import java.util.HashSet;
import java.util.Set;

import okhttp3.Call;
import okhttp3.Callback;
import okhttp3.MediaType;
import okhttp3.MultipartBody;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.RequestBody;
import okhttp3.Response;

public class ChatActivity extends AppCompatActivity {

    // Tag para logs
    private static final String TAG = "CHAT_DEBUG";

    // Componentes da interface
    private EditText editMensagem;
    private LinearLayout chatLogContainer;
    private ScrollView scrollView;

    // Conexão com o SignalR
    private HubConnection hubConnection;

    // Informações do ticket e usuário
    private int ticketId;
    private String usuario;
    private String tecnico;

    // Controle de estado
    private boolean handlersRegistrados = false;
    private boolean modoVisualizacao = false;

    // Controle de duplicação de mensagens
    private final Set<String> mensagensRecebidas = new HashSet<>();
    private final Set<String> mensagensRenderizadas = new HashSet<>();

    // Temporizador do encerramento
    private Handler temporizadorHandler = new Handler(Looper.getMainLooper());
    private Runnable encerramentoRunnable;

    // Launcher para escolher arquivos
    private final ActivityResultLauncher<Intent> abrirArquivo = registerForActivityResult(
            new ActivityResultContracts.StartActivityForResult(),
            result -> {
                if (result.getResultCode() == RESULT_OK && result.getData() != null) {
                    Uri uriSelecionado = result.getData().getData();
                    if (uriSelecionado != null) {
                        enviarArquivoParaServidor(uriSelecionado, ticketId);
                    }
                }
            });

    @SuppressLint({"InflateParams", "CheckResult"})
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_chat);

        // Vincula componentes da interface
        editMensagem = findViewById(R.id.editMensagem);
        chatLogContainer = findViewById(R.id.chatLogContainer);
        scrollView = findViewById(R.id.scrollView);

        ImageButton btnEnviar = findViewById(R.id.btnEnviar);
        ImageButton btnAnexo = findViewById(R.id.btnAnexo);
        LinearLayout barraEnvio = findViewById(R.id.layoutEnviarMensagem);

        // Área do nome e inicial do técnico
        TextView tecnicoInicial = findViewById(R.id.tecnicoInicial);
        TextView tecnicoNome = findViewById(R.id.tecnicoNome);

        // Recebe informações enviadas pela Activity anterior
        ticketId = getIntent().getIntExtra("ticketId", 0);
        usuario = getIntent().getStringExtra("usuario");
        tecnico = getIntent().getStringExtra("tecnico");
        modoVisualizacao = getIntent().getBooleanExtra("modoVisualizacao", false);

        // Valida dados do chat
        if (ticketId == 0 || usuario == null || tecnico == null) {
            Toast.makeText(this, "Erro: dados do chat inválidos.", Toast.LENGTH_SHORT).show();
            finish();
            return;
        }

        // Exibe nome e inicial do técnico
        tecnicoNome.setText(tecnico);
        tecnicoInicial.setText(tecnico.substring(0, 1).toUpperCase());

        // Modo visualização esconde barra de envio
        if (modoVisualizacao) {
            editMensagem.setVisibility(View.GONE);
            btnEnviar.setVisibility(View.GONE);
            btnAnexo.setVisibility(View.GONE);
            barraEnvio.setVisibility(View.GONE);
        }

        // Carrega histórico de mensagens
        carregarMensagensAnteriores(ticketId);

        // Inicia conexão com o hub SignalR
        hubConnection = HubConnectionBuilder.create(getString(R.string.chat_hub_url)).build();

        // Registra handlers (ReceberMensagem, encerramento, etc.)
        configurarHandlersSignalR();

        // Inicia conexão
        hubConnection.start().subscribe(() -> {
            hubConnection.invoke("EntrarNoTicket", ticketId);
        }, error -> runOnUiThread(() ->
                Toast.makeText(ChatActivity.this, "Erro SignalR: " + error.getMessage(), Toast.LENGTH_LONG).show()
        ));

        // Enviar mensagem texto
        btnEnviar.setOnClickListener(v -> {
            String texto = editMensagem.getText().toString().trim();
            if (!texto.isEmpty()) {
                btnEnviar.setEnabled(false);

                // Envia para o servidor
                hubConnection.invoke("EnviarMensagem", ticketId, usuario, texto, "cliente");

                // Exibe no chat local
                adicionarBolha(texto, true);

                editMensagem.setText("");
                new Handler(Looper.getMainLooper()).postDelayed(() -> btnEnviar.setEnabled(true), 800);
            }
        });

        // Abrir seletor de arquivos
        btnAnexo.setOnClickListener(v -> {
            Intent intent = new Intent(Intent.ACTION_GET_CONTENT);
            intent.setType("*/*");
            intent.addCategory(Intent.CATEGORY_OPENABLE);
            abrirArquivo.launch(intent);
        });
    }

    // ============================================================
    // CONFIGURAÇÃO DOS HANDLERS DO SIGNALR
    // ============================================================
    private void configurarHandlersSignalR() {
        if (handlersRegistrados) return;

        hubConnection.remove("ReceberMensagem");
        hubConnection.remove("ChatEncerradoPeloTecnico");

        // Recebe mensagens em tempo real
        hubConnection.on("ReceberMensagem", (dados) -> runOnUiThread(() -> {
            try {
                LinkedTreeMap map = (LinkedTreeMap) dados;
                String autor = map.get("autor") != null ? map.get("autor").toString() : "";
                String mensagem = map.get("mensagem") != null ? map.get("mensagem").toString() : "";
                String papel = map.get("papel") != null ? map.get("papel").toString() : "";

                // Impede mostrar mensagens enviadas por você mesmo
                if (autor.trim().equalsIgnoreCase(usuario.trim())) return;

                // Impede duplicações
                String idMensagem = autor + mensagem + papel;
                if (mensagensRecebidas.contains(idMensagem)) return;
                mensagensRecebidas.add(idMensagem);

                adicionarBolha(mensagem, false);

            } catch (Exception e) {
                Log.e(TAG, "Erro processar mensagem: " + e.getMessage(), e);
            }
        }), Object.class);

        // Quando técnico decide encerrar
        hubConnection.on("ChatEncerradoPeloTecnico",
                (ticketIdServer) -> runOnUiThread(this::mostrarDialogoEncerramento),
                Integer.class);

        handlersRegistrados = true;
    }

    // ============================================================
    // POPUP DE ENCERRAMENTO COM TIMER
    // ============================================================
    private void mostrarDialogoEncerramento() {

        // Cria layout personalizado
        LinearLayout layout = new LinearLayout(ChatActivity.this);
        layout.setOrientation(LinearLayout.VERTICAL);
        layout.setPadding(40, 40, 40, 40);
        layout.setGravity(Gravity.CENTER_HORIZONTAL);

        TextView titulo = new TextView(this);
        titulo.setText("⚠️ Encerramento de chamado");
        titulo.setTextSize(20);
        titulo.setTextColor(0xFF222222);
        titulo.setGravity(Gravity.CENTER);
        titulo.setPadding(0, 0, 0, 20);
        layout.addView(titulo);

        TextView mensagem = new TextView(this);
        mensagem.setText("O técnico está encerrando o atendimento.\nDeseja finalizar o chamado?");
        mensagem.setTextSize(16);
        mensagem.setTextColor(0xFF333333);
        mensagem.setGravity(Gravity.CENTER);
        mensagem.setPadding(0, 0, 0, 20);
        layout.addView(mensagem);

        // Contador regressivo
        TextView contador = new TextView(this);
        contador.setText("Encerrando em 6 segundos...");
        contador.setTextSize(18);
        contador.setTextColor(0xFFE53935);
        contador.setGravity(Gravity.CENTER);
        layout.addView(contador);

        // Cria o dialog
        AlertDialog dialog = new AlertDialog.Builder(this)
                .setView(layout)
                .setCancelable(false)
                .setPositiveButton("Sim", (d, which) -> {
                    confirmarEncerramento();
                    d.dismiss();
                })
                .setNegativeButton("Não", (d, which) -> {
                    if (encerramentoRunnable != null)
                        temporizadorHandler.removeCallbacks(encerramentoRunnable);

                    Toast.makeText(this, "Chamado mantido aberto.", Toast.LENGTH_SHORT).show();
                    d.dismiss();
                })
                .create();

        dialog.show();

        // Timer decrescente
        final int[] segundos = {6};

        encerramentoRunnable = new Runnable() {
            @Override
            public void run() {
                segundos[0]--;
                if (segundos[0] > 0) {
                    contador.setText("Encerrando em " + segundos[0] + " segundos...");
                    temporizadorHandler.postDelayed(this, 1000);
                } else {
                    dialog.dismiss();
                    confirmarEncerramento();
                }
            }
        };

        temporizadorHandler.postDelayed(encerramentoRunnable, 1000);
    }

    // Envia confirmação ao servidor
    private void confirmarEncerramento() {
        if (hubConnection != null && hubConnection.getConnectionState() == HubConnectionState.CONNECTED) {
            hubConnection.invoke("ClienteConfirmouEncerrar", ticketId);
        }
        Toast.makeText(this, "Chamado encerrado com sucesso.", Toast.LENGTH_SHORT).show();
        encerrarChat();
    }

    // Redireciona para a tela principal
    private void encerrarChat() {
        Intent intent = new Intent(ChatActivity.this, ChamadoActivity.class);
        intent.setFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP | Intent.FLAG_ACTIVITY_NEW_TASK);
        startActivity(intent);
        finish();
    }

    // ============================================================
    // CARREGA HISTÓRICO DO CHAT
    // ============================================================
    private void carregarMensagensAnteriores(int ticketId) {
        new Thread(() -> {
            try {
                String url = "http://192.168.1.9:5290/Tickets/VisualizarChatMobile/" + ticketId;

                // Abre conexão HTTP
                HttpURLConnection conn = (HttpURLConnection) new URL(url).openConnection();
                conn.setRequestMethod("GET");

                // Lê resposta do servidor
                InputStream in = conn.getInputStream();
                byte[] buffer = new byte[8192];
                int bytesRead;
                StringBuilder response = new StringBuilder();

                while ((bytesRead = in.read(buffer)) != -1) {
                    response.append(new String(buffer, 0, bytesRead));
                }

                conn.disconnect();

                JSONObject json = new JSONObject(response.toString());
                JSONArray array = json.getJSONArray("mensagens");

                // Atualiza UI
                runOnUiThread(() -> {
                    chatLogContainer.removeAllViews();
                    for (int i = 0; i < array.length(); i++) {
                        try {
                            JSONObject obj = array.getJSONObject(i);
                            String remetente = obj.getString("remetente");
                            String conteudo = obj.getString("conteudo");

                            boolean isUsuario = remetente.equalsIgnoreCase(usuario);
                            adicionarBolha(conteudo, isUsuario);

                        } catch (Exception e) {
                            Log.e(TAG, "Erro parse mensagem antiga: " + e.getMessage());
                        }
                    }
                });

            } catch (Exception e) {
                Log.e(TAG, "Erro carregar mensagens antigas: " + e.getMessage(), e);
            }
        }).start();
    }

    // ============================================================
    // ADICIONA UMA MENSAGEM NA TELA
    // ============================================================
    @SuppressLint("InflateParams")
    private void adicionarBolha(String mensagem, boolean isUsuario) {

        // Evita duplicação
        String msgId = mensagem.replace("file:", "").trim().toLowerCase();
        if (mensagensRenderizadas.contains(msgId)) return;
        mensagensRenderizadas.add(msgId);

        // Layout da bolha externa
        LinearLayout messageLayout = new LinearLayout(this);
        messageLayout.setOrientation(LinearLayout.VERTICAL);

        LinearLayout.LayoutParams layoutParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.WRAP_CONTENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
        );

        layoutParams.gravity = isUsuario ? Gravity.END : Gravity.START;
        layoutParams.setMargins(8, 8, 8, 8);
        messageLayout.setLayoutParams(layoutParams);

        // Layout interno (bolha)
        LinearLayout bubbleLayout = new LinearLayout(this);
        bubbleLayout.setOrientation(LinearLayout.VERTICAL);
        bubbleLayout.setBackgroundResource(isUsuario ? R.drawable.bg_bolha_usuario : R.drawable.bg_bolha_tecnico);
        bubbleLayout.setPadding(20, 14, 20, 14);

        // VERIFICA SE É ARQUIVO
        if (mensagem.startsWith("file:")) {

            String fileUrl = mensagem.replace("file:", "").trim();
            String ext = fileUrl.substring(fileUrl.lastIndexOf('.') + 1).toLowerCase();

            // Se for imagem
            if (ext.matches("jpg|jpeg|png|gif|bmp|webp")) {

                android.widget.ImageView imageView = new android.widget.ImageView(this);
                imageView.setAdjustViewBounds(true);
                imageView.setMaxWidth(600);
                imageView.setMaxHeight(600);

                new Thread(() -> {
                    try {
                        URL url = new URL(fileUrl);
                        android.graphics.Bitmap bitmap =
                                android.graphics.BitmapFactory.decodeStream(url.openConnection().getInputStream());

                        runOnUiThread(() -> imageView.setImageBitmap(bitmap));
                    } catch (Exception e) {
                        Log.e(TAG, "Erro carregar imagem: " + e.getMessage(), e);
                    }
                }).start();

                bubbleLayout.addView(imageView);

            } else {
                // Se for outro arquivo (PDF, ZIP...)
                TextView txtLink = new TextView(this);
                String nomeArquivo = fileUrl.substring(fileUrl.lastIndexOf('/') + 1);

                txtLink.setText("📎 " + nomeArquivo);
                txtLink.setTextColor(0xFF007BFF);
                txtLink.setPaintFlags(txtLink.getPaintFlags() | Paint.UNDERLINE_TEXT_FLAG);

                txtLink.setOnClickListener(v -> {
                    Intent intent = new Intent(Intent.ACTION_VIEW, Uri.parse(fileUrl));
                    startActivity(intent);
                });

                bubbleLayout.addView(txtLink);
            }

        } else {
            // Mensagem de texto
            TextView txtMensagem = new TextView(this);
            txtMensagem.setText(Html.fromHtml(mensagem, Html.FROM_HTML_MODE_LEGACY));
            txtMensagem.setMovementMethod(LinkMovementMethod.getInstance());
            txtMensagem.setTextColor(isUsuario ? 0xFFFFFFFF : 0xFF000000);
            bubbleLayout.addView(txtMensagem);
        }

        // HORÁRIO
        TextView txtHora = new TextView(this);
        txtHora.setTextColor(0xFF888888);
        txtHora.setTextSize(11f);
        txtHora.setPadding(6, 2, 6, 0);
        txtHora.setTextAlignment(isUsuario ? View.TEXT_ALIGNMENT_TEXT_END : View.TEXT_ALIGNMENT_TEXT_START);

        java.text.SimpleDateFormat sdf = new java.text.SimpleDateFormat("HH:mm");
        txtHora.setText(sdf.format(new java.util.Date()));

        // Adiciona tudo ao layout
        messageLayout.addView(bubbleLayout);
        messageLayout.addView(txtHora);

        chatLogContainer.addView(messageLayout);

        // Scroll automático
        scrollView.post(() -> scrollView.fullScroll(ScrollView.FOCUS_DOWN));
    }

    // ============================================================
    // ENVIA ARQUIVO PARA O SERVIDOR
    // ============================================================
    private void enviarArquivoParaServidor(Uri uriArquivo, int ticketId) {
        try {
            // Copia arquivo para cache temporário
            File tempFile = new File(getCacheDir(), "upload_temp");

            try (InputStream inputStream = getContentResolver().openInputStream(uriArquivo);
                 OutputStream outputStream = new FileOutputStream(tempFile)) {

                byte[] buffer = new byte[4096];
                int bytesRead;

                while ((bytesRead = inputStream.read(buffer)) != -1) {
                    outputStream.write(buffer, 0, bytesRead);
                }
            }

            // Pega nome original do arquivo
            String nomeOriginal = getFileNameWithExtension(uriArquivo);

            RequestBody fileBody =
                    RequestBody.create(MediaType.parse("application/octet-stream"), tempFile);

            // Corpo da requisição com multipart
            MultipartBody requestBody = new MultipartBody.Builder()
                    .setType(MultipartBody.FORM)
                    .addFormDataPart("file", nomeOriginal, fileBody)
                    .addFormDataPart("ticketId", String.valueOf(ticketId))
                    .addFormDataPart("usuario", usuario)
                    .build();

            Request request = new Request.Builder()
                    .url("http://192.168.1.9:5290/Upload/Create")
                    .post(requestBody)
                    .build();

            OkHttpClient client = new OkHttpClient();

            // Faz upload
            client.newCall(request).enqueue(new Callback() {
                @Override
                public void onFailure(@NonNull Call call, @NonNull IOException e) {
                    runOnUiThread(() -> Toast.makeText(ChatActivity.this,
                            "Falha ao enviar arquivo: " + e.getMessage(),
                            Toast.LENGTH_LONG).show());
                }

                @Override
                public void onResponse(@NonNull Call call, @NonNull Response response)
                        throws IOException {

                    String resposta = response.body().string();
                    response.close();

                    // Extrai URL do arquivo do JSON
                    String fileUrl = parseFileUrlFromJson(resposta);

                    if (fileUrl != null) {
                        String mensagem = "file:" + fileUrl;

                        // Mostra no chat
                        runOnUiThread(() -> adicionarBolha(mensagem, true));

                        // Envia pelo SignalR
                        hubConnection.invoke("EnviarMensagem",
                                ticketId, usuario, mensagem, "cliente");
                    }
                }
            });

        } catch (Exception ex) {
            Toast.makeText(this,
                    "Erro ao preparar arquivo: " + ex.getMessage(),
                    Toast.LENGTH_LONG).show();
        }
    }

    // Extrai URL do arquivo enviado
    private String parseFileUrlFromJson(String json) {
        try {
            Gson gson = new Gson();
            LinkedTreeMap map = gson.fromJson(json, LinkedTreeMap.class);

            if (map.get("fileUrl") != null)
                return map.get("fileUrl").toString();

        } catch (Exception e) {
            Log.e(TAG, "Erro parse JSON do arquivo: " + e.getMessage(), e);
        }

        return null;
    }

    // Obtém nome do arquivo
    private String getFileNameWithExtension(Uri uri) {
        String result = null;

        if ("content".equals(uri.getScheme())) {
            try (Cursor cursor =
                         getContentResolver().query(uri, null, null, null, null)) {

                if (cursor != null && cursor.moveToFirst()) {
                    result = cursor.getString(cursor.getColumnIndexOrThrow(
                            android.provider.OpenableColumns.DISPLAY_NAME));
                }
            }
        }

        if (result == null)
            result = uri.getLastPathSegment();

        return result != null ? result : "arquivo.dat";
    }

    // Encerra conexão ao fechar Activity
    @Override
    protected void onDestroy() {
        super.onDestroy();

        if (hubConnection != null &&
                hubConnection.getConnectionState() == HubConnectionState.CONNECTED) {
            hubConnection.stop();
        }
    }
}
