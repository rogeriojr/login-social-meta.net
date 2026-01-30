# Template de Autenticação Social com Meta (Facebook/Instagram)

Este projeto serve como um **template de referência** para integrar o Login Social (Facebook e Instagram) em aplicações .NET existentes.

O foco aqui é demonstrar a configuração correta do middleware de autenticação, o tratamento de HTTPS (essencial para o Facebook) e a recuperação segura de dados do usuário (incluindo foto de alta resolução).

## 🚀 Como Integrar no Seu Projeto

Siga estes passos para levar essa funcionalidade para o seu projeto em produção.

### 1. Instalação de Pacotes

No seu projeto, instale os seguintes pacotes NuGet:

```bash
dotnet add package Microsoft.AspNetCore.Authentication.Facebook
dotnet add package AspNet.Security.OAuth.Instagram
dotnet add package DotNetEnv
```

*   `Microsoft.AspNetCore.Authentication.Facebook`: Middleware oficial para Facebook.
*   `AspNet.Security.OAuth.Instagram`: Middleware para Instagram (Basic Display API).
*   `DotNetEnv`: Para carregar variáveis de ambiente de um arquivo `.env`.

### 2. Configuração de Segurança (`.env`)

```env
FACEBOOK_APP_ID=seu_app_id_aqui
FACEBOOK_APP_SECRET=seu_app_secret_aqui
INSTAGRAM_CLIENT_ID=seu_id_instagram_aqui
INSTAGRAM_CLIENT_SECRET=sua_chave_instagram_aqui
```

### 3. Configuração do `Program.cs`

```csharp
.AddFacebook(options => { ... })
.AddInstagram(options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("INSTAGRAM_CLIENT_ID");
    options.ClientSecret = Environment.GetEnvironmentVariable("INSTAGRAM_CLIENT_SECRET");
    
    options.Events = new OAuthEvents
    {
        OnCreatingTicket = context =>
        {
            var username = context.User.GetProperty("username").GetString();
            if (!string.IsNullOrEmpty(username))
            {
                context.Identity.AddClaim(new Claim(ClaimTypes.Name, username));
            }
            return Task.CompletedTask;
        }
    };
});
```

Adicione o seguinte código no início do seu `Program.cs` para carregar as chaves e configurar o serviço.

**Importante:** A ordem dos middlewares (`Use...`) é crítica.

```csharp
// 1. Carregar variáveis do .env
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// ... outros serviços ...

// 2. Configurar Autenticação e Cookies
// A política de cookies é essencial para navegadores modernos (Chrome/Edge) e iframes
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
    options.Secure = CookieSecurePolicy.Always;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.Facebook.FacebookDefaults.AuthenticationScheme;
})
.AddCookie()
.AddFacebook(options =>
{
    // Ler chaves do ambiente (prioridade) ou appsettings (fallback)
    options.AppId = Environment.GetEnvironmentVariable("FACEBOOK_APP_ID") 
                    ?? builder.Configuration["Authentication:Facebook:AppId"];
    options.AppSecret = Environment.GetEnvironmentVariable("FACEBOOK_APP_SECRET") 
                        ?? builder.Configuration["Authentication:Facebook:AppSecret"];
    
    // Solicitar permissões extras (ex: foto)
    options.Fields.Add("picture");
    
    // Evento para capturar a URL da foto (não vem por padrão nos Claims básicos)
    options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
    {
        OnCreatingTicket = context =>
        {
            var picture = context.User.GetProperty("picture").GetProperty("data").GetProperty("url").GetString();
            if (!string.IsNullOrEmpty(picture))
            {
                context.Identity.AddClaim(new System.Security.Claims.Claim("urn:facebook:picture", picture));
            }
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

// ...

// 3. Configuração de Proxy Reverso / HTTPS (CRÍTICO PARA FACEBOOK)
// O Facebook exige HTTPS. Se você usa Ngrok, Docker ou Load Balancer,
// o middleware abaixo garante que o .NET gere as URLs de callback com 'https://'
app.UseForwardedHeaders();

// Forçar HTTPS agressivamente (Útil para ambiente de Dev com Ngrok)
app.Use(async (context, next) =>
{
    context.Request.Scheme = "https";
    await next();
});

app.UseHttpsRedirection();
app.UseRouting();

app.UseCookiePolicy(); // <--- Antes da Autenticação
app.UseAuthentication();
app.UseAuthorization();
```

### 4. Controller (`HomeController.cs`)

Como iniciar o login e recuperar os dados no callback.

```csharp
public IActionResult Login()
{
    // Inicia o fluxo e define para onde voltar (Privacy/Perfil)
    return Challenge(new AuthenticationProperties
    {
        RedirectUri = Url.Action("Privacy", "Home")
    }, FacebookDefaults.AuthenticationScheme);
}

public IActionResult Privacy() // ou Perfil
{
    // Recuperar dados dos Claims
    var name = User.FindFirst(ClaimTypes.Name)?.Value;
    var email = User.FindFirst(ClaimTypes.Email)?.Value;
    
    // Recuperar a foto customizada que mapeamos no Program.cs
    var photoUrl = User.FindFirst("urn:facebook:picture")?.Value;

    var model = new UserProfileViewModel
    {
        Name = name,
        Email = email,
        PhotoUrl = photoUrl,
        Provider = "Facebook"
    };
    return View(model);
}
```

## 🛠️ Executando este Projeto Localmente

1.  **Clone o repo:**
    ```bash
    git clone https://github.com/rogeriojr/-login-social-meta.net.git
    ```
2.  **Crie o arquivo `.env`:**
    Copie as chaves do seu App no [Meta for Developers](https://developers.facebook.com/).
3.  **Rode o projeto:**
    ```bash
    dotnet run
    ```
4.  **Exponha com Ngrok (obrigatório para Facebook):**
    ```bash
    ngrok http 5069
    ```
5.  **Configure no Facebook:**
    Adicione a URL do Ngrok (`https://....ngrok-free.app/signin-facebook`) nas configurações de "Valid OAuth Redirect URIs".

---
Desenvolvido para facilitar a integração de Social Login em .NET.
