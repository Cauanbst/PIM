package com.example.appsuportecliente;

import androidx.appcompat.app.AppCompatActivity;
import android.os.Bundle;
import android.widget.ImageView;
import android.graphics.BitmapFactory;
import android.graphics.Bitmap;
import android.util.Log;

import java.net.URL;

public class ImageViewActivity extends AppCompatActivity {

    // TAG utilizada para logs de debug e erros
    private static final String TAG = "IMAGE_VIEW";

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_image_view);

        // Referência ao ImageView da tela (onde a imagem será exibida)
        ImageView imageView = findViewById(R.id.imageViewFull);

        // Recupera a URL da imagem passada pela outra Activity
        String imageUrl = getIntent().getStringExtra("imageUrl");

        // Verifica se a URL é válida
        if (imageUrl != null && !imageUrl.isEmpty()) {

            // Cria uma nova thread para carregar a imagem
            // Isso evita travar a UI (thread principal)
            new Thread(() -> {
                try {
                    // Converte o texto da URL em um objeto URL
                    URL url = new URL(imageUrl);

                    // Faz o download da imagem e transforma em Bitmap
                    Bitmap bitmap = BitmapFactory.decodeStream(
                            url.openConnection().getInputStream()
                    );

                    // Atualiza o ImageView na UI Thread
                    runOnUiThread(() -> imageView.setImageBitmap(bitmap));

                } catch (Exception e) {
                    // Erro ao carregar a imagem
                    Log.e(TAG, "❌ Erro ao carregar imagem: " + e.getMessage(), e);
                }
            }).start();
        }

        // Fecha a Activity quando o usuário clicar na imagem
        imageView.setOnClickListener(v -> finish());
    }
}
