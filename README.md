# 🚀 Sample API - .NET 8

API simples em .NET 8 para testar pipeline CI/CD com GitHub Actions e deploy na AWS EC2.

## 📁 Estrutura do Projeto

```
.
├── .github/
│   └── workflows/
│       ├── ci.yml             # Pipeline CI (build + test)
│       └── cd.yml             # Pipeline CD (build + push ECR + deploy EC2)
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
│   ├── SETUP_EC2.md
│   └── PIPELINE.md
├── Dockerfile
├── .dockerignore
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

Duas pipelines separadas:

- **CI (`ci.yml`)** — Push na `develop` ou PR aberto para `develop` → Build + Testes + Validação Docker
- **CD (`cd.yml`)** — PR mergeado na `develop` → Build + Push ECR + Deploy EC2

Veja mais detalhes em [docs/PIPELINE.md](docs/PIPELINE.md).

## ⚙️ Configuração dos Secrets no GitHub

Configure os seguintes secrets no repositório (`Settings > Secrets and variables > Actions`):

| Secret                  | Descrição                                    |
|-------------------------|----------------------------------------------|
| `AWS_ACCESS_KEY_ID`     | Access Key do IAM user com acesso ao ECR     |
| `AWS_SECRET_ACCESS_KEY` | Secret Key do IAM user                       |
| `EC2_HOST`              | IP público ou DNS da instância EC2           |
| `EC2_USER`              | Usuário SSH (ex: `ec2-user`, `ubuntu`)       |
| `EC2_SSH_KEY`           | Chave privada SSH para acesso à EC2          |

## 📝 Licença

MIT
