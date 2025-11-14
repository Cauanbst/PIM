plugins {
    alias(libs.plugins.android.application)
}

android {
    namespace = "com.example.appsuportecliente"
    compileSdk = 34 // 36 ainda é preview, use 34 para estabilidade

    defaultConfig {
        applicationId = "com.example.appsuportecliente"
        minSdk = 26
        targetSdk = 34
        versionCode = 1
        versionName = "1.0"

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_11
        targetCompatibility = JavaVersion.VERSION_11
    }
}

dependencies {
    // 🔹 Dependências básicas do Android
    implementation(libs.appcompat)
    implementation(libs.material)
    implementation(libs.activity)
    implementation(libs.constraintlayout)

    // 🔹 OkHttp (para requisições HTTP)
    implementation("com.squareup.okhttp3:okhttp:4.12.0")

    // 🔹 Interceptor de logs (para debug de requisições)
    implementation("com.squareup.okhttp3:logging-interceptor:4.12.0")

    // 🔹 Retrofit (para conectar com o backend ASP.NET)
    implementation("com.squareup.retrofit2:retrofit:2.9.0")

    // 🔹 Conversor Gson (para objetos Java ↔ JSON)
    implementation("com.squareup.retrofit2:converter-gson:2.9.0")

    // 🔹 SignalR (comunicação em tempo real)
    implementation("com.microsoft.signalr:signalr:7.0.5")

    // 🔹 Picasso (para exibir imagens de forma simples)
    implementation("com.squareup.picasso:picasso:2.71828")
    implementation("com.google.android.material:material:1.11.0")


    // 🔹 Dependências de teste
    testImplementation(libs.junit)
    androidTestImplementation(libs.ext.junit)
    androidTestImplementation(libs.espresso.core)
}
