# 🔄 Pipeline CI/CD

## Visão Geral

A pipeline é dividida em **3 workflows**:

```
  feature/xxx
      │
      │  push (nada acontece)
      │
      ▼
      ├── Abre PR → develop
      │         │
      │         ▼
      │   ┌────────────┐
      │   │  ci.yml    │   ← Valida o código
      │   │ 🔨 Build   │
      │   │ 🧪 Test    │
      │   │ 🐳 Docker  │
      │   └────────────┘
      │
      ├── PR mergeado na develop
      │         │
      │         ▼
      │   ┌─────────────┐
      │   │ cd-dev.yml  │   ← Notifica o dev (deploy DEV futuro)
      │   │ � Aviso     │
      │   │ "Abra PR    │
      │   │  → main"    │
      │   └─────────────┘
      │
      └── Abre PR develop → main
                │
                ├── PR mergeado na main
                │
                ▼
          ┌──────────────┐
          │ cd-prod.yml  │   ← Deploy em produção
          │ � ECR Push   │
          │ 🚀 Deploy    │
          │    EC2       │
          └──────────────┘
```

---

## Workflow 1: CI (`ci.yml`)

**Arquivo:** `.github/workflows/ci.yml`

**Aciona quando:**
- Pull Request aberto para `develop` (ex: `feature/xxx` → `develop`)

**Steps:**
1. Checkout do código
2. Setup .NET 8 SDK
3. Restore das dependências
4. Build em modo Release
5. Execução dos testes (com upload dos resultados)
6. Validação do build Docker

> ⚠️ Push direto em branches `feature/*` **não aciona** a pipeline. Só quando abre PR para `develop`.

---

## Workflow 2: CD DEV (`cd-dev.yml`)

**Arquivo:** `.github/workflows/cd-dev.yml`

**Aciona quando:**
- Pull Request é **mergeado** na `develop`

> PRs fechados sem merge **não acionam** este workflow.

### Situação Atual (sem ambiente DEV)

Apenas exibe uma mensagem no log do GitHub Actions avisando o dev:
- ✅ PR mergeado na develop com sucesso
- 👉 Abra um PR de `develop` → `main` para fazer o deploy em produção
- Inclui link direto para criar o PR

### Futuro (com ambiente DEV)

O workflow já tem toda a lógica **comentada** e pronta para ativar:
1. Login no AWS ECR
2. Build e push da imagem Docker
3. Busca instância EC2 por tags (`AppName=api`, `Environment=dev`)
4. Deploy via SSM (sync-env + docker-compose)

**Para ativar:** descomente o job `deploy-dev` e comente o job `notify` no arquivo `cd-dev.yml`.

---

## Workflow 3: CD PROD (`cd-prod.yml`)

**Arquivo:** `.github/workflows/cd-prod.yml`

**Aciona quando:**
- Pull Request é **mergeado** na `main` (ex: `develop` → `main`)

> PRs fechados sem merge **não acionam** o deploy.

**Steps:**
1. Checkout do código
2. Configuração de credenciais AWS
3. Login no Amazon ECR
4. Build e push da imagem Docker para o ECR (tag `sha` + `latest`)
5. Busca instância EC2 por tags (`AppName=api`, `Environment=dev`)
6. Deploy via SSM — envia script para a EC2:
   - `cd /opt/app`
   - `./sync-env.sh` (atualiza `.env` via SSM Parameter Store)
   - Login no ECR
   - `docker-compose pull` (puxa imagem `latest`)
   - `docker-compose down` (para containers)
   - `docker image prune -f` (limpa imagens antigas)
   - `docker-compose up -d` (sobe containers com nova imagem)
   - Health check

---

## Fluxo de Trabalho Completo

```
1. git checkout -b feature/xxx develop
2. Desenvolve e commita                    → nada acontece
3. Abre PR: feature/xxx → develop          → CI roda (build + test) ✅
4. Code review + aprovação
5. Merge do PR na develop                  → CD DEV roda (notifica) 📢
6. Abre PR: develop → main                 → (nenhuma pipeline nesse momento)
7. Merge do PR na main                     → CD PROD roda (ECR + deploy) 🚀
```

---

## Secrets Necessários

### Produção (obrigatórios)

| Secret                  | Exemplo / Descrição                       |
|-------------------------|-------------------------------------------|
| `AWS_ACCESS_KEY_ID`     | Access Key do IAM user                    |
| `AWS_SECRET_ACCESS_KEY` | Secret Key do IAM user                    |
| `ECR_REGISTRY`          | `123456789012.dkr.ecr.us-east-1.amazonaws.com` |

> ⚠️ O deploy usa **SSM (Systems Manager)** — não precisa de chave SSH nem IP da EC2. A instância é encontrada automaticamente pelas tags.

### Permissões IAM necessárias

O IAM user do GitHub Actions precisa de:
- `ecr:GetAuthorizationToken`, `ecr:BatchCheckLayerAvailability`, `ecr:PutImage`, etc.
- `ec2:DescribeInstances`
- `ssm:SendCommand`, `ssm:GetCommandInvocation`

### Pré-requisitos na EC2

- **SSM Agent** instalado e rodando
- **IAM Instance Role** com acesso ao ECR e SSM Parameter Store
- **Tags** na instância: `AppName=api`, `Environment=dev` (ou `prod`)
- **Docker** e **docker-compose** instalados
- Pasta `/opt/app` com `docker-compose.yml` e `sync-env.sh`

### Como adicionar os Secrets

1. Vá no repositório do GitHub
2. `Settings` → `Secrets and variables` → `Actions`
3. Clique em `New repository secret`
4. Adicione cada secret listado acima

## Troubleshooting

### Build falhou no CI
- Verifique se o .NET SDK 8.0 está configurado corretamente
- Veja os logs de teste no artefato `test-results`

### Push para ECR falhou
- Verifique se os secrets `AWS_ACCESS_KEY_ID` e `AWS_SECRET_ACCESS_KEY` estão corretos
- Confirme que o repositório ECR (`dev/nautihub`) existe
- Confirme que o IAM user tem permissões no ECR

### Deploy falhou (SSM)
- Verifique se a EC2 tem o SSM Agent rodando: `sudo systemctl status amazon-ssm-agent`
- Confirme que as tags `AppName=api` e `Environment=dev` estão na instância
- Verifique se o IAM user tem permissão `ssm:SendCommand`
- Veja o output de erro no log do GitHub Actions (StandardErrorContent)

### docker-compose não encontrado
- Instale na EC2: `sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose && sudo chmod +x /usr/local/bin/docker-compose`

### Health check falhou após deploy
- O container pode demorar alguns segundos para iniciar
- Verifique os logs: `docker-compose logs` na pasta `/opt/app`
