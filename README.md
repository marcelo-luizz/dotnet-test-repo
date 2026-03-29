# 🚀 Sample API - .NET 8

API simples em .NET 8 para testar pipeline CI/CD com GitHub Actions e deploy na AWS EC2.

## 📁 Estrutura do Projeto

```
.
├── .github/
│   └── workflows/
│       ├── ci.yml             # Pipeline CI (build + test)
│       ├── cd-dev.yml         # Pipeline CD DEV (notifica + deploy futuro)
│       └── cd-prod.yml        # Pipeline CD PROD (build + push ECR + deploy EC2)
├── src/
│   └── Api/
│       ├── Endpoints/
│       │   ├── HealthEndpoints.cs
│       │   └── WeatherEndpoints.cs
│       ├── Api.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       └── appsettings.Development.json
├── tests/
│   └── Api.Tests/
│       ├── HealthEndpointsTests.cs
│       ├── WeatherEndpointsTests.cs
│       └── Api.Tests.csproj
├── docs/
│   ├── SETUP_EC2.md           # Guia de setup da EC2
│   └── PIPELINE.md            # Documentação detalhada do CI/CD
├── Dockerfile
├── .dockerignore
├── .editorconfig              # Regras de estilo e análise de código
├── .gitignore
├── SampleApi.sln
└── README.md
```

## 🏃 Rodando Localmente

### Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/) (opcional)

### Sem Docker

```bash
# Restaurar dependências
dotnet restore

# Rodar a API
dotnet run --project src/Api

# Rodar os testes
dotnet test
```

A API estará disponível em `http://localhost:5000`.

### Com Docker

```bash
# Build da imagem
docker build -t sample-api .

# Rodar o container
docker run -d -p 8080:8080 --name sample-api sample-api

# Verificar health
curl http://localhost:8080/api/health
```

## 📡 Endpoints

| Método | Rota                         | Descrição                          |
|--------|------------------------------|------------------------------------|
| GET    | `/api/health`                | Health check da API                |
| GET    | `/api/health/ready`          | Readiness check                    |
| GET    | `/api/weather/forecast`      | Previsão do tempo (5 dias)         |
| GET    | `/api/weather/forecast/{city}` | Previsão por cidade              |
| GET    | `/swagger`                   | Documentação Swagger UI            |

## 🔄 Pipeline CI/CD

Três workflows separados:

- **CI (`ci.yml`)** — PR aberto para `develop` → Build + Testes + Validação Docker
- **CD DEV (`cd-dev.yml`)** — PR mergeado na `develop` → Notifica o dev para abrir PR na `main` *(deploy para DEV comentado, pronto para ativar no futuro)*
- **CD PROD (`cd-prod.yml`)** — PR mergeado na `main` → Build + Push ECR + Deploy EC2

### Fluxo

```
feature/xxx → PR → develop     → CI roda (build + test)
                   merge         → CD DEV roda (notifica: "abra PR para main")
develop     → PR → main         → CD PROD roda (ECR + deploy EC2)
```

Veja mais detalhes em [docs/PIPELINE.md](docs/PIPELINE.md).

## ⚙️ Configuração dos Secrets no GitHub

Configure os seguintes secrets no repositório (`Settings > Secrets and variables > Actions`):

| Secret                  | Descrição                                        | Usado em    |
|-------------------------|--------------------------------------------------|-------------|
| `AWS_ACCESS_KEY_ID`     | Access Key do IAM user com acesso ao ECR/SSM     | CD PROD     |
| `AWS_SECRET_ACCESS_KEY` | Secret Key do IAM user                           | CD PROD     |
| `ECR_REGISTRY`          | URL do ECR (ex: `123456789.dkr.ecr.us-east-1.amazonaws.com`) | CD PROD |

> ⚠️ O deploy usa **SSM (Systems Manager)** — não precisa de chave SSH. A EC2 é encontrada automaticamente pelas tags `AppName=api` e `Environment=dev`.

> 💡 O IAM user precisa de permissões: `ecr:*`, `ec2:DescribeInstances`, `ssm:SendCommand`, `ssm:GetCommandInvocation`.

Veja o guia completo de setup da EC2 em [docs/SETUP_EC2.md](docs/SETUP_EC2.md).

## 📝 Licença

MIT
