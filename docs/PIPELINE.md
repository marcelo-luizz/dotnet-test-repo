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
2. Build e push da imagem Docker (repositório `sample-api-dev`)
3. SSH na EC2 DEV para deploy

**Para ativar:** descomente o job `deploy-dev` e comente o job `notify` no arquivo `cd-dev.yml`. Adicione os secrets: `EC2_HOST_DEV`, `EC2_USER_DEV`, `EC2_SSH_KEY_DEV`.

---

## Workflow 3: CD PROD (`cd-prod.yml`)

**Arquivo:** `.github/workflows/cd-prod.yml`

**Aciona quando:**
- Pull Request é **mergeado** na `main` (ex: `develop` → `main`)

> PRs fechados sem merge **não acionam** o deploy.

**Steps:**
1. Checkout do código
2. Login no AWS ECR
3. Build e push da imagem Docker para o ECR
4. SSH na EC2 para:
   - Pull da nova imagem do ECR
   - Stop/Remove do container antigo
   - Start do novo container
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
| `EC2_HOST`              | `54.123.45.67`                            |
| `EC2_USER`              | `ubuntu`                                  |
| `EC2_SSH_KEY`           | Conteúdo da chave `.pem` da EC2           |

### Desenvolvimento (para o futuro)

| Secret                  | Exemplo / Descrição                       |
|-------------------------|-------------------------------------------|
| `EC2_HOST_DEV`          | IP da EC2 de DEV                          |
| `EC2_USER_DEV`          | Usuário SSH da EC2 de DEV                 |
| `EC2_SSH_KEY_DEV`       | Chave `.pem` da EC2 de DEV               |

### Como adicionar os Secrets

1. Vá no repositório do GitHub
2. `Settings` → `Secrets and variables` → `Actions`
3. Clique em `New repository secret`
4. Adicione cada secret listado acima

### Obtendo a chave SSH

```bash
# Copie o conteúdo da chave .pem
cat ~/.ssh/sua-chave-ec2.pem
```

Cole o conteúdo completo (incluindo `-----BEGIN RSA PRIVATE KEY-----` e `-----END RSA PRIVATE KEY-----`) como valor do secret `EC2_SSH_KEY`.

## Troubleshooting

### Build falhou
- Verifique se o .NET SDK 8.0 está configurado corretamente
- Veja os logs de teste no artefato `test-results`

### Deploy falhou
- Verifique se os secrets estão configurados corretamente
- Confirme que a EC2 tem Docker instalado
- Confirme que a porta 8080 está liberada no Security Group
- Verifique se a chave SSH está correta

### Health check falhou após deploy
- O container pode demorar alguns segundos para iniciar
- Verifique os logs: `docker logs sample-api`
